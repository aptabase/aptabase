using Aptabase.Data;
using Dapper;

namespace Aptabase.Features.Apps;

public class Application
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string AppKey { get; set; } = "";
    public string IconPath { get; set; } = "";
    public bool HasOwnership { get; set; } = false;
}

public interface IAppQueries
{
    Task<Application?> GetActiveAppByAppKey(string appKey, CancellationToken cancellationToken);
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
        var cmd = new CommandDefinition("SELECT id, name, icon_path, app_key FROM apps WHERE app_key = @appKey AND deleted_at IS NULL",
            new { appKey },
            cancellationToken: cancellationToken
        );

        return _db.Connection.QueryFirstOrDefaultAsync<Application?>(cmd);
    }
}