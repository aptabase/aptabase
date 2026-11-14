using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aptabase.Features.OAuth;

/// <summary>
/// The kill switch. With QUERY_API_ENABLED off, every route of the query API answers 404
/// as if it was never deployed.
/// </summary>
public class RequiresQueryApiAttribute : ActionFilterAttribute
{
    // Ahead of every other filter, so a switched-off route is a 404 and not, say, the 401
    // of an authentication check that ran first
    public RequiresQueryApiAttribute()
    {
        Order = int.MinValue;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var env = context.HttpContext.RequestServices.GetRequiredService<EnvSettings>();
        if (!env.QueryApiEnabled)
            context.Result = new NotFoundResult();
    }
}

/// <summary>
/// Limits failed token exchanges. Every secret involved is 256 bits of randomness, so this
/// protects cost and log noise, not the secrets. In memory per instance, which is enough
/// for that purpose.
/// </summary>
public class TokenFailureLimiter
{
    private const int PerClientPerMinute = 20;
    private const int GlobalPerMinute = 600;

    private readonly ConcurrentDictionary<string, (long Window, int Count)> _counts = new();

    private static long CurrentWindow => DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;

    public bool IsBlocked(string clientId)
        => Count("*") >= GlobalPerMinute || Count(Key(clientId)) >= PerClientPerMinute;

    public void RecordFailure(string clientId)
    {
        var window = CurrentWindow;
        foreach (var key in new[] { "*", Key(clientId) })
            _counts.AddOrUpdate(key, _ => (window, 1), (_, c) => c.Window == window ? (window, c.Count + 1) : (window, 1));

        if (_counts.Count > 50_000)
        {
            foreach (var entry in _counts.Where(e => e.Value.Window != window).ToArray())
                _counts.TryRemove(entry.Key, out _);
        }
    }

    private int Count(string key) => _counts.TryGetValue(key, out var c) && c.Window == CurrentWindow ? c.Count : 0;

    // Client ids are caller-supplied, so the key is bounded
    private static string Key(string clientId) => clientId.Length <= 64 ? $"c:{clientId}" : $"h:{OAuthCrypto.Sha256Hex(clientId)}";
}

