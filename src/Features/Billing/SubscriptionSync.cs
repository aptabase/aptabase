using Aptabase.Data;
using Aptabase.Features.Billing.LemonSqueezy;
using Dapper;

namespace Aptabase.Features.Billing;

/// <summary>
/// Applies the state of a LemonSqueezy subscription to our database and to the owner's lock.
/// The webhook feeds it as events arrive; the overuse job and the billing page feed it with a
/// fresh read from the LemonSqueezy API, so a missed or rejected webhook cannot leave a paying
/// customer on a stale, smaller plan.
/// </summary>
public interface ISubscriptionSync
{
    Task<string?> FindOwnerId(long subscriptionId);
    Task Apply(long subscriptionId, string ownerId, SubscriptionAttributes sub, bool unlock = true);
    Task<SubscriptionAttributes?> RefreshFromLemonSqueezy(long subscriptionId, string ownerId, CancellationToken cancellationToken, bool unlockOnPlanChange = true);
    Task<ReconciliationSummary> ReconcileAll(CancellationToken cancellationToken);
}

public record ReconciliationSummary(int Stored, int Fetched, int Changed, int Missing);

public class StoredSubscription
{
    public long Id { get; set; }
    public string OwnerId { get; set; } = "";
    public long VariantId { get; set; }
    public string Status { get; set; } = "";
    public DateTime? EndsAt { get; set; }

    public bool Differs(SubscriptionAttributes sub) =>
        VariantId != sub.VariantID
        || !string.Equals(Status, sub.Status, StringComparison.Ordinal)
        || !NullableUtcEquals(EndsAt, sub.EndsAt);

    private static bool NullableUtcEquals(DateTime? a, DateTime? b)
    {
        if (a is null || b is null) return a is null && b is null;
        return a.Value.ToUniversalTime() == b.Value.ToUniversalTime();
    }
}

public class SubscriptionSync : ISubscriptionSync
{
    private readonly IDbContext _db;
    private readonly LemonSqueezyClient _lsClient;
    private readonly ILogger _logger;

    public SubscriptionSync(IDbContext db, LemonSqueezyClient lsClient, ILogger<SubscriptionSync> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _lsClient = lsClient ?? throw new ArgumentNullException(nameof(lsClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string?> FindOwnerId(long subscriptionId)
    {
        return await _db.Connection.QueryFirstOrDefaultAsync<string>(
            @"SELECT owner_id FROM subscriptions WHERE id = @subscriptionId",
            new { subscriptionId });
    }

    public async Task Apply(long subscriptionId, string ownerId, SubscriptionAttributes sub, bool unlock = true)
    {
        // A cancellation or expiry also arrives as subscription_updated and used to unlock the
        // owner like any other change, which let an over-quota account resume ingestion by
        // cancelling its subscription. Those two statuses leave the lock alone.
        if (unlock && SubscriptionStatus.ClearsLock(sub.Status))
        {
            await _db.Connection.ExecuteAsync(
                @"UPDATE users SET lock_reason = null WHERE id = @ownerId",
                new { ownerId }
            );
        }
        else
        {
            _logger.LogInformation("Subscription {SubId} for user {UserId} is now {Status}; lock left unchanged.", subscriptionId, ownerId, sub.Status);
        }

        if (SubscriptionStatus.IsEnded(sub.Status))
        {
            await _db.Connection.ExecuteAsync(
                @"DELETE FROM subscriptions WHERE id = @subscriptionId",
                new { subscriptionId });
            return;
        }

        await _db.Connection.ExecuteAsync(
            @"INSERT INTO subscriptions
                (id, owner_id, customer_id, product_id, variant_id, status, ends_at)
            VALUES
                (@subscriptionId, @ownerId, @customerId, @productId, @variantId, @status, @endsAt)
            ON CONFLICT (id)
            DO UPDATE SET product_id = @productId,
                        variant_id = @variantId,
                        status = @status,
                        ends_at = @endsAt,
                        modified_at = now()",
            new
            {
                subscriptionId,
                ownerId,
                customerId = sub.CustomerID,
                productId = sub.ProductID,
                variantId = sub.VariantID,
                status = sub.Status,
                endsAt = sub.EndsAt,
            });
    }

    /// <summary>
    /// Pulls the whole store from LemonSqueezy in pages and rewrites every stored subscription whose
    /// plan, status or end date differs. It never touches locks: the hourly overuse job releases
    /// locked subscribers whose (now corrected) plan has room, with the usage numbers it already has.
    /// Subscriptions LemonSqueezy returns that this region does not store belong to the other region
    /// or were never attributed, and are skipped.
    /// </summary>
    public async Task<ReconciliationSummary> ReconcileAll(CancellationToken cancellationToken)
    {
        var stored = (await _db.Connection.QueryAsync<StoredSubscription>(
            @"SELECT id, owner_id, variant_id, status, ends_at FROM subscriptions")).ToList();

        if (stored.Count == 0)
            return new ReconciliationSummary(0, 0, 0, 0);

        var fetched = await _lsClient.ListStoreSubscriptions(cancellationToken);
        var byId = fetched.ToDictionary(x => x.Id, x => x.Attributes);

        var changed = 0;
        var missing = 0;
        foreach (var row in stored)
        {
            if (!byId.TryGetValue(row.Id, out var current))
            {
                missing++;
                _logger.LogWarning("Stored subscription {SubId} of user {UserId} was not returned by LemonSqueezy; leaving it as is.", row.Id, row.OwnerId);
                continue;
            }

            if (!row.Differs(current))
                continue;

            await Apply(row.Id, row.OwnerId, current, unlock: false);
            changed++;
            _logger.LogInformation("Reconciled subscription {SubId} of user {UserId}: variant {OldVariant} -> {NewVariant}, status {OldStatus} -> {NewStatus}.",
                row.Id, row.OwnerId, row.VariantId, current.VariantID, row.Status, current.Status);
        }

        return new ReconciliationSummary(stored.Count, fetched.Count, changed, missing);
    }

    public async Task<SubscriptionAttributes?> RefreshFromLemonSqueezy(long subscriptionId, string ownerId, CancellationToken cancellationToken, bool unlockOnPlanChange = true)
    {
        try
        {
            var storedVariantId = await _db.Connection.QueryFirstOrDefaultAsync<long?>(
                @"SELECT variant_id FROM subscriptions WHERE id = @subscriptionId",
                new { subscriptionId });

            var sub = await _lsClient.GetSubscription(subscriptionId, cancellationToken);
            if (sub is null)
            {
                _logger.LogWarning("Subscription {SubId} for user {UserId} is unknown to LemonSqueezy; keeping the stored one.", subscriptionId, ownerId);
                return null;
            }

            // Only a plan we had wrong may release the lock here. The billing page triggers this refresh
            // for any over-quota subscriber, and an unchanged plan must not let a locked account unlock
            // itself by loading that page.
            var planChanged = storedVariantId != sub.VariantID;
            await Apply(subscriptionId, ownerId, sub, unlock: unlockOnPlanChange && planChanged);
            _logger.LogInformation("Refreshed subscription {SubId} for user {UserId} from LemonSqueezy: variant {VariantId}, status {Status}.", subscriptionId, ownerId, sub.VariantID, sub.Status);
            return sub;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not refresh subscription {SubId} for user {UserId} from LemonSqueezy; keeping the stored one.", subscriptionId, ownerId);
            return null;
        }
    }
}
