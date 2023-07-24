using Aptabase.Data;

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

        await _db.ExecuteScalarAsync("INSERT INTO blobs (path, content, content_type) VALUES (@path, @content, @contentType)", new {
            path,
            content = content,
            contentType = "image/png"
        }, cancellationToken);

        return path;
    }

    public async Task<Blob?> DownloadAsync(string path, CancellationToken cancellationToken)
    {
        return await _db.QuerySingleOrDefaultAsync<Blob>(
            "SELECT path, content, content_type FROM blobs WHERE path = @path", 
            new { path }, cancellationToken
        );
    }
}