[ApiController, RequiresQueryApi]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class OAuthController : Controller
{
    public const string PublicCorsPolicy = "QueryApiPublic";

    private readonly EnvSettings _env;
    private readonly IOAuthStore _store;
    private readonly IOAuthTokenService _tokens;
    private readonly TokenFailureLimiter _failures;
    private readonly ILogger _logger;

    public OAuthController(EnvSettings env, IOAuthStore store, IOAuthTokenService tokens, TokenFailureLimiter failures, ILogger<OAuthController> logger)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _failures = failures ?? throw new ArgumentNullException(nameof(failures));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Issuer and endpoints come from BASE_URL, never from the Host header of the request
    [HttpGet("/.well-known/oauth-authorization-server")]
    [EnableCors(PublicCorsPolicy)]
    public IActionResult AuthorizationServerMetadata()
    {
        return Json(new Dictionary<string, object>
        {
            ["issuer"] = _env.Origin,
            ["authorization_endpoint"] = $"{_env.Origin}/oauth/authorize",
            ["token_endpoint"] = $"{_env.Origin}/oauth/token",
            ["response_types_supported"] = new[] { "code" },
            ["grant_types_supported"] = new[] { "authorization_code", "refresh_token", "client_credentials" },
            ["code_challenge_methods_supported"] = new[] { "S256" },
            ["token_endpoint_auth_methods_supported"] = new[] { "none", "client_secret_post", "client_secret_basic" },
            ["scopes_supported"] = OAuthScopes.All,
            ["client_id_metadata_document_supported"] = true,
            ["authorization_response_iss_parameter_supported"] = true,
        });
    }

    // One document for the origin covers both /mcp and /api/v1. sessions:read is left out
    // on purpose: clients ask for what is listed here, and session data comes through step-up.
    [HttpGet("/.well-known/oauth-protected-resource")]
    [EnableCors(PublicCorsPolicy)]
    public IActionResult ProtectedResourceMetadata()
    {
        return Json(new Dictionary<string, object>
        {
            ["resource"] = _env.Origin,
            ["authorization_servers"] = new[] { _env.Origin },
            ["scopes_supported"] = new[] { OAuthScopes.StatsRead },
            ["bearer_methods_supported"] = new[] { "header" },
            ["resource_name"] = "Aptabase",
        });
    }

    // Validates the shape of the request and nothing else. Nothing is fetched here, because
    // this endpoint is anonymous and a fetch would make it a free outbound-request oracle.
    // Every failure is a first-party page: the redirect URI has not been checked against
    // the client's own document yet, so sending an error there would be an open redirect.
    [HttpGet("/oauth/authorize")]
    public IActionResult Authorize(
        [FromQuery(Name = "response_type")] string? responseType,
        [FromQuery(Name = "client_id")] string? clientId,
        [FromQuery(Name = "redirect_uri")] string? redirectUri,
        [FromQuery(Name = "code_challenge")] string? codeChallenge,
        [FromQuery(Name = "code_challenge_method")] string? codeChallengeMethod,
        [FromQuery] string? scope,
        [FromQuery] string? resource,
        [FromQuery] string? state)
    {
        if (responseType != "code")
            return AuthorizeError("This authorization request is not supported. Only the authorization code flow is available.");

        if (string.IsNullOrEmpty(clientId) || (OAuthClients.FindBuiltIn(clientId) is null && !OAuthClients.IsMetadataDocumentUrl(clientId, out _)))
            return AuthorizeError("The application that sent you here did not identify itself in a way Aptabase accepts.");

        if (string.IsNullOrEmpty(redirectUri) || !OAuthClients.IsAllowedRedirectUri(redirectUri))
            return AuthorizeError("The application that sent you here asked to be called back at an address Aptabase does not allow.");

        if (codeChallengeMethod != "S256" || !OAuthCrypto.IsValidCodeChallenge(codeChallenge))
            return AuthorizeError("The application that sent you here did not include a valid PKCE challenge (S256 is required).");

        if (string.IsNullOrWhiteSpace(scope))
            scope = OAuthScopes.StatsRead;

        if (!OAuthScopes.TryParse(scope, out var scopes) || scopes.Length == 0)
            return AuthorizeError("The application that sent you here asked for a permission Aptabase does not know.");

        var normalizedResource = string.IsNullOrEmpty(resource) ? _env.Origin : OAuthResource.Normalize(resource);
        if (normalizedResource is null || !OAuthResource.IsAcceptedResource(normalizedResource, _env.Origin))
            return AuthorizeError("The application that sent you here asked for access to a different server than this one.");

        if (state is { Length: > 1024 })
            return AuthorizeError("This authorization request is too large.");

        var request = new ConsentRequest(clientId, redirectUri, codeChallenge!, scopes, normalizedResource, state);
        ConsentCookie.Set(HttpContext, _tokens.ProtectConsentRequest(request));

        return Redirect($"{_env.SelfBaseUrl}/oauth/consent");
    }

    private ContentResult AuthorizeError(string message)
    {
        var html = $@"<!doctype html>
<html lang=""en"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>Aptabase · Authorization failed</title>
<style>
body {{ font-family: system-ui, -apple-system, sans-serif; background: #fafafa; color: #18181b; display: flex; min-height: 100vh; margin: 0; align-items: center; justify-content: center; }}
main {{ max-width: 28rem; padding: 2rem; }}
h1 {{ font-size: 1.25rem; }}
p {{ color: #52525b; line-height: 1.5; }}
</style>
</head>
<body>
<main>
<h1>This authorization request cannot continue</h1>
<p>{WebUtility.HtmlEncode(message)}</p>
<p>Nothing was shared. You can close this window and try again from the application.</p>
</main>
</body>
</html>";

        Response.Headers["X-Frame-Options"] = "DENY";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'";
        return new ContentResult { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = 400 };
    }

    [HttpPost("/oauth/token")]
    [EnableCors(PublicCorsPolicy)]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token(CancellationToken cancellationToken)
    {
        var form = await Request.ReadFormAsync(cancellationToken);
        string Field(string name) => form.TryGetValue(name, out var value) ? value.ToString() : "";

        var (clientId, clientSecret) = ReadClientCredentials(Field("client_id"), Field("client_secret"));
        var limiterKey = clientId.Length > 0 ? clientId : HttpContext.ResolveClientIpAddress();

        if (_failures.IsBlocked(limiterKey))
        {
            Response.Headers.RetryAfter = "60";
            return StatusCode(429, new { error = "slow_down", error_description = "Too many failed token requests. Try again in a minute." });
        }

        var result = Field("grant_type") switch
        {
            "authorization_code" => await ExchangeCode(clientId, Field("code"), Field("code_verifier"), Field("redirect_uri"), Field("resource"), cancellationToken),
            "refresh_token" => await Refresh(clientId, Field("refresh_token"), cancellationToken),
            "client_credentials" => await ClientCredentials(clientId, clientSecret, Field("resource"), cancellationToken),
            _ => TokenError(400, "unsupported_grant_type", "Supported grant types are authorization_code, refresh_token and client_credentials."),
        };

        if (result is ObjectResult { StatusCode: >= 400 })
            _failures.RecordFailure(limiterKey);

        return result;
    }

    private (string ClientId, string ClientSecret) ReadClientCredentials(string formClientId, string formClientSecret)
    {
        if (AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header)
            && string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(header.Parameter))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
                var separator = decoded.IndexOf(':');
                if (separator > 0)
                    return (Uri.UnescapeDataString(decoded[..separator]), Uri.UnescapeDataString(decoded[(separator + 1)..]));
            }
            catch (FormatException)
            {
            }
        }

        return (formClientId, formClientSecret);
    }

    // Any failure answers the same invalid_grant, so a caller learns nothing about which
    // check a code failed. The code is gone after the first attempt either way.
    private async Task<IActionResult> ExchangeCode(string clientId, string code, string codeVerifier, string redirectUri, string resource, CancellationToken cancellationToken)
    {
        if (code.Length == 0 || code.Length > 200)
            return InvalidGrant();

        var stored = await _store.ConsumeAuthorizationCode(code, cancellationToken);
        if (stored is null)
            return InvalidGrant();

        var requestedResource = resource.Length == 0 ? stored.Resource : OAuthResource.Normalize(resource);

        if (!string.Equals(stored.ClientId, clientId, StringComparison.Ordinal)
            || !string.Equals(stored.RedirectUri, redirectUri, StringComparison.Ordinal)
            || !string.Equals(stored.Resource, requestedResource, StringComparison.Ordinal)
            || stored.CodeChallenge is null
            || !OAuthCrypto.IsValidCodeVerifier(codeVerifier)
            || !OAuthCrypto.VerifyS256(codeVerifier, stored.CodeChallenge))
            return InvalidGrant();

        var grant = await _store.GetGrant(stored.GrantId, cancellationToken);
        if (grant is null || grant.IsRevoked || grant.IsService)
            return InvalidGrant();

        var refreshToken = await _store.CreateRefreshToken(grant.Id, stored.ClientId, stored.Resource, cancellationToken);
        return TokenResponse(grant, stored.Resource, refreshToken);
    }

    private async Task<IActionResult> Refresh(string clientId, string refreshToken, CancellationToken cancellationToken)
    {
        if (refreshToken.Length == 0 || refreshToken.Length > 200 || clientId.Length == 0)
            return InvalidGrant();

        var outcome = await _store.RotateRefreshToken(refreshToken, clientId, cancellationToken);
        if (outcome is null || outcome.Grant.IsRevoked)
            return InvalidGrant();

        return TokenResponse(outcome.Grant, outcome.Resource, outcome.RefreshToken);
    }

    // A hash that no secret produces, compared when the client does not exist, so that an
    // unknown client id and a wrong secret take the same path and answer the same way.
    private static readonly string NoSecretHash = OAuthCrypto.Sha256Hex(OAuthCrypto.RandomToken());

    private async Task<IActionResult> ClientCredentials(string clientId, string clientSecret, string resource, CancellationToken cancellationToken)
    {
        var grant = clientId.Length is > 0 and <= 22 ? await _store.GetGrant(clientId, cancellationToken) : null;
        var usable = grant is { IsService: true, IsRevoked: false, SecretHash: not null };

        var presented = OAuthCrypto.Sha256Hex(clientSecret.Length <= 200 ? clientSecret : "");
        var matches = OAuthCrypto.FixedTimeEquals(presented, usable ? grant!.SecretHash! : NoSecretHash);

        if (!usable || !matches)
        {
            Response.Headers.WWWAuthenticate = "Basic realm=\"aptabase\"";
            return TokenError(401, "invalid_client", "Client authentication failed.");
        }

        var normalizedResource = resource.Length == 0 ? _env.Origin : OAuthResource.Normalize(resource);
        if (normalizedResource is null || !OAuthResource.IsAcceptedResource(normalizedResource, _env.Origin))
            return TokenError(400, "invalid_target", "The requested resource is not served by this server.");

        return TokenResponse(grant!, normalizedResource, null);
    }

    private IActionResult TokenResponse(OAuthGrant grant, string resource, string? refreshToken)
    {
        var body = new Dictionary<string, object>
        {
            ["access_token"] = _tokens.CreateAccessToken(grant, resource),
            ["token_type"] = "Bearer",
            ["expires_in"] = IOAuthTokenService.AccessTokenLifetimeSeconds,
            ["scope"] = grant.Scopes,
        };

        if (refreshToken is not null)
            body["refresh_token"] = refreshToken;

        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return Json(body);
    }

    private IActionResult InvalidGrant() => TokenError(400, "invalid_grant", "The code or refresh token is invalid, expired or already used.");

    private IActionResult TokenError(int status, string error, string description)
    {
        Response.Headers.CacheControl = "no-store";
        return StatusCode(status, new Dictionary<string, string> { ["error"] = error, ["error_description"] = description });
    }
}

public static class ConsentCookie
{
    // Lax, not Strict: the user arrives on /oauth/authorize from the client and comes back
    // from GitHub, Google or a magic link in webmail, all top-level cross-site navigations
    // that a Strict cookie would not be sent on.
    public static void Set(HttpContext context, string value)
    {
        context.Response.Cookies.Append(IOAuthTokenService.ConsentCookieName, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/",
            MaxAge = IOAuthTokenService.ConsentLifetime,
        });
    }

    public static void Clear(HttpContext context)
    {
        context.Response.Cookies.Delete(IOAuthTokenService.ConsentCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
        });
    }

    public static ConsentRequest? Read(HttpContext context)
    {
        var env = context.RequestServices.GetRequiredService<EnvSettings>();
        if (!env.QueryApiEnabled)
            return null;

        if (!context.Request.Cookies.TryGetValue(IOAuthTokenService.ConsentCookieName, out var value))
            return null;

        return context.RequestServices.GetRequiredService<IOAuthTokenService>().UnprotectConsentRequest(value);
    }

    /// <summary>
    /// Where a completed sign-in lands. There is no caller-supplied return path anywhere:
    /// a pending consent in this browser leads to the fixed consent page, everything else home.
    /// </summary>
    public static string PostSignInUrl(HttpContext context)
    {
        var env = context.RequestServices.GetRequiredService<EnvSettings>();
        return Read(context) is null ? $"{env.SelfBaseUrl}/" : $"{env.SelfBaseUrl}/oauth/consent";
    }
}
