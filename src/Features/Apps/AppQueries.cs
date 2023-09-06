using Aptabase.Data;
using Dapper;

namespace Aptabase.Features.Apps;

public class Application
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string AppKey { get; set; } = "";
    public string IconPath { get; set; } = "";
    public bool HasEvents { get; set; } = false;
    public bool HasOwnership { get; set; } = false;
    public char? LockReason { get; set; }
}

public interface IAppQueries
{
    Task<Application?> GetActiveAppByAppKey(string appKey, CancellationToken cancellationToken);
    Task MaskAsOnboarded(string appId, CancellationToken cancellationToken);
}

public class AppQueries : IAppQueries
{
    private readonly IDbContext _db;

    public AppQueries(IDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<Application?> GetActiveAppByAppKey(string appKey, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition(@"
            SELECT a.id, a.name, a.icon_path, 
                   a.app_key, a.has_events, 
                   u.lock_reason
            FROM apps a
            INNER JOIN users u
            ON u.id = a.owner_id
            WHERE a.app_key = @appKey AND a.deleted_at IS NULL",
            new { appKey },
            cancellationToken: cancellationToken
        );

        return _db.Connection.QueryFirstOrDefaultAsync<Application?>(cmd);
    }

    public async Task MaskAsOnboarded(string appId, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition($"UPDATE apps SET has_events = true WHERE id = @appId",
            new { appId },
            cancellationToken: cancellationToken
        );

        await _db.Connection.ExecuteAsync(cmd);
    }
}