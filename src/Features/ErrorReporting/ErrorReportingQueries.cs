using Dapper;
using Aptabase.Data;

namespace Aptabase.Features.ErrorReporting;

public static class ErrorReportingQueries
{
    /// <summary>
    /// Atomically consumes one unit of the app's error quota. Returns true if the app was
    /// under its quota (and the count was incremented), false if the quota is exhausted.
    /// </summary>
    public static async Task<bool> TryConsumeErrorQuota(this IDbContext db, string appId, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition(
            @"UPDATE apps
              SET error_count = error_count + 1
              WHERE id = @appId
              AND error_count < error_quota
              RETURNING id",
            new { appId },
            cancellationToken: cancellationToken
        );

        var id = await db.Connection.ExecuteScalarAsync<string>(cmd);
        return id is not null;
    }

    /// <summary>
    /// Resets the per-app error counter for every app. Intended to run on a monthly schedule.
    /// </summary>
    public static async Task<int> ResetAllErrorCounts(this IDbContext db, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition(
            "UPDATE apps SET error_count = 0 WHERE error_count > 0",
            cancellationToken: cancellationToken
        );

        return await db.Connection.ExecuteAsync(cmd);
    }
}
