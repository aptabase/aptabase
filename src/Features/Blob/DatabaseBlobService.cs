using Aptabase.Data;
using ClickHouse.Client.Utility;
using Dapper;

namespace Aptabase.Features.Blob;

public class DatabaseBlobService : IBlobService
{
    private readonly IDbContext _db;

    public DatabaseBlobService(IDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }
    
    public async Task<string> UploadAsync(string prefix, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        var path = $"{prefix}/{Guid.NewGuid().ToString()}.png";

        var cmd = new CommandDefinition("INSERT INTO blobs (path, content, content_type) VALUES (@path, @content, @contentType)", new {
            path,
            content = content,
            contentType = "image/png"
        }, cancellationToken: cancellationToken);

        await _db.Connection.ExecuteScalarAsync(cmd);

        return path;
    }

    public async Task<Blob?> DownloadAsync(string path, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition("SELECT path, content, content_type FROM blobs WHERE path = @path", new { path }, cancellationToken: cancellationToken);
        return await _db.Connection.QuerySingleOrDefaultAsync<Blob>(cmd);
    }
}