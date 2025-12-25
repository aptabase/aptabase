using Aptabase.Data;
using Dapper;

namespace Aptabase.Features.Export;

public class Export
{
    public string Id { get; set; } = "";
    public string AppId { get; set; } = "";
    public string AppName { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string S3Bucket { get; set; } = "";
    public string S3Key { get; set; } = "";
    public string Format { get; set; } = "";
    public ExportStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum ExportStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Faulted = 3
}

public interface IExportQueries
{
    Task<string> StartExportAsync(string appId, DateTime startDate, DateTime endDate,
        string s3Bucket, string s3Key, string format, CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusAsync(string exportId, ExportStatus fromStatus, ExportStatus toStatus,
        string? errorMessage = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<Export>> GetPendingExportsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<Export>> GetOwnedExportsAsync(string appId, CancellationToken cancellationToken = default);
}

public class ExportQueries : IExportQueries
{
    private readonly IDbContext _db;

    public ExportQueries(IDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<string> StartExportAsync(string appId, DateTime startDate,
        DateTime endDate, string s3Bucket, string s3Key, string format, CancellationToken cancellationToken = default)
    {
        var exportId = NanoId.New();
        var cmd = new CommandDefinition(@"
            INSERT INTO exports (id, app_id, start_date, end_date, s3_bucket, s3_key, 
                               status, created_at, format)
            VALUES (@exportId, @appId, @startDate, @endDate, @s3Bucket, @s3Key, 
                   @status, @createdAt, @format)",
            new
            {
                exportId,
                appId,
                startDate,
                endDate,
                s3Bucket,
                s3Key,
                status = (int)ExportStatus.Pending,
                createdAt = DateTime.UtcNow,
                format
            },
            cancellationToken: cancellationToken
        );

        await _db.Connection.ExecuteAsync(cmd);
        return exportId;
    }

    public async Task<bool> UpdateStatusAsync(string exportId, ExportStatus fromStatus, ExportStatus toStatus,
        string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        var setClause = "status = @toStatus";
        var parameters = new DynamicParameters();
        parameters.Add("exportId", exportId);
        parameters.Add("fromStatus", (int)fromStatus);
        parameters.Add("toStatus", (int)toStatus);

        if (toStatus == ExportStatus.Completed)
        {
            setClause += ", completed_at = @completedAt, error_message = NULL";
            parameters.Add("completedAt", DateTime.UtcNow);
        }
        else if (toStatus == ExportStatus.Faulted)
        {
            setClause += ", completed_at = @completedAt";
            parameters.Add("completedAt", DateTime.UtcNow);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                setClause += ", error_message = @errorMessage";
                parameters.Add("errorMessage", errorMessage);
            }
        }

        var cmd = new CommandDefinition($@"
            UPDATE exports 
            SET {setClause}
            WHERE id = @exportId AND status = @fromStatus",
            parameters,
            cancellationToken: cancellationToken
        );

        var rowsAffected = await _db.Connection.ExecuteAsync(cmd);
        return rowsAffected > 0;
    }

    public async Task<IEnumerable<Export>> GetPendingExportsAsync(CancellationToken cancellationToken = default)
    {
        var cmd = new CommandDefinition(@"
            SELECT e.id, e.app_id, a.name as app_name, e.start_date, e.end_date, 
                   e.s3_bucket, e.s3_key, e.status, e.error_message,
                   e.created_at, e.completed_at
            FROM exports e
            INNER JOIN apps a ON a.id = e.app_id
            WHERE e.status = @status 
            AND a.deleted_at IS NULL
            ORDER BY e.created_at ASC",
            new { status = (int)ExportStatus.Pending },
            cancellationToken: cancellationToken
        );

        return await _db.Connection.QueryAsync<Export>(cmd);
    }

    public async Task<IEnumerable<Export>> GetOwnedExportsAsync(string appId, CancellationToken cancellationToken = default)
    {
        var cmd = new CommandDefinition(@"
            SELECT e.id, e.app_id, a.name as app_name, e.start_date, e.end_date, 
                   e.s3_bucket, e.s3_key, e.status, e.error_message,
                   e.created_at, e.completed_at
            FROM exports e
            INNER JOIN apps a ON a.id = e.app_id
            WHERE e.app_id = @appId 
            AND a.deleted_at IS NULL
            ORDER BY e.created_at DESC",
            new { appId },
            cancellationToken: cancellationToken
        );

        return await _db.Connection.QueryAsync<Export>(cmd);
    }
}