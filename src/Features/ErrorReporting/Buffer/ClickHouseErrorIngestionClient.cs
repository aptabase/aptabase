using ClickHouse.Client.Copy;
using ClickHouse.Client.ADO;

namespace Aptabase.Features.ErrorReporting.Buffer;

public class ClickHouseErrorIngestionClient : IErrorIngestionClient
{
    private readonly ClickHouseConnection _conn;

    private readonly string[] COLUMNS = [
        "error_id",
        "app_id",
        "timestamp",
        "error_message",
        "error_type",
        "stack_trace",
        "platform",
        "os_name",
        "os_version",
        "app_version",
        "sdk_version",
        "session_id",
        "severity",
        "kind",
        "ttl"
    ];

    public ClickHouseErrorIngestionClient(ClickHouseConnection conn, ILogger<ClickHouseErrorIngestionClient> logger)
    {
        _conn = conn ?? throw new ArgumentNullException(nameof(conn));
    }

    public async Task<long> BulkSendErrorAsync(IEnumerable<ErrorRow> rows, CancellationToken ct = default)
    {
        using var bulkCopy = new ClickHouseBulkCopy(_conn)
        {
            DestinationTableName = "error_events",
            BatchSize = 1000,
            ColumnNames = COLUMNS,
        };

        var values = rows.Select(row => new object[] {
            row.ErrorId,
            row.AppId,
            row.Timestamp,
            row.ErrorMessage,
            row.ErrorType,
            row.StackTrace,
            row.Platform,
            row.OsName,
            row.OsVersion,
            row.AppVersion,
            row.SdkVersion,
            row.SessionId,
            row.Severity,
            row.Kind,
            row.TTL,
        });
        await bulkCopy.InitAsync();
        await bulkCopy.WriteToServerAsync(values, ct);
        return bulkCopy.RowsWritten;
    }
}
