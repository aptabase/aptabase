using Dapper;
using Aptabase.Data;
using Aptabase.Features.Authentication;

namespace Aptabase.Features.Stats;

public static class StatsQueries
{
    public static async Task<bool> HasReadAccessToApp(this IDbContext db, string appId, UserIdentity user, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition(
            @"SELECT a.id
              FROM apps a
              LEFT JOIN app_shares s
              ON s.app_id = a.id
              LEFT JOIN users u
              ON u.id = a.owner_id
              WHERE a.id = @appId
              AND (a.owner_id = @userId OR s.email = @userEmail)
              AND u.lock_reason IS NULL
              LIMIT 1",
            new { appId, userId = user.Id, userEmail = user.Email },
            cancellationToken: cancellationToken
        );
        
        var id = await db.Connection.ExecuteScalarAsync<string>(cmd);
        return id == appId;
    }
}