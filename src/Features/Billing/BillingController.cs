using Aptabase.Features.Authentication;
using Aptabase.Features.Billing.LemonSqueezy;
using Aptabase.Features.Stats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aptabase.Features.Billing;

[ApiController, IsAuthenticated]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class BillingController : Controller
{
    private readonly IQueryClient _queryClient;
    private readonly IBillingQueries _billingQueries;
    private readonly ISubscriptionSync _subscriptions;
    private readonly LemonSqueezyClient _lsClient;

    public BillingController(IBillingQueries billingQueries, ISubscriptionSync subscriptions, LemonSqueezyClient lsClient, IQueryClient queryClient)
    {
        _billingQueries = billingQueries ?? throw new ArgumentNullException(nameof(billingQueries));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
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
        var plan = await ResolvePlan(user, sub);

        var state = (usage?.Count ?? 0) < plan.MonthlyEvents ? "OK" : "OVERUSE";
        
        return Ok(new
        {
            State = state,
            Usage = usage?.Count ?? 0,
            DateTime.UtcNow.Month,
            DateTime.UtcNow.Year,
            Subscription = sub != null ? new
            {
                sub.Status,
                sub.EndsAt,
            } : null,
            Plan = plan
        });
    }

    private async Task<SubscriptionPlan> ResolvePlan(UserIdentity user, Subscription? sub)
    {
        return sub is null || SubscriptionStatus.IsEnded(sub.Status)
            ? SubscriptionPlan.GetFreeVariant(await _billingQueries.GetUserFreeTierOrTrial(user))
            : SubscriptionPlan.GetByVariantId(sub.VariantId);
    }

    [HttpGet("/api/_billing/historical")]
    public async Task<IActionResult> HistoricalUsage(CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();
        var appIds = await _billingQueries.GetOwnedAppIds(user);

        var rows = await _queryClient.NamedQueryAsync<BillingHistoricUsage>("billing_historical_usage__v1", new
        {
            app_ids = appIds,
        }, cancellationToken);

        return Ok(rows);
    }

    // The customer-facing counterpart of the checks the overuse job and the billing page already do:
    // re-read the subscription from LemonSqueezy, store it, and lift an overuse or trial lock when the
    // current plan has room for this month's usage. Unlocking goes through the same usage check as the
    // job, so clicking it cannot release an account that is genuinely over its plan.
    [HttpPost("/api/_billing/sync")]
    [EnableRateLimiting("BillingSync")]
    public async Task<IActionResult> SyncSubscription(CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUserIdentity();
        var sub = await _billingQueries.GetUserSubscription(user);

        if (sub is null)
        {
            return Ok(new SyncResult("no_subscription", false, null, await _billingQueries.GetLockReason(user.Id)));
        }

        var previousVariantId = sub.VariantId;
        var refreshed = await _subscriptions.RefreshFromLemonSqueezy(sub.Id, user.Id, cancellationToken, unlockOnPlanChange: false);
        if (refreshed is null)
        {
            return Ok(new SyncResult("unavailable", false, null, await _billingQueries.GetLockReason(user.Id)));
        }

        sub = await _billingQueries.GetUserSubscription(user);
        var plan = await ResolvePlan(user, sub);
        var outcome = sub?.VariantId == previousVariantId ? "unchanged" : "updated";

        var appIds = await _billingQueries.GetOwnedAppIds(user);
        var usage = await _queryClient.NamedQuerySingleAsync<BillingUsage>("get_billing_usage__v1", new {
            app_ids = appIds,
        }, cancellationToken);
        var hasRoom = (usage?.Count ?? 0) < plan.MonthlyEvents;
        var hasActiveSubscription = sub is not null && !SubscriptionStatus.IsEnded(sub.Status);

        var unlocked = false;
        var lockReason = await _billingQueries.GetLockReason(user.Id);
        if ((lockReason == 'O' && hasRoom) || (lockReason == 'T' && hasActiveSubscription))
        {
            await _billingQueries.UnlockUser(user.Id);
            unlocked = true;
            lockReason = null;
        }

        return Ok(new SyncResult(outcome, unlocked, plan, lockReason));
    }

    // LockReason is the lock still in place after the sync (null when none), so the UI can say
    // "still paused" instead of a reassuring "up to date" to a customer whose usage is still over.
    public record SyncResult(string Outcome, bool Unlocked, SubscriptionPlan? Plan, char? LockReason)
    {
        public SyncResult(string outcome, bool unlocked, SubscriptionPlan? plan, string? lockReason)
            : this(outcome, unlocked, plan, string.IsNullOrEmpty(lockReason) ? null : lockReason[0]) { }
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
        {
            return NotFound();
        }

        var url = await _lsClient.GetBillingPortalUrl(sub.Id, cancellationToken);

        return Ok(new { url });
    }
}
