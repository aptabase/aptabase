using Aptabase.Features.Authentication;
using Aptabase.Features.GeoIP;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aptabase.Features.Stats;

public record PeriodicStatsRow
{
    public DateTime Period { get; set; }
    public double Users { get; set; } = 0;
    public int Sessions { get; set; } = 0;
    public int Events { get; set; } = 0;
}

public record KeyMetrics
{
    public KeyMetricsRow Current { get; set; } = new KeyMetricsRow();
    public KeyMetricsRow? Previous { get; set; }

    public KeyMetrics()
    {
    }

    public KeyMetrics(KeyMetricsRow current)
    {
        Current = current;
    }

    public KeyMetrics(KeyMetricsRow current, KeyMetricsRow previous)
    {
        Current = current;
        Previous = previous;
    }
}

public record KeyMetricsRow
{
    public double DailyUsers { get; set; } = 0;
    public int Sessions { get; set; } = 0;
    public int Events { get; set; } = 0;
    public double DurationSeconds { get; set; } = 0;
}

public record TopNItem
{
    public string Name { get; set; } = "";
    public double Value { get; set; }
}

public record EventPropKeys
{
    public string[] StringKeys { get; set; } = Array.Empty<string>();
    public string[] NumericKeys { get; set; } = Array.Empty<string>();
}

public record EventPropsItem
{
    public string StringKey { get; set; } = "";
    public string StringValue { get; set; } = "";
    public string NumericKey { get; set; } = "";
    public int Events { get; set; }
    public decimal Median { get; set; }
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public decimal Sum { get; set; }
}

