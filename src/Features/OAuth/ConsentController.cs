using Aptabase.Features.Api;
using Aptabase.Features.Apps;
using Aptabase.Features.Authentication;
using Aptabase.Features.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Aptabase.Features.OAuth;

public class ConsentDecisionBody
{
    // The id GET /api/_oauth/consent returned for the request this decision is about
    public string RequestId { get; set; } = "";

    public bool Allow { get; set; }
    public string[] Scopes { get; set; } = [];

    // Null means every app the user can read, now and in the future
    public string[]? AppIds { get; set; }
}

public class CreateServiceClientBody
{
    public string Name { get; set; } = "";
    public string[] Scopes { get; set; } = [];
}

/// <summary>
/// The first-party half of the authorization server: consent, connected apps and service
/// clients. Cookie-authenticated and never on a permissive CORS policy.
/// </summary>
[ApiController, IsAuthenticated, RequiresQueryApi]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ConsentController : Controller
{
    private readonly EnvSettings _env;
    private readonly IOAuthStore _store;
    private readonly IClientResolver _clients;
    private readonly IApiAccess _access;
    private readonly IApiTiers _tiers;
    private readonly IAppQueries _apps;
    private readonly IAuthService _auth;
    private readonly IEmailClient _email;
    private readonly ILogger _logger;

    public ConsentController(
        EnvSettings env,
        IOAuthStore store,
        IClientResolver clients,
        IApiAccess access,
        IApiTiers tiers,
        IAppQueries apps,
        IAuthService auth,
        IEmailClient email,
        ILogger<ConsentController> logger)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _clients = clients ?? throw new ArgumentNullException(nameof(clients));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _tiers = tiers ?? throw new ArgumentNullException(nameof(tiers));
        _apps = apps ?? throw new ArgumentNullException(nameof(apps));
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        _email = email ?? throw new ArgumentNullException(nameof(email));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // The client's metadata document is fetched here, behind a signed-in session, and the
    // redirect URI of the request is checked against it. A failure is still a first-party
    // error: nothing has been verified that would make a redirect safe.
    [HttpGet("/api/_oauth/consent")]
    public async Task<IActionResult> GetConsent(CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();

        var request = ConsentCookie.Read(HttpContext);
        if (request is null)
            return BadRequest(new { error = "no_request", message = "There is no authorization request waiting in this browser. Start again from the application you want to connect." });

        var (client, error) = await ResolveClient(request, cancellationToken);
        if (client is null)
            return BadRequest(new { error = "client", message = error });

        var apps = await _access.ListReadableApps(user.Id, cancellationToken);
        var current = await _store.FindUserGrant(user.Id, request.ClientId, cancellationToken);

        // What the page shows is what the grant will hold: what the client asks for now,
        // plus what the user already gave it. Never something the user cannot see.
        var scopes = OAuthScopes.All.Where(s => request.Scopes.Contains(s) || (current?.HasScope(s) ?? false)).ToArray();

        return Ok(new
        {
            requestId = ConsentCookie.RequestId(HttpContext),
            client = new
            {
                name = client.Name,
                url = client.Url,
                host = new Uri(client.Url).Host,
                verified = client.Verified,
            },
            scopes,
            requestedScopes = request.Scopes,
            apps = apps.Select(a => new { id = a.Id, name = a.Name, iconPath = a.IconPath, hasOwnership = a.HasOwnership }),
            current = current is null ? null : new
            {
                scopes = current.ScopeList,
                allApps = current.AppIds is null,
                appIds = current.AppIds?.Where(id => apps.Any(a => a.Id == id)).ToArray() ?? [],
            },
        });
    }

    [HttpPost("/api/_oauth/consent")]
    public async Task<IActionResult> PostConsent([FromBody] ConsentDecisionBody body, CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();

        var request = ConsentCookie.Read(HttpContext);
        if (request is null)
            return BadRequest(new { error = "no_request", message = "This authorization request has expired. Start again from the application you want to connect." });

        // Allow and Deny alike apply to the request that was on screen, or to nothing
        var currentId = ConsentCookie.RequestId(HttpContext);
        if (currentId is null || body.RequestId.Length != currentId.Length || !OAuthCrypto.FixedTimeEquals(body.RequestId, currentId))
            return Conflict(new { error = "request_changed", message = "Another authorization request was started in this browser after this page was opened. Reload the page to see the request you would be deciding on." });

        var (client, error) = await ResolveClient(request, cancellationToken);
        if (client is null)
            return BadRequest(new { error = "client", message = error });

        // From here on the redirect URI is verified, so answers may travel to it
        if (!body.Allow)
        {
            ConsentCookie.Clear(HttpContext);
            return Ok(new { redirectUrl = BuildRedirect(request, new() { ["error"] = "access_denied" }) });
        }

        var current = await _store.FindUserGrant(user.Id, request.ClientId, cancellationToken);
        var offered = OAuthScopes.All.Where(s => request.Scopes.Contains(s) || (current?.HasScope(s) ?? false)).ToArray();
        var scopes = body.Scopes.Distinct().ToArray();
        if (scopes.Length == 0 || scopes.Any(s => !offered.Contains(s)))
            return BadRequest(new { error = "scopes", message = "Pick at least one of the permissions shown." });

        // Every chosen app is checked against what the signed-in user can read right now.
        // The page only offers those, but the page is not what enforces it.
        string[]? appIds = null;
        if (body.AppIds is not null)
        {
            appIds = body.AppIds.Distinct().ToArray();
            var readable = await _access.ListReadableApps(user.Id, cancellationToken);
            if (appIds.Length == 0 || appIds.Length > 500 || appIds.Any(id => readable.All(a => a.Id != id)))
                return BadRequest(new { error = "apps", message = "Pick at least one app you have access to." });
        }

        var (grant, created) = await _store.UpsertUserGrant(user.Id, client.ClientId, client.Name, scopes, appIds, cancellationToken);
        var code = await _store.CreateAuthorizationCode(grant.Id, request, cancellationToken);

        _logger.LogInformation("Grant {GrantId} {Action} for client {ClientId} by user {UserId}", grant.Id, created ? "created" : "replaced", client.ClientId, user.Id);

        if (created)
            await NotifyNewGrant(user, client, scopes, cancellationToken);

        ConsentCookie.Clear(HttpContext);
        return Ok(new { redirectUrl = BuildRedirect(request, new() { ["code"] = code }) });
    }

    private async Task<(ResolvedClient? Client, string Error)> ResolveClient(ConsentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _clients.ResolveAsync(request.ClientId, forceRefresh: false, cancellationToken);

            // A client may have added a redirect URI since its document was cached
            if (!OAuthClients.RedirectUriMatches(client.RedirectUris, request.RedirectUri))
                client = await _clients.ResolveAsync(request.ClientId, forceRefresh: true, cancellationToken);

            if (!OAuthClients.RedirectUriMatches(client.RedirectUris, request.RedirectUri))
                return (null, "The application asked to be called back at an address it has not registered. Nothing was shared.");

            return (client, "");
        }
        catch (ClientMetadataException ex)
        {
            _logger.LogWarning("Could not resolve OAuth client {ClientId}: {Reason}", request.ClientId, ex.Message);
            return (null, $"Aptabase could not verify the application that sent you here. {ex.Message}");
        }
    }

    // state goes back untouched, and iss tells the client which server answered (RFC 9207)
    private string BuildRedirect(ConsentRequest request, Dictionary<string, string?> parameters)
    {
        if (!string.IsNullOrEmpty(request.State))
            parameters["state"] = request.State;

        parameters["iss"] = _env.Origin;
        return QueryHelpers.AddQueryString(request.RedirectUri, parameters);
    }

    // The one email of the query API. It is the user's chance to notice a consent they
    // were tricked into, so a failure to send must not undo the grant, only be logged.
    private async Task NotifyNewGrant(UserIdentity user, ResolvedClient client, string[] scopes, CancellationToken cancellationToken)
    {
        try
        {
            // The subject is fixed text: the client name is chosen by a third party, and the
            // subject is placed in the message without the encoding the body values get
            await _email.SendEmailAsync(user.Email, "A new application was connected to your Aptabase account", "NewGrant", new()
            {
                { "name", user.Name },
                { "client", client.Name },
                { "clienturl", client.Url },
                { "verified", client.Verified ? "Verified by Aptabase" : "Not verified by Aptabase" },
                { "scopes", string.Join(", ", scopes.Select(DescribeScope)) },
                { "url", $"{_env.SelfBaseUrl}/connected-apps" },
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Could not send the new grant email to user {UserId}", user.Id);
        }
    }

    private static string DescribeScope(string scope) => scope switch
    {
        OAuthScopes.StatsRead => "read aggregated statistics",
        OAuthScopes.SessionsRead => "read individual sessions and events",
        _ => scope,
    };

    [HttpGet("/api/_oauth/grants")]
    public async Task<IActionResult> ListGrants(CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();
        var grants = await _store.ListUserGrants(user.Id, cancellationToken);
        var apps = await _access.ListReadableApps(user.Id, cancellationToken);

        return Ok(grants.Select(g => new
        {
            id = g.Id,
            clientName = g.ClientName,
            clientUrl = OAuthClients.FindBuiltIn(g.ClientId)?.Url ?? g.ClientId,
            verified = OAuthClients.FindBuiltIn(g.ClientId) is not null || OAuthClients.IsVerifiedMetadataUrl(g.ClientId),
            scopes = g.ScopeList,
            allApps = g.AppIds is null,
            // Only apps the user can still read are named. An app whose share was removed
            // must not keep showing up here.
            apps = (g.AppIds ?? []).Select(id => apps.FirstOrDefault(a => a.Id == id)).Where(a => a is not null).Select(a => new { id = a!.Id, name = a.Name }),
            consentedAt = g.ConsentedAt,
            lastUsedAt = g.LastUsedAt,
            revokedAt = g.RevokedAt,
            revokedReason = g.RevokedReason,
        }));
    }

    [HttpDelete("/api/_oauth/grants/{grantId}")]
    public async Task<IActionResult> RevokeGrant(string grantId, CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();
        var grant = await _store.GetGrant(grantId, cancellationToken);
        if (grant is null || grant.IsService || grant.SubjectId != user.Id)
            return NotFound();

        await _store.RevokeGrant(grant.Id, RevokedReason.User, cancellationToken);
        return Ok(new { });
    }

    [HttpGet("/api/_apps/{appId}/clients")]
    public async Task<IActionResult> ListServiceClients(string appId, CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();
        if (await _apps.GetOwnedAppAsync(appId, user.Id) is null)
            return NotFound();

        var tier = await _tiers.ResolveAsync(user.Id, cancellationToken);
        var grants = await _store.ListServiceGrants(appId, cancellationToken);

        return Ok(new
        {
            limit = tier.MaxServiceClients,
            clients = grants.Select(g => new
            {
                clientId = g.Id,
                name = g.ClientName,
                scopes = g.ScopeList,
                secretPrefix = g.SecretPrefix,
                createdAt = g.CreatedAt,
                lastUsedAt = g.LastUsedAt,
                deniedCount = g.DeniedAt > DateTime.UtcNow.AddMinutes(-OAuthStore.DeniedWindowMinutes) ? g.DeniedCount : 0,
                revokedAt = g.RevokedAt,
                revokedReason = g.RevokedReason,
            }),
        });
    }

    // A service client covers exactly one app and is bound to the owner who creates it.
    // The secret is shown once and only its hash is kept.
    [HttpPost("/api/_apps/{appId}/clients")]
    public async Task<IActionResult> CreateServiceClient(string appId, [FromBody] CreateServiceClientBody body, CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();
        if (await _apps.GetOwnedAppAsync(appId, user.Id) is null)
            return NotFound();

        var name = body.Name.Trim();
        if (name.Length is < 2 or > 80)
            return BadRequest(new { error = "name", message = "The name must be between 2 and 80 characters." });

        var scopes = body.Scopes.Distinct().ToArray();
        if (scopes.Length == 0 || scopes.Any(s => !OAuthScopes.All.Contains(s)))
            return BadRequest(new { error = "scopes", message = "Pick at least one permission." });

        var tier = await _tiers.ResolveAsync(user.Id, cancellationToken);
        if (tier.MaxServiceClients.HasValue && await _store.CountLiveServiceGrants(appId, cancellationToken) >= tier.MaxServiceClients.Value)
            return StatusCode(409, new { error = "limit", message = $"Your plan allows {tier.MaxServiceClients} service client(s) per app. Revoke one or upgrade your plan." });

        var secret = NewSecret();
        var grant = await _store.CreateServiceGrant(user.Id, appId, name, scopes, OAuthCrypto.Sha256Hex(secret), secret[..12], cancellationToken);

        _logger.LogInformation("Service client {GrantId} created for app {AppId} by user {UserId}", grant.Id, appId, user.Id);
        return Ok(new { clientId = grant.Id, clientSecret = secret });
    }

    [HttpPost("/api/_apps/{appId}/clients/{clientId}/rotate")]
    public async Task<IActionResult> RotateServiceClient(string appId, string clientId, CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();
        if (await _apps.GetOwnedAppAsync(appId, user.Id) is null)
            return NotFound();

        var secret = NewSecret();
        if (!await _store.RotateServiceSecret(clientId, appId, OAuthCrypto.Sha256Hex(secret), secret[..12], cancellationToken))
            return NotFound();

        return Ok(new { clientId, clientSecret = secret });
    }

    [HttpDelete("/api/_apps/{appId}/clients/{clientId}")]
    public async Task<IActionResult> RevokeServiceClient(string appId, string clientId, CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();
        if (await _apps.GetOwnedAppAsync(appId, user.Id) is null)
            return NotFound();

        var grant = await _store.GetGrant(clientId, cancellationToken);
        if (grant is null || !grant.IsService || grant.AppIds is not [var grantAppId] || grantAppId != appId)
            return NotFound();

        await _store.RevokeGrant(grant.Id, RevokedReason.Owner, cancellationToken);
        return Ok(new { });
    }

    private static string NewSecret() => $"aptsk_{OAuthCrypto.RandomToken()}";
}
