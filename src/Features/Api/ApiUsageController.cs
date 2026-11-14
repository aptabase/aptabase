using Aptabase.Features.Authentication;
using Aptabase.Features.OAuth;
using Microsoft.AspNetCore.Mvc;

namespace Aptabase.Features.Api;

/// <summary>
/// API usage as the dashboard shows it. Cookie-authenticated. Anyone who can read an app
/// sees how its API allowance is being spent, because they are the ones spending it;
/// the controls that cost money or change access stay with the owner.
/// </summary>
[ApiController, IsAuthenticated, RequiresQueryApi]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ApiUsageController : Controller
{
    private readonly IApiAccess _access;
    private readonly IApiTiers _tiers;
    private readonly IApiMetering _metering;

    public ApiUsageController(IApiAccess access, IApiTiers tiers, IApiMetering metering)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _tiers = tiers ?? throw new ArgumentNullException(nameof(tiers));
        _metering = metering ?? throw new ArgumentNullException(nameof(metering));
    }

    [HttpGet("/api/_apps/{appId}/api-usage")]
    public async Task<IActionResult> AppUsage(string appId, CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();
        var app = await _access.GetReadableApp(user.Id, appId, cancellationToken);
        if (app is null)
            return StatusCode(403);

        var tier = await _tiers.ResolveAsync(app.OwnerId, cancellationToken);
        var quota = await _metering.PeekAsync(app.OwnerId, tier.MonthlyRequests, cancellationToken);
        var rows = await _metering.GetAppUsage(appId, app.OwnerId, cancellationToken);

        return Ok(new
        {
            isOwner = app.HasOwnership,
            // A number, never the plan name or its price
            allowance = quota.Allowance,
            used = quota.Used,
            remaining = quota.Remaining,
            resetsAt = quota.ResetsAt,
            clients = rows.Select(r => new
            {
                grantId = r.GrantId,
                kind = r.Kind,
                clientName = r.ClientName,
                person = DescribePerson(r, user.Id, app.HasOwnership),
                requests = r.Requests,
                rejected = r.Rejected,
                lastUsedAt = r.LastUsedAt,
                revoked = r.RevokedAt.HasValue,
            }),
        });
    }

    // Collaborator emails stay with the owner, as they do on the sharing page. A shared
    // user sees who is using the API by display name only.
    private static string DescribePerson(AppApiUsageRow row, string viewerId, bool viewerIsOwner)
    {
        if (row.Kind == OAuthGrant.KindService)
            return "Service client";

        if (row.SubjectId == viewerId)
            return "You";

        if (viewerIsOwner)
            return row.SubjectEmail;

        return string.IsNullOrWhiteSpace(row.SubjectName) ? "Another collaborator" : row.SubjectName;
    }

    [HttpGet("/api/_billing/api-usage")]
    public async Task<IActionResult> OwnerUsage(CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();
        var tier = await _tiers.ResolveAsync(user.Id, cancellationToken);
        var quota = await _metering.PeekAsync(user.Id, tier.MonthlyRequests, cancellationToken);
        var rows = await _metering.GetOwnerUsage(user.Id, cancellationToken);

        return Ok(new
        {
            tier = tier.Name,
            allowance = quota.Allowance,
            used = quota.Used,
            remaining = quota.Remaining,
            resetsAt = quota.ResetsAt,
            maxWindowDays = tier.MaxWindowDays,
            apps = rows.Select(r => new { appId = r.AppId, appName = r.AppName, requests = r.Requests, rejected = r.Rejected }),
        });
    }
}