public record LiveGeoDataPoint
{
    public string CountryCode { get; set; } = "";
    public string RegionName { get; set; } = "";
    public ulong Users { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public record LiveRecentSession
{
    public string Id { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public ulong Duration { get; set; }
    public ulong EventsCount { get; set; }
    public string AppVersion { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string RegionName { get; set; } = "";
    public string OsName { get; set; } = "";
    public string OsVersion { get; set; } = "";
    public string DeviceModel { get; set; } = "";
}

public record SessionTimeline
{
    public string Id { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public ulong Duration { get; set; }
    public ulong EventsCount { get; set; }
    public string AppVersion { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string RegionName { get; set; } = "";
    public string OsName { get; set; } = "";
    public string OsVersion { get; set; } = "";
    public string[] EventsName { get; set; } = Array.Empty<string>();
    public DateTime[] EventsTimestamp { get; set; } = Array.Empty<DateTime>();
    public string[] EventsStringProps { get; set; } = Array.Empty<string>();
    public string[] EventsNumericProps { get; set; } = Array.Empty<string>();
}

public enum TopNValue
{
    UniqueSessions,
    TotalEvents
}

public enum GranularityEnum
{
    Hour,
    Day,
    Month
}

public record QueryArgs
{
    public string AppId { get; set; } = "";
    public string? SessionId { get; set; } = "";
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public GranularityEnum? Granularity { get; set; }
    public string? CountryCode { get; set; }
    public string? OsName { get; set; }
    public string? DeviceModel { get; set; }
    public string? EventName { get; set; }
    public string? AppVersion { get; set; }

    public QueryArgs CloneToPreviousInterval()
    {
        if (!DateFrom.HasValue || !DateTo.HasValue)
        {
            return this;
        }

        var previousEnd = DateFrom.Value;
        // Go back 1 second so that when relativeTo is 00:00:00 we start with the previous day
        // Happens when looking for previous period and "period" is "month" or "last-month"
        if (previousEnd.TimeOfDay == TimeSpan.Zero)
            previousEnd = previousEnd.AddSeconds(-1);
        var previousStart =
            DateFrom.Value.Subtract(
                DateTo.Value.Subtract((DateFrom.Value))); //start - (end - start)

        return new QueryArgs
        {
            AppId = AppId,
            SessionId = SessionId,
            DateFrom = previousStart,
            DateTo = previousEnd,
            Granularity = Granularity,
            CountryCode = CountryCode,
            OsName = OsName,
            EventName = EventName,
            AppVersion = AppVersion,
        };
    }
}

public class QueryParams
{
    public string BuildMode { get; set; } = "";
    public string AppId { get; set; } = "";
    public string? SessionId { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public GranularityEnum? Granularity { get; set; }
    public string? CountryCode { get; set; }
    public string? OsName { get; set; }
    public string? EventName { get; set; }
    public string? AppVersion { get; set; }
    public string? DeviceModel { get; set; }

    public QueryArgs Parse()
    {
        var appId = BuildMode.ToLower() switch
        {
            "debug" => $"{AppId}_DEBUG",
            _ => AppId,
        };

        return new QueryArgs
        {
            AppId = appId,
            SessionId = SessionId,
            DateFrom = StartDate,
            DateTo = EndDate,
            Granularity = Granularity,
            CountryCode = CountryCode,
            OsName = OsName,
            DeviceModel = DeviceModel,
            EventName = EventName,
            AppVersion = AppVersion,
        };
    }
}

[ApiController, IsAuthenticated, HasReadAccessToApp]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting("Stats")]
public class StatsController : Controller
{
    private readonly IQueryClient _queryClient;
    private readonly GeoIPClient _geodb;

    public StatsController(IQueryClient queryClient, GeoIPClient geodb)
    {
        _geodb = geodb ?? throw new ArgumentNullException(nameof(geodb));
        _queryClient = queryClient ?? throw new ArgumentNullException(nameof(queryClient));
    }

    [HttpGet("/api/_stats/top-countries")]
    public async Task<IActionResult> TopCountries([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("country_code", body.Granularity ?? GranularityEnum.Hour, TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-osversions")]
    public async Task<IActionResult> TopOSVersions([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("os_version", body.Granularity ?? GranularityEnum.Hour, TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-devices")]
    public async Task<IActionResult> TopDevices([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("device_model", body.Granularity ?? GranularityEnum.Hour, TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-operatingsystems")]
    public async Task<IActionResult> TopOperatingSystems([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("os_name", body.Granularity ?? GranularityEnum.Hour, TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-regions")]
    public async Task<IActionResult> TopRegions([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("region_name", body.Granularity ?? GranularityEnum.Hour, TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-events")]
    public async Task<IActionResult> TopEvents([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("event_name", body.Granularity ?? GranularityEnum.Hour, TopNValue.TotalEvents, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-appversions")]
    public async Task<IActionResult> TopAppVersions([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("app_version", body.Granularity ?? GranularityEnum.Hour, TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-appbuildnumbers")]
    public async Task<IActionResult> TopAppBuildNumbers([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("app_build_number", body.Granularity ?? GranularityEnum.Hour, TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/metrics")]
    public async Task<IActionResult> KeyMetrics([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        var currentQuery = body.Parse();
        var current = GetKeyMetrics(currentQuery, cancellationToken);

        if (!currentQuery.DateFrom.HasValue || !currentQuery.DateTo.HasValue) {
            return Ok(new KeyMetrics(await current));
        }
        
        var previousQuery = currentQuery.CloneToPreviousInterval();
        var previous = await GetKeyMetrics(previousQuery, cancellationToken);
        return Ok(new KeyMetrics(await current, previous));
    }

    [HttpGet("/api/_stats/periodic")]
    public async Task<IActionResult> PeriodicStats([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        var query = body.Parse();
        var rows = await _queryClient.NamedQueryAsync<PeriodicStatsRow>("key_metrics_periodic__v2", new {
            date_from = query.DateFrom?.ToString("yyyy-MM-dd HH:mm:ss"),
            date_to = query.DateTo?.ToString("yyyy-MM-dd HH:mm:ss"),
            granularity = (query.Granularity ?? GranularityEnum.Hour).ToString(),
            app_id = query.AppId,
            event_name = query.EventName,
            os_name = query.OsName,
            app_version = query.AppVersion,
            country_code = query.CountryCode,
            device_model = query.DeviceModel,
        }, cancellationToken);

        return Ok(rows);
    }

    [HttpGet("/api/_stats/top-props")]
    public async Task<IActionResult> EventProps([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        var query = body.Parse();
        var rows = await _queryClient.NamedQueryAsync<EventPropsItem>("top_props__v2", new {
            date_from = query.DateFrom?.ToString("yyyy-MM-dd HH:mm:ss"),
            date_to = query.DateTo?.ToString("yyyy-MM-dd HH:mm:ss"),
            granularity = (body.Granularity ?? GranularityEnum.Hour).ToString(),
            app_id = query.AppId,
            event_name = query.EventName,
            os_name = query.OsName,
            app_version = query.AppVersion,
            country_code = query.CountryCode,
            device_model = query.DeviceModel,
        }, cancellationToken);

        return Ok(rows);
    }

    [HttpGet("/api/_stats/live-geo")]
    public async Task<IActionResult> LiveGeo([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        var query = body.Parse();

        var rows = await _queryClient.NamedQueryAsync<LiveGeoDataPoint>("live_geo__v1", new {
            app_id = query.AppId,
        }, cancellationToken);

        foreach (var row in rows)
        {
            var (lat, lng) = _geodb.GetLatLng(row.CountryCode, row.RegionName);
            row.Latitude = lat;
            row.Longitude = lng;
        }

        return Ok(rows);
    }

    [HttpGet("/api/_stats/live-sessions")]
    public async Task<IActionResult> LiveSessions([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        var query = body.Parse();

        var rows = await _queryClient.NamedQueryAsync<LiveRecentSession>("live_sessions__v1", new {
            app_id = query.AppId,
        }, cancellationToken);

        return Ok(rows);
    }

    [HttpGet("/api/_stats/live-session-details")]
    public async Task<IActionResult> LiveSessionDetails([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        var query = body.Parse();

        var row = await _queryClient.NamedQuerySingleAsync<SessionTimeline>("live_session_details__v1", new {
            app_id = query.AppId,
            session_id = query.SessionId,
        }, cancellationToken);

        return Ok(row);
    }

    [HttpGet("/api/_stats/historical-sessions")]
    public async Task<IActionResult> HistoricalSessions([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        var query = body.Parse();

        var row = await _queryClient.NamedQueryAsync<LiveRecentSession>("historical_sessions__v1", new
        {
            app_id = query.AppId,
            session_id = query.SessionId,
            date_from = query.DateFrom?.ToString("yyyy-MM-dd HH:mm:ss"),
            date_to = query.DateTo?.ToString("yyyy-MM-dd HH:mm:ss"),
            event_name = query.EventName,
            os_name = query.OsName,
            app_version = query.AppVersion,
            country_code = query.CountryCode
        }, cancellationToken);

        return Ok(row);
    }

    private async Task<KeyMetricsRow> GetKeyMetrics(QueryArgs args, CancellationToken cancellationToken)
    {
        return await _queryClient.NamedQuerySingleAsync<KeyMetricsRow>("key_metrics__v2", new {
            date_from = args.DateFrom?.ToString("yyyy-MM-dd HH:mm:ss"),
            date_to = args.DateTo?.ToString("yyyy-MM-dd HH:mm:ss"),
            granularity = (args.Granularity ?? GranularityEnum.Hour).ToString(),
            app_id = args.AppId,
            event_name = args.EventName,
            os_name = args.OsName,
            app_version = args.AppVersion,
            country_code = args.CountryCode,
            device_model = args.DeviceModel,
        }, cancellationToken);
    }

    private async Task<IActionResult> TopN(string nameColumn, GranularityEnum granularity, TopNValue valueColumn, QueryParams body, CancellationToken cancellationToken)
    {
        var query = body.Parse();
        var rows = await _queryClient.NamedQueryAsync<TopNItem>("top_n__v2", new {
            name_column = nameColumn,
            granularity = granularity.ToString(),
            value_column = valueColumn.ToString(),
            date_from = query.DateFrom?.ToString("yyyy-MM-dd HH:mm:ss"),
            date_to = query.DateTo?.ToString("yyyy-MM-dd HH:mm:ss"),
            app_id = query.AppId,
            event_name = query.EventName,
            os_name = query.OsName,
            app_version = query.AppVersion,
            country_code = query.CountryCode,
            device_model = query.DeviceModel,
        }, cancellationToken);

        return Ok(rows);
    }
}
