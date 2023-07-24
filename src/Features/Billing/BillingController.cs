using System.Data;
using Aptabase.Features.Authentication;
using Aptabase.Features.Billing.LemonSqueezy;
using Aptabase.Features.Query;
using Microsoft.AspNetCore.Mvc;
using Aptabase.Data;
using Dapper;

namespace Aptabase.Features.Billing;

public class BillingUsage
{
    public long Count { get; set; }
    public long Quota { get; set; }
}

[ApiController, IsAuthenticated]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class BillingController : Controller
{
    private readonly IQueryClient _queryClient;
    private readonly IDbContext _db;
    private readonly LemonSqueezyClient _lsClient;

    public BillingController(IDbContext db, LemonSqueezyClient lsClient, IQueryClient queryClient)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _lsClient = lsClient ?? throw new ArgumentNullException(nameof(lsClient));
        _queryClient = queryClient ?? throw new ArgumentNullException(nameof(queryClient));
    }

    [HttpGet("/api/_billing")]
    public async Task<IActionResult> BillingState(CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUser();
        var releaseAppIds = await _db.Connection.QueryAsync<string>(@"SELECT id FROM apps WHERE owner_id = @userId", new { userId = user.Id });
        var debugAppIds = releaseAppIds.Select(id => $"{id}_DEBUG");
        var appIds = releaseAppIds.Concat(debugAppIds).ToArray();

        var usage = await _queryClient.NamedQuerySingleAsync<BillingUsage>("get_billing_usage", new {
            app_ids = appIds,
            year = DateTime.UtcNow.Year,
            month = DateTime.UtcNow.Month
        }, cancellationToken);

        var sub = await GetUserSubscription(user);
        var plan = sub is null || sub.Status == "expired" 
            ? SubscriptionPlan.AptabaseFree
            : SubscriptionPlan.GetByVariantId(sub.VariantId);
        var state = (usage?.Count ?? 0) < plan.MonthlyEvents ? "OK" : "OVERUSE";
        
        return Ok(new {
            State = state,
            Usage = usage?.Count ?? 0,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year,
            Subscription = sub != null ? new {
                Status = sub.Status,
                EndsAt = sub.EndsAt,
            } : null,
            Plan = plan
        });
    }

    [HttpPost("/api/_billing/checkout")]
    public async Task<IActionResult> GenerateCheckoutUrl(CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUser();
        var url = await _lsClient.CreateCheckout(user, cancellationToken);

        return Ok(new { url });
    }

    private async Task<Subscription?> GetUserSubscription(UserAccount user)
    {
        return await _db.Connection.QueryFirstOrDefaultAsync<Subscription>(
            @"SELECT * FROM subscriptions 
              WHERE owner_id = @userId
              ORDER BY created_at DESC LIMIT 1",
            new { userId = user.Id });
    }
}
