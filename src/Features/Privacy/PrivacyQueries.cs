using Aptabase.Data;
using Dapper;

namespace Aptabase.Features.Privacy;

public interface IPrivacyQueries
{
    Task<int> PurgeOldSalts(CancellationToken cancellationToken);
}

public class PrivacyQueries : IPrivacyQueries
{
    private readonly IDbContext _db;

    public PrivacyQueries(IDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<int> PurgeOldSalts(CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition(
            "DELETE FROM app_salts WHERE TO_DATE(date, 'YYYY/MM/DD') <= CURRENT_DATE - INTERVAL '2' DAY",
            cancellationToken: cancellationToken
        );
        
        return await _db.Connection.ExecuteAsync(cmd);
    }
}