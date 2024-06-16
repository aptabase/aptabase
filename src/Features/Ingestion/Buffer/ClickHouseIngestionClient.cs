using Dapper;
using ClickHouse.Client.Copy;
using ClickHouse.Client.ADO;

namespace Aptabase.Features.Ingestion.Buffer;

public class ClickHouseIngestionClient : IIngestionClient
{
    private readonly ClickHouseConnection _conn;

    private readonly string[] COLUMNS = [
        "app_id",
        "timestamp",
        "event_name",
        "user_id",
        "session_id",
        "os_name",
        "os_version",
        "device_model",
        "locale",
        "app_version",
        "app_build_number",
        "engine_name",
        "engine_version",
        "sdk_version",
        "country_code",
        "region_name",
        "city",
        "string_props",
        "numeric_props",
        "ttl"
    ];

    public ClickHouseIngestionClient(ClickHouseConnection conn, ILogger<ClickHouseIngestionClient> logger)
    {
        _conn = conn ?? throw new ArgumentNullException(nameof(conn));
    }

    public async Task<long> SendEventAsync(EventRow row)
    {
        return await _conn.ExecuteAsync($@"INSERT INTO events ({string.Join(",", COLUMNS)}) VALUES (@{string.Join(", @", COLUMNS)})", new {
            app_id = row.AppId,
            timestamp = row.Timestamp,
            event_name = row.EventName,
            user_id = row.UserId,
            session_id = row.SessionId,
            os_name = row.OSName,
            os_version = row.OSVersion,
            device_model = row.DeviceModel,
            locale = row.Locale,
            app_version = row.AppVersion,
            app_build_number = row.AppBuildNumber,
            engine_name = row.EngineName,
            engine_version = row.EngineVersion,
            sdk_version = row.SdkVersion,
            country_code = row.CountryCode,
            region_name = row.RegionName,
            city = row.City,
            string_props = row.StringProps,
            numeric_props = row.NumericProps,
            ttl = row.TTL,
        });
    }

    public async Task<long> BulkSendEventAsync(IEnumerable<EventRow> rows, CancellationToken ct = default)
    {
        using var bulkCopy = new ClickHouseBulkCopy(_conn)
        {
            DestinationTableName = "events",
            BatchSize = 1000,
            ColumnNames = COLUMNS,
        };

        var values = rows.Select(row => new object[] { 
            row.AppId,
            row.Timestamp,
            row.EventName,
            row.UserId,
            row.SessionId,
            row.OSName,
            row.OSVersion,
            row.DeviceModel,
            row.Locale,
            row.AppVersion,
            row.AppBuildNumber,
            row.EngineName,
            row.EngineVersion,
            row.SdkVersion,
            row.CountryCode,
            row.RegionName,
            row.City,
            row.StringProps,
            row.NumericProps,
            row.TTL,
        });
        await bulkCopy.InitAsync();
        await bulkCopy.WriteToServerAsync(values, ct);
        return bulkCopy.RowsWritten;
    }
}