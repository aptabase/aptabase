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
        var users = await _db.Connection.QueryAsync<UserIdentity>(
            @"SELECT DISTINCT u.id, u.name, u.email
              FROM users u
              LEFT JOIN subscriptions s
              ON s.owner_id = u.id
              INNER JOIN apps a
              ON a.owner_id = u.id
              AND a.has_events = true
              WHERE u.free_trial_ends_at = now() + INTERVAL '7 DAY'
              AND s.id IS NULL");
        return users.ToArray();
    }
}