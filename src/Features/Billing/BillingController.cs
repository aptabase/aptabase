using Aptabase.Features.Authentication;
using Aptabase.Features.Billing.LemonSqueezy;
using Microsoft.AspNetCore.Mvc;
using Aptabase.Data;
using Aptabase.Features.Stats;

namespace Aptabase.Features.Billing;

public class BillingUsage
{
    public long Count { get; set; }
    public long Quota { get; set; }
}

public class BillingHistoricUsage
{
    public DateTime Date { get; set; }
    public long Events { get; set; }
}

[ApiController, IsAuthenticated]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class BillingController : Controller
{
    private readonly IQueryClient _queryClient;
    private readonly IBillingQueries _billingQueries;
    private readonly LemonSqueezyClient _lsClient;

    public BillingController(IDbContext db, IBillingQueries billingQueries, LemonSqueezyClient lsClient, IQueryClient queryClient)
    {
        _billingQueries = billingQueries ?? throw new ArgumentNullException(nameof(billingQueries));
        _lsClient = lsClient ?? throw new ArgumentNullException(nameof(lsClient));
        _queryClient = queryClient ?? throw new ArgumentNullException(nameof(queryClient));
    }

    [HttpGet("/api/_billing")]
    public async Task<IActionResult> BillingState(CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();
        var appIds = await _billingQueries.GetOwnedAppIds(user);

        var usage = await _queryClient.NamedQuerySingleAsync<BillingUsage>("get_billing_usage__v1", new {
            app_ids = appIds,
        }, cancellationToken);

        var sub = await _billingQueries.GetUserSubscription(user);
        var plan = sub is null || sub.Status == "expired" 
            ? SubscriptionPlan.GetFreeVariant(await _billingQueries.GetUserFreeTierOrTrial(user))
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

    [HttpGet("/api/_billing/historical")]
    public async Task<IActionResult> HistoricalUsage(CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();
        var appIds = await _billingQueries.GetOwnedAppIds(user);

        var rows = await _queryClient.NamedQueryAsync<BillingHistoricUsage>("billing_historical_usage__v1", new {
            app_ids = appIds,
        }, cancellationToken);

        return Ok(rows);
    }

    [HttpPost("/api/_billing/checkout")]
    public async Task<IActionResult> GenerateCheckoutUrl(CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();
        var url = await _lsClient.CreateCheckout(user, cancellationToken);

        return Ok(new { url });
    }

    [HttpPost("/api/_billing/portal")]
    public async Task<IActionResult> GeneratePortalUrl(CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();
        var sub = await _billingQueries.GetUserSubscription(user);
        if (sub is null)
            return NotFound();

        var url = await _lsClient.GetBillingPortalUrl(sub.Id, cancellationToken);
        return Ok(new { url });
    }
}
