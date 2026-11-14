using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace Aptabase.Features.OAuth;

public class ClientMetadataException : Exception
{
    public ClientMetadataException(string message) : base(message) { }
}

public interface IClientResolver
{
    /// <summary>
    /// Resolves a client id to its name and redirect URIs. Throws
    /// <see cref="ClientMetadataException"/> when the client cannot be resolved.
    /// </summary>
    Task<ResolvedClient> ResolveAsync(string clientId, bool forceRefresh, CancellationToken cancellationToken);
}

public interface IClientMetadataFetcher
{
    Task<(string Body, TimeSpan? MaxAge)> FetchAsync(Uri url, CancellationToken cancellationToken);
}

public class ClientResolver : IClientResolver
{
    private static readonly TimeSpan CacheFloor = TimeSpan.FromHours(1);
    private static readonly TimeSpan CacheCeiling = TimeSpan.FromHours(24);

    private readonly IClientMetadataFetcher _fetcher;
    private readonly IMemoryCache _cache;

    public ClientResolver(IClientMetadataFetcher fetcher, IMemoryCache cache)
    {
        _fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    private class MetadataDocument
    {
        [JsonPropertyName("client_id")]
        public string? ClientId { get; set; }
        [JsonPropertyName("client_name")]
        public string? ClientName { get; set; }
        [JsonPropertyName("redirect_uris")]
        public string[]? RedirectUris { get; set; }
    }

    public async Task<ResolvedClient> ResolveAsync(string clientId, bool forceRefresh, CancellationToken cancellationToken)
    {
        var builtIn = OAuthClients.FindBuiltIn(clientId);
        if (builtIn is not null)
            return builtIn;

        if (!OAuthClients.IsMetadataDocumentUrl(clientId, out var url))
            throw new ClientMetadataException("The client id is not a valid client metadata document URL.");

        var cacheKey = $"oauth-client:{clientId}";
        if (!forceRefresh && _cache.TryGetValue<ResolvedClient>(cacheKey, out var cached) && cached is not null)
            return cached;

        var (body, maxAge) = await _fetcher.FetchAsync(url, cancellationToken);

        MetadataDocument? doc;
        try
        {
            doc = JsonSerializer.Deserialize<MetadataDocument>(body);
        }
        catch (JsonException)
        {
            throw new ClientMetadataException("The client metadata document is not valid JSON.");
        }

        // The document must name itself with exactly the URL it was fetched from,
        // otherwise any page could vouch for any other client id.
        if (doc is null || !string.Equals(doc.ClientId, clientId, StringComparison.Ordinal))
            throw new ClientMetadataException("The client metadata document does not match its client id.");

        // The name is shown on the consent page, in lists and in an email. It is always
        // encoded where it is rendered, and control characters have no business in it.
        var name = new string((doc.ClientName ?? "").Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (name.Length == 0)
            throw new ClientMetadataException("The client metadata document has no client name.");

        var redirectUris = (doc.RedirectUris ?? []).Where(OAuthClients.IsAllowedRedirectUri).ToArray();
        if (redirectUris.Length == 0)
            throw new ClientMetadataException("The client metadata document lists no usable redirect URI.");

        var client = new ResolvedClient(
            clientId,
            name.Length > 80 ? name[..80] : name,
            clientId,
            redirectUris,
            OAuthClients.IsVerifiedMetadataUrl(clientId));

        var ttl = maxAge ?? CacheFloor;
        if (ttl < CacheFloor) ttl = CacheFloor;
        if (ttl > CacheCeiling) ttl = CacheCeiling;
        _cache.Set(cacheKey, client, ttl);

        return client;
    }
}

/// <summary>
/// Fetches a URL chosen by a third party, so it is written as an SSRF sink: HTTPS only,
/// no redirects, a size cap, a timeout, and the connection is pinned to an address that
/// was checked to be public. Resolving once and connecting to that address is what stops
/// DNS rebinding, a second lookup at connect time would defeat the check.
/// </summary>
public class ClientMetadataFetcher : IClientMetadataFetcher
{
    private const int MaxBytes = 64 * 1024;
    private const int GlobalBudgetPerMinute = 60;
    private const int PerHostBudgetPerMinute = 10;

    private readonly HttpClient _http;
    private readonly ConcurrentDictionary<string, (long Window, int Count)> _budget = new();

    public ClientMetadataFetcher()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = TimeSpan.FromSeconds(5),
            PooledConnectionLifetime = TimeSpan.FromMinutes(1),
            ConnectCallback = ConnectToPublicAddressAsync,
        };

        _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Aptabase-OAuth/1.0");
    }

    public async Task<(string Body, TimeSpan? MaxAge)> FetchAsync(Uri url, CancellationToken cancellationToken)
    {
        if (url.Scheme != Uri.UriSchemeHttps)
            throw new ClientMetadataException("Client metadata documents must be served over HTTPS.");

        if (!TakeBudget("*", GlobalBudgetPerMinute) || !TakeBudget(url.Host.ToLowerInvariant(), PerHostBudgetPerMinute))
            throw new ClientMetadataException("Too many client metadata requests, try again in a minute.");

        try
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new ClientMetadataException("The client metadata document could not be fetched.");

            if (response.Content.Headers.ContentLength > MaxBytes)
                throw new ClientMetadataException("The client metadata document is too large.");

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var buffer = new byte[MaxBytes + 1];
            var read = 0;
            while (read < buffer.Length)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(read, buffer.Length - read), cancellationToken);
                if (n == 0) break;
                read += n;
            }

            if (read > MaxBytes)
                throw new ClientMetadataException("The client metadata document is too large.");

            return (System.Text.Encoding.UTF8.GetString(buffer, 0, read), response.Headers.CacheControl?.MaxAge);
        }
        catch (ClientMetadataException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or SocketException)
        {
            throw new ClientMetadataException("The client metadata document could not be fetched.");
        }
    }

    private bool TakeBudget(string key, int limit)
    {
        var window = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
        var updated = _budget.AddOrUpdate(key, _ => (window, 1), (_, current) => current.Window == window ? (window, current.Count + 1) : (window, 1));

        if (_budget.Count > 10_000)
        {
            foreach (var entry in _budget.Where(e => e.Value.Window != window).ToArray())
                _budget.TryRemove(entry.Key, out _);
        }

        return updated.Count <= limit;
    }

    private static async ValueTask<Stream> ConnectToPublicAddressAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        var address = addresses.FirstOrDefault(IsPublicAddress)
            ?? throw new HttpRequestException("Host does not resolve to a public address.");

        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6None))
            return false;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return b[0] switch
            {
                0 or 10 or 127 => false,
                100 when b[1] >= 64 && b[1] <= 127 => false,   // carrier-grade NAT
                169 when b[1] == 254 => false,                 // link-local, cloud metadata
                172 when b[1] >= 16 && b[1] <= 31 => false,
                192 when b[1] == 168 => false,
                192 when b[1] == 0 && b[2] == 0 => false,
                198 when b[1] == 18 || b[1] == 19 => false,
                >= 224 => false,                               // multicast and reserved
                _ => true,
            };
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast || address.IsIPv6UniqueLocal)
                return false;

            // 64:ff9b::/96 (NAT64) and 2002::/16 (6to4) can embed an IPv4 address
            var b = address.GetAddressBytes();
            if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xff && b[3] == 0x9b) return false;
            if (b[0] == 0x20 && b[1] == 0x02) return false;
            return true;
        }

        return false;
    }
}
