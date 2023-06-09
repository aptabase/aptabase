using Aptabase.Data;
using Dapper;

namespace Aptabase.Application.Blob;

public class DatabaseBlobService : IBlobService
{
    private readonly IDbConnectionFactory _dbFactory;

    public DatabaseBlobService(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
    }
    
    public async Task<string> UploadAsync(string prefix, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        using var db = _dbFactory.Create();
        var path = $"{prefix}/{Guid.NewGuid().ToString()}.png";

        var insertBlob = new CommandDefinition("INSERT INTO blobs (path, content, content_type) VALUES (@path, @content, @contentType)", new {
            path,
            content = content,
            contentType = "image/png"
        }, cancellationToken: cancellationToken);

        await db.ExecuteScalarAsync(insertBlob);

        return path;
    }

    public async Task<Blob?> DownloadAsync(string path, CancellationToken cancellationToken)
    {
        using var db = _dbFactory.Create();

        var downloadBlob = new CommandDefinition("SELECT path, content, content_type FROM blobs WHERE path = @path", new {
            path,
        }, cancellationToken: cancellationToken);
        return await db.QuerySingleOrDefaultAsync<Blob>(downloadBlob);
    }
}