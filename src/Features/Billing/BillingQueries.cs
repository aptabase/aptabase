using Aptabase.Data;
using Aptabase.Features.Authentication;
using Dapper;

namespace Aptabase.Features.Billing;

public interface IBillingQueries
{
    Task<UserIdentity[]> GetTrialsDueSoon();
    Task<Subscription?> GetUserSubscription(UserIdentity user);
    Task<FreeSubscription> GetUserFreeTierOrTrial(UserIdentity user);
    Task<string[]> GetOwnedAppIds(UserIdentity user);
    Task<int> LockUsersWithExpiredTrials();
}

public class BillingQueries : IBillingQueries
{
    private readonly IDbContext _db;

    public BillingQueries(IDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Subscription?> GetUserSubscription(UserIdentity user)
    {
        return await _db.Connection.QueryFirstOrDefaultAsync<Subscription>(
            @"SELECT * FROM subscriptions 
              WHERE owner_id = @userId
              ORDER BY created_at DESC LIMIT 1",
            new { userId = user.Id });
    }

    public async Task<FreeSubscription> GetUserFreeTierOrTrial(UserIdentity user)
    {
        return await _db.Connection.QueryFirstAsync<FreeSubscription>(
            @"SELECT free_quota, free_trial_ends_at FROM users WHERE id = @userId",
            new { userId = user.Id });
    }

    public async Task<string[]> GetOwnedAppIds(UserIdentity user)
    {
        var releaseAppIds = await _db.Connection.QueryAsync<string>(@"SELECT id FROM apps WHERE owner_id = @userId", new { userId = user.Id });
        var debugAppIds = releaseAppIds.Select(id => $"{id}_DEBUG");
        return releaseAppIds.Concat(debugAppIds).ToArray();
    }

    public async Task<UserIdentity[]> GetTrialsDueSoon()
    {
        var start = DateTime.UtcNow.AddDays(5).Date;
        var end = start.AddDays(1).Date;

        var users = await _db.Connection.QueryAsync<UserIdentity>(
            @"SELECT DISTINCT u.id, u.name, u.email
              FROM users u
              LEFT JOIN subscriptions s
              ON s.owner_id = u.id
              INNER JOIN apps a
              ON a.owner_id = u.id
              AND a.has_events = true
              WHERE u.free_trial_ends_at BETWEEN @start AND @end
              AND s.id IS NULL", new { start, end });
        return users.ToArray();
    }

    public async Task<int> LockUsersWithExpiredTrials()
    {
        var count = await _db.Connection.ExecuteAsync(
            @"UPDATE users u
              SET lock_reason = 'T'
              WHERE u.free_quota IS NULL
              AND u.free_trial_ends_at <= now()
              AND u.lock_reason IS NULL
              AND NOT EXISTS (SELECT 1 FROM subscriptions s WHERE s.owner_id = u.id)");
        return count;
    }
}