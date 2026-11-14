using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using Aptabase.Features.OAuth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Aptabase.Features.Api;

/// <summary>
/// Who a bearer request is: always a grant. The grant row is the source of truth, the
/// claims in the token are only a pointer to it.
/// </summary>
public record ApiPrincipal(OAuthGrant Grant)
{
    public string GrantId => Grant.Id;
    public string SubjectId => Grant.SubjectId;
    public bool HasScope(string scope) => Grant.HasScope(scope);
}

public static class ApiPrincipalExtensions
{
    private const string ItemKey = "Aptabase.ApiPrincipal";

    public static void SetApiPrincipal(this HttpContext context, ApiPrincipal principal) => context.Items[ItemKey] = principal;

    public static ApiPrincipal GetApiPrincipal(this HttpContext context)
        => context.Items[ItemKey] as ApiPrincipal ?? throw new InvalidOperationException("The request did not pass the query API gate.");
}

/// <summary>
/// Per-grant and per-subject limits of the query API. In memory per instance: their job
/// is to stop a runaway loop, not to meter. The monthly allowance, which is metered,
/// lives in Postgres.
/// </summary>
public class ApiLimits
{
    public const int RequestsPerMinute = 60;
    public const int ConcurrentQueriesPerGrant = 2;

    // App-access denials per subject in a ten-minute window: slow down at the first
    // threshold, revoke the offending user grant at the second
    public const int DeniedSlowDownThreshold = 20;
    public const int DeniedRevokeThreshold = 100;

    private readonly ConcurrentDictionary<string, (long Window, int Count)> _requests = new();
    private readonly ConcurrentDictionary<string, int> _inFlight = new();
    private readonly ConcurrentDictionary<string, DateTime> _slowedSubjects = new();

    /// <summary>Counts a request. False when the grant is over its rate limit; the out value is seconds until the window ends.</summary>
    public bool TryCountRequest(string grantId, out int retryAfterSeconds)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var window = now / 60;
        var updated = _requests.AddOrUpdate(grantId, _ => (window, 1), (_, c) => c.Window == window ? (window, c.Count + 1) : (window, 1));

        if (_requests.Count > 100_000)
        {
            foreach (var entry in _requests.Where(e => e.Value.Window != window).ToArray())
                _requests.TryRemove(entry.Key, out _);
        }

        retryAfterSeconds = (int)(60 - now % 60);
        return updated.Count <= RequestsPerMinute;
    }

    public bool TryEnterQuery(string grantId)
    {
        var count = _inFlight.AddOrUpdate(grantId, 1, (_, c) => c + 1);
        if (count <= ConcurrentQueriesPerGrant)
            return true;

        ExitQuery(grantId);
        return false;
    }

    public void ExitQuery(string grantId)
    {
        var count = _inFlight.AddOrUpdate(grantId, 0, (_, c) => Math.Max(0, c - 1));
        if (count == 0)
            _inFlight.TryRemove(new KeyValuePair<string, int>(grantId, 0));
    }

    public void SlowDown(string subjectId)
        => _slowedSubjects[subjectId] = DateTime.UtcNow.AddMinutes(OAuthStore.DeniedWindowMinutes);

    public bool IsSlowed(string subjectId, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;
        if (!_slowedSubjects.TryGetValue(subjectId, out var until))
            return false;

        if (until <= DateTime.UtcNow)
        {
            _slowedSubjects.TryRemove(subjectId, out _);
            return false;
        }

        retryAfterSeconds = Math.Max(1, (int)(until - DateTime.UtcNow).TotalSeconds);
        return true;
    }
}

/// <summary>
/// Stands in front of /api/v1 and /mcp. Validates the access token, loads the grant it
/// points to, and applies the per-grant limits, in that order: nothing here keys on a
/// claim that has not been verified. No cookie is ever read on these routes.
/// </summary>
public class ApiGate
{
    public const string OpenApiPath = "/api/v1/openapi.json";

    private readonly RequestDelegate _next;
    private readonly EnvSettings _env;
    private readonly IOAuthTokenService _tokens;
    private readonly IOAuthStore _store;
    private readonly IMemoryCache _cache;
    private readonly ApiLimits _limits;
    private readonly JsonWebTokenHandler _handler = new() { MapInboundClaims = false };

    public ApiGate(RequestDelegate next, EnvSettings env, IOAuthTokenService tokens, IOAuthStore store, IMemoryCache cache, ApiLimits limits)
    {
        _next = next;
        _env = env;
        _tokens = tokens;
        _store = store;
        _cache = cache;
        _limits = limits;
    }

    // /mcp is one endpoint. Anything below it is not a route and answers 404 like any
    // other unmapped protocol path.
    public static bool Covers(PathString path)
        => path.StartsWithSegments("/api/v1") || path.Equals("/mcp", StringComparison.OrdinalIgnoreCase) || path.Equals("/mcp/", StringComparison.OrdinalIgnoreCase);

    public static string GrantCacheKey(string grantId) => $"api-grant:{grantId}";

