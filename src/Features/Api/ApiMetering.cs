using Aptabase.Data;
using Dapper;

namespace Aptabase.Features.Api;

public record QuotaDecision(bool Allowed, long Used, long? Allowance, DateTime ResetsAt)
{
    public long? Remaining => Allowance.HasValue ? Math.Max(0, Allowance.Value - Used) : null;
}

public class AppApiUsageRow
{
    public string GrantId { get; set; } = "";
    public string Kind { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string SubjectId { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string SubjectEmail { get; set; } = "";
    public long Requests { get; set; }
    public long Rejected { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public class OwnerApiUsageRow
{
    public string AppId { get; set; } = "";
    public string AppName { get; set; } = "";
    public long Requests { get; set; }
    public long Rejected { get; set; }
}

public interface IApiMetering
{
    /// <summary>Counts one request against the owner's monthly allowance, or refuses it.</summary>
    Task<QuotaDecision> CountAsync(string grantId, string appId, string ownerId, long? allowance, CancellationToken cancellationToken);

    /// <summary>The owner's usage this month, without counting anything.</summary>
    Task<QuotaDecision> PeekAsync(string ownerId, long? allowance, CancellationToken cancellationToken);

    Task<IEnumerable<AppApiUsageRow>> GetAppUsage(string appId, string ownerId, CancellationToken cancellationToken);
    Task<IEnumerable<OwnerApiUsageRow>> GetOwnerUsage(string ownerId, CancellationToken cancellationToken);
}

/// <summary>
/// Monthly request metering. Usage is one row per grant, app, owner and month, and the
/// owner's total is the sum of their rows, so the quota decision and the usage views read
/// the same numbers and cannot disagree. Months are UTC, like the events quota.
/// </summary>
public class ApiMetering : IApiMetering
{
    private const string Period = "date_trunc('month', timezone('utc', now()))::date";

    private const string Upsert = $@"
        INSERT INTO api_grant_usage (grant_id, app_id, period, owner_id, requests, rejected, modified_at)
        VALUES (@grantId, @appId, {Period}, @ownerId, 1, 0, now())
        ON CONFLICT (grant_id, app_id, period, owner_id)
        DO UPDATE SET requests = api_grant_usage.requests + 1, modified_at = now()";

    private const string OwnerSum = $@"
        SELECT COALESCE(SUM(requests), 0)::bigint FROM api_grant_usage
        WHERE owner_id = @ownerId AND period = {Period}";

    private readonly IDbContext _db;

    public ApiMetering(IDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public static DateTime NextReset(DateTime utcNow)
        => new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

    public async Task<QuotaDecision> CountAsync(string grantId, string appId, string ownerId, long? allowance, CancellationToken cancellationToken)
    {
        var resetsAt = NextReset(DateTime.UtcNow);
        var args = new { grantId, appId, ownerId };

        await using var cn = _db.Connection;
        await cn.OpenAsync(cancellationToken);

        // No allowance to enforce: the request is still recorded for the usage views
        if (allowance is null)
        {
            await cn.ExecuteAsync(new CommandDefinition(Upsert, args, cancellationToken: cancellationToken));
            return new QuotaDecision(true, 0, null, resetsAt);
        }

        // Increment, then sum, inside one transaction that holds a lock per owner. Without
        // the lock two concurrent requests at the boundary would both see both increments
        // and one of them would be refused although the allowance had room for it. Requests
        // for different owners never wait on each other.
        await using var tx = await cn.BeginTransactionAsync(cancellationToken);

        await cn.ExecuteAsync(new CommandDefinition(
            "SELECT pg_advisory_xact_lock(hashtext(@ownerId))", args, tx, cancellationToken: cancellationToken));
        await cn.ExecuteAsync(new CommandDefinition(Upsert, args, tx, cancellationToken: cancellationToken));
        var used = await cn.ExecuteScalarAsync<long>(new CommandDefinition(OwnerSum, args, tx, cancellationToken: cancellationToken));

        if (used > allowance.Value)
        {
            await cn.ExecuteAsync(new CommandDefinition(
                $@"UPDATE api_grant_usage SET requests = requests - 1, rejected = rejected + 1
                   WHERE grant_id = @grantId AND app_id = @appId AND owner_id = @ownerId AND period = {Period}",
                args, tx, cancellationToken: cancellationToken));
            await tx.CommitAsync(cancellationToken);
            return new QuotaDecision(false, used - 1, allowance, resetsAt);
        }

        await tx.CommitAsync(cancellationToken);
        return new QuotaDecision(true, used, allowance, resetsAt);
    }

    public async Task<QuotaDecision> PeekAsync(string ownerId, long? allowance, CancellationToken cancellationToken)
    {
        using var cn = _db.Connection;
        var used = await cn.ExecuteScalarAsync<long>(new CommandDefinition(OwnerSum, new { ownerId }, cancellationToken: cancellationToken));
        return new QuotaDecision(true, used, allowance, NextReset(DateTime.UtcNow));
    }

    // Only the rows that came out of this owner's allowance: after an ownership transfer
    // the previous owner's rows for the same app belong to their view, not to this one
    public async Task<IEnumerable<AppApiUsageRow>> GetAppUsage(string appId, string ownerId, CancellationToken cancellationToken)
    {
        using var cn = _db.Connection;
        return await cn.QueryAsync<AppApiUsageRow>(new CommandDefinition(
            $@"SELECT g.id AS grant_id, g.kind, g.client_name, g.subject_id,
                      s.name AS subject_name, s.email AS subject_email,
                      u.requests, u.rejected, g.last_used_at, g.revoked_at
               FROM api_grant_usage u
               INNER JOIN oauth_grants g ON g.id = u.grant_id
               INNER JOIN users s ON s.id = g.subject_id
               WHERE u.app_id = @appId AND u.owner_id = @ownerId AND u.period = {Period}
               ORDER BY u.requests DESC, g.client_name",
            new { appId, ownerId }, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<OwnerApiUsageRow>> GetOwnerUsage(string ownerId, CancellationToken cancellationToken)
    {
        using var cn = _db.Connection;
        return await cn.QueryAsync<OwnerApiUsageRow>(new CommandDefinition(
            $@"SELECT a.id AS app_id, a.name AS app_name,
                      SUM(u.requests)::bigint AS requests, SUM(u.rejected)::bigint AS rejected
               FROM api_grant_usage u
               INNER JOIN apps a ON a.id = u.app_id
               WHERE u.owner_id = @ownerId AND u.period = {Period}
               GROUP BY a.id, a.name
               ORDER BY requests DESC, a.name",
            new { ownerId }, cancellationToken: cancellationToken));
    }
}
