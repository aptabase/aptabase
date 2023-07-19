using Dapper;
using ClickHouse.Client;

namespace Aptabase.Application.Ingestion;

public class ClickHouseIngestionClient : IIngestionClient
{
    private IClickHouseConnection _conn;
    private ILogger _logger;

    public ClickHouseIngestionClient(IClickHouseConnection conn, ILogger<ClickHouseIngestionClient> logger)
    {
        _conn = conn ?? throw new ArgumentNullException(nameof(conn));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<InsertResult> SendSingleAsync(EventRow row, CancellationToken cancellationToken)
    {
        var rows = await _conn.ExecuteAsync(@"INSERT INTO events (
                                                  app_id, timestamp, event_name, user_id, session_id, os_name,
                                                  os_version, locale,  app_version, app_build_number,
                                                  engine_name, engine_version, sdk_version, country_code,
                                                  region_name, city, string_props, numeric_props,ttl)
                                              VALUES (
                                                  @app_id, @timestamp, @event_name, @user_id,
                                                  @session_id, @os_name, @os_version, @locale,
                                                  @app_version, @app_build_number, @engine_name,
                                                  @engine_version, @sdk_version, @country_code,
                                                  @region_name, @city, @string_props, @numeric_props, @ttl
                                              )", new {
                                                  app_id = row.AppId,
                                                  timestamp = DateTime.Parse(row.Timestamp).ToUniversalTime(),
                                                  event_name = row.EventName,
                                                  user_id = row.UserId,
                                                  session_id = row.SessionId,
                                                  os_name = row.OSName,
                                                  os_version = row.OSVersion,
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
                                                  ttl = DateTime.Parse(row.TTL).ToUniversalTime(),
                                              });

        return new InsertResult { SuccessfulRows = rows };
    }

    public async Task<InsertResult> SendMultipleAsync(EventRow[] rows, CancellationToken cancellationToken)
    {
        var result = new InsertResult();
        foreach (var row in rows)
        {
            var insertResult = await SendSingleAsync(row, cancellationToken);
            result.SuccessfulRows += insertResult.SuccessfulRows;
        }
        return result;
    }
}