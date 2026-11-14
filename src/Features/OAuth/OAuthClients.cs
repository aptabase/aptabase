using System.Net;

namespace Aptabase.Features.OAuth;

public static class OAuthScopes
{
    public const string StatsRead = "stats:read";
    public const string SessionsRead = "sessions:read";

    public static readonly string[] All = [StatsRead, SessionsRead];

    public static bool TryParse(string? value, out string[] scopes)
    {
        scopes = (value ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToArray();
        return scopes.All(s => All.Contains(s));
    }

    public static string Format(IEnumerable<string> scopes) => string.Join(' ', All.Where(scopes.Contains));
}

/// <summary>
/// A client as the authorization server sees it once resolved: either a built-in constant
/// or the content of a Client ID Metadata Document.
/// </summary>
public record ResolvedClient(string ClientId, string Name, string Url, string[] RedirectUris, bool Verified);

public static class OAuthClients
{
    // Clients that cannot host a Client ID Metadata Document get a fixed public client here.
    // No secret, PKCE like everyone else, and codes only ever go to these redirect URIs.
    private static readonly ResolvedClient[] BuiltIn =
    [
        new("cursor", "Cursor", "https://cursor.com",
            ["http://localhost:8787/callback", "https://www.cursor.com/agents/mcp/oauth/callback"], true),
    ];

    // Metadata document URLs of clients we know. Anyone can host a document with any name,
    // so only these get the "Verified" label on the consent page.
    private static readonly HashSet<string> VerifiedMetadataUrls = new(StringComparer.Ordinal)
    {
        "https://claude.ai/oauth/claude-code-client-metadata",
    };

    public static ResolvedClient? FindBuiltIn(string clientId)
        => BuiltIn.FirstOrDefault(c => string.Equals(c.ClientId, clientId, StringComparison.Ordinal));

    public static bool IsVerifiedMetadataUrl(string clientId) => VerifiedMetadataUrls.Contains(clientId);

    /// <summary>
    /// A metadata document client id is an HTTPS URL with a path, and nothing that could
    /// make two spellings of it mean different documents.
    /// </summary>
    public static bool IsMetadataDocumentUrl(string clientId, out Uri uri)
    {
        uri = null!;
        if (clientId.Length > 255 || !Uri.TryCreate(clientId, UriKind.Absolute, out var parsed))
            return false;

        if (parsed.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(parsed.UserInfo) || !string.IsNullOrEmpty(parsed.Fragment))
            return false;

        if (parsed.AbsolutePath.Length <= 1 || clientId.Contains("/./") || clientId.Contains("/../"))
            return false;

        uri = parsed;
        return true;
    }

    /// <summary>
    /// Redirect URIs may be https, http on a loopback host, or a private-use scheme in
    /// reverse-domain form. Never with a fragment.
    /// </summary>
    public static bool IsAllowedRedirectUri(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 2000 || value.Contains('#'))
            return false;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme == Uri.UriSchemeHttps)
            return !string.IsNullOrEmpty(uri.Host) && string.IsNullOrEmpty(uri.UserInfo);

        if (uri.Scheme == Uri.UriSchemeHttp)
            return IsLoopbackHost(uri.Host) && string.IsNullOrEmpty(uri.UserInfo);

        // com.example.app:/callback
        return uri.Scheme.Contains('.');
    }

    /// <summary>
    /// Exact string match, except for loopback http where the port is ignored
    /// (RFC 8252 section 7.3): native clients listen on a different port each time.
    /// </summary>
    public static bool RedirectUriMatches(IEnumerable<string> registered, string requested)
    {
        if (!IsAllowedRedirectUri(requested))
            return false;

        foreach (var candidate in registered)
        {
            if (string.Equals(candidate, requested, StringComparison.Ordinal))
                return true;

            if (Uri.TryCreate(candidate, UriKind.Absolute, out var a) && Uri.TryCreate(requested, UriKind.Absolute, out var b)
                && a.Scheme == Uri.UriSchemeHttp && b.Scheme == Uri.UriSchemeHttp
                && IsLoopbackHost(a.Host) && IsLoopbackHost(b.Host)
                && string.Equals(a.Host, b.Host, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.AbsolutePath, b.AbsolutePath, StringComparison.Ordinal)
                && string.Equals(a.Query, b.Query, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static bool IsLoopbackHost(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        return IPAddress.TryParse(host.Trim('[', ']'), out var ip) && IPAddress.IsLoopback(ip);
    }
}

public static class OAuthResource
{
    /// <summary>
    /// RFC 3986 normalization, as far as resource indicators need it: lowercase scheme and
    /// host, default port dropped, trailing slash dropped. Returns null when the value is
    /// not an absolute http(s) URL or carries a query or fragment.
    /// </summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
            return null;

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            return null;

        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
            return null;

        var origin = uri.GetLeftPart(UriPartial.Authority).ToLowerInvariant();
        var path = uri.AbsolutePath.TrimEnd('/');
        return origin + path;
    }

    /// <summary>
    /// The authorization server issues tokens for this origin only: the origin itself,
    /// or one of the two surfaces under it.
    /// </summary>
    public static bool IsAcceptedResource(string normalized, string origin)
        => normalized == origin || normalized == $"{origin}/mcp" || normalized == $"{origin}/api/v1";

    /// <summary>
    /// Whether a token with this audience may be used on the surface the request targets.
    /// A token for the origin works everywhere, one for a surface only there.
    /// </summary>
    public static bool AudienceAllowsPath(string? audience, string origin, PathString path)
    {
        var normalized = Normalize(audience);
        if (normalized is null)
            return false;

        if (normalized == origin)
            return true;

        if (normalized == $"{origin}/mcp")
            return path.StartsWithSegments("/mcp");

        if (normalized == $"{origin}/api/v1")
            return path.StartsWithSegments("/api/v1");

        return false;
    }
}
