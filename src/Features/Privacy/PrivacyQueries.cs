using Aptabase.Data;
using Dapper;

namespace Aptabase.Features.Privacy;

public static class PrivacyQueries
{
    public static async Task<int> PurgeOldSalts(this IDbContext db, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition(
            "DELETE FROM app_salts WHERE TO_DATE(date, 'YYYY/MM/DD') <= CURRENT_DATE - INTERVAL '2' DAY",
            cancellationToken: cancellationToken
        );
        
        return await db.Connection.ExecuteAsync(cmd);
    }
}