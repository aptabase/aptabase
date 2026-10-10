using Aptabase.Data;
using System.Security.Cryptography;
using Dapper;

namespace Aptabase.Features.Privacy;

public interface IPrivacyQueries
{
    Task<byte[]> ReadOrCreateSalt(string appId, string date);
    Task<int> PurgeOldSalts(CancellationToken cancellationToken);
}

public class PrivacyQueries : IPrivacyQueries
{
    private readonly IDbContext _db;

    public PrivacyQueries(IDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<byte[]> ReadOrCreateSalt(string appId, string date)
    {
        // The no-op DO UPDATE makes RETURNING yield the stored salt whether this statement inserted it
        // or another instance did, even one that committed concurrently. DO NOTHING would return no row.
        var newSalt = RandomNumberGenerator.GetBytes(16);
        return await _db.Connection.QuerySingleAsync<byte[]>(@"
            INSERT INTO app_salts (app_id, date, salt) VALUES (@appId, @date, @newSalt)
            ON CONFLICT (app_id, date) DO UPDATE SET salt = app_salts.salt
            RETURNING salt", new { appId, date, newSalt });
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