    public async Task Invoke(HttpContext context)
    {
        if (!_env.QueryApiEnabled)
        {
            context.Response.StatusCode = 404;
            return;
        }

        if (context.Request.Path.Equals(OpenApiPath, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Tokens are read from the Authorization header and from nowhere else
        if (!AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out var header)
            || !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(header.Parameter))
        {
            await Challenge(context, invalidToken: false);
            return;
        }

        var grant = await Authenticate(header.Parameter, context);
        if (grant is null)
        {
            await Challenge(context, invalidToken: true);
            return;
        }

        if (!_limits.TryCountRequest(grant.Id, out var retryAfter))
        {
            await TooManyRequests(context, retryAfter, $"Rate limit of {ApiLimits.RequestsPerMinute} requests per minute exceeded.");
            return;
        }

        if (grant.SubjectDenied >= ApiLimits.DeniedSlowDownThreshold)
            _limits.SlowDown(grant.SubjectId);

        // A subject that kept asking for apps it cannot read is slowed down. A client that
        // keeps going regardless is a runaway or a probe: each such request counts as one
        // more denial, and a user grant that reaches the second threshold is revoked. The
        // rate limit above bounds how many of these writes one grant can cause.
        if (_limits.IsSlowed(grant.SubjectId, out var slowedFor))
        {
            var total = await _store.RecordDenied(grant.Id, context.RequestAborted);
            if (total >= ApiLimits.DeniedRevokeThreshold && !grant.IsService)
                await _store.RevokeGrant(grant.Id, RevokedReason.AccessFailures, context.RequestAborted);

            await TooManyRequests(context, slowedFor, "Too many requests for apps this account cannot access. Try again later.");
            return;
        }

        var missingScope = await FindMissingToolScope(context, grant);
        if (missingScope is not null)
        {
            context.Response.StatusCode = 403;
            context.Response.Headers.WWWAuthenticate = ChallengeHeader(_env, "insufficient_scope", missingScope);
            await WriteError(context, "insufficient_scope", $"This connection does not have the {missingScope} permission.");
            return;
        }

        context.SetApiPrincipal(new ApiPrincipal(grant));
        await _next(context);
    }

    // An MCP tool call for which the token lacks the scope is answered at the HTTP level,
    // with the challenge that tells the client which scope to ask for. Returning a tool
    // error instead would leave the client with no way to start a step-up authorization.
    private static async Task<string?> FindMissingToolScope(HttpContext context, OAuthGrant grant)
    {
        var request = context.Request;
        if (!HttpMethods.IsPost(request.Method) || !request.Path.StartsWithSegments("/mcp"))
            return null;

        if (request.ContentLength is null or > 64 * 1024)
            return null;

        request.EnableBuffering();
        try
        {
            using var document = await JsonDocument.ParseAsync(request.Body, cancellationToken: context.RequestAborted);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("method", out var method) || method.GetString() != "tools/call"
                || !root.TryGetProperty("params", out var parameters) || parameters.ValueKind != JsonValueKind.Object
                || !parameters.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String)
                return null;

            return McpTools.RequiredScopes.TryGetValue(name.GetString()!, out var scope) && !grant.HasScope(scope) ? scope : null;
        }
        catch (JsonException)
        {
            return null;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }

    // Every failure here looks the same from outside: 401, no reason
    private async Task<OAuthGrant?> Authenticate(string token, HttpContext context)
    {
        if (token.Length > 4096)
            return null;

        var result = await _handler.ValidateTokenAsync(token, _tokens.AccessTokenValidationParameters);
        if (!result.IsValid || result.SecurityToken is not JsonWebToken jwt)
            return null;

        if (!jwt.Audiences.Any(aud => OAuthResource.AudienceAllowsPath(aud, _env.Origin, context.Request.Path)))
            return null;

        if (!jwt.TryGetClaim("grant_id", out var grantIdClaim) || string.IsNullOrEmpty(grantIdClaim.Value))
            return null;

        var grantId = grantIdClaim.Value;
        var cacheKey = GrantCacheKey(grantId);
        if (!_cache.TryGetValue<OAuthGrant>(cacheKey, out var grant) || grant is null)
        {
            grant = await _store.TouchGrant(grantId, context.RequestAborted);
            if (grant is null)
                return null;

            _cache.Set(cacheKey, grant, TimeSpan.FromSeconds(60));
        }

        // A token that names a different subject or client than its grant was not issued
        // for this grant. Scopes are deliberately not compared: after a re-consent an older
        // token keeps working, with whatever the row says now.
        var subject = grant.IsService ? grant.Id : grant.SubjectId;
        if (jwt.Subject != subject)
            return null;

        if (!jwt.TryGetClaim("client_id", out var clientIdClaim) || clientIdClaim.Value != grant.ClientId)
            return null;

        return grant;
    }

    public static string ChallengeHeader(EnvSettings env, string? error = null, string scope = OAuthScopes.StatsRead)
    {
        var value = $"Bearer resource_metadata=\"{env.Origin}/.well-known/oauth-protected-resource\", scope=\"{scope}\"";
        return error is null ? value : $"{value}, error=\"{error}\"";
    }

    private Task Challenge(HttpContext context, bool invalidToken)
    {
        context.Response.StatusCode = 401;
        context.Response.Headers.WWWAuthenticate = ChallengeHeader(_env, invalidToken ? "invalid_token" : null);
        context.Response.Headers.CacheControl = "no-store";
        return WriteError(context, "unauthorized", "A valid access token is required.");
    }

    private static Task TooManyRequests(HttpContext context, int retryAfterSeconds, string message)
    {
        context.Response.StatusCode = 429;
        context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        return WriteError(context, "rate_limited", message);
    }

    private static Task WriteError(HttpContext context, string code, string message)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { error = code, message }));
    }
}
