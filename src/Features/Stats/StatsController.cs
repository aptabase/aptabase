using Aptabase.Features.Authentication;
using Aptabase.Features.GeoIP;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aptabase.Features.Stats;

public record PeriodicStats
{
    public IEnumerable<PeriodicStatsRow> Rows { get; set; } = Array.Empty<PeriodicStatsRow>();
    public string Granularity { get; set; } = "";
}

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
    public GranularityEnum Granularity { get; set; }
    public string? CountryCode { get; set; }
    public string? OsName { get; set; }
    public string? DeviceModel { get; set; }
    public string? EventName { get; set; }
    public string? AppVersion { get; set; }
}

public class QueryParams
{
    public string BuildMode { get; set; } = "";
    public string AppId { get; set; } = "";
    public string? SessionId { get; set; } = "";
    public string? Period { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public GranularityEnum? Granularity { get; set; }
    public string? CountryCode { get; set; }
    public string? OsName { get; set; }
    public string? EventName { get; set; }
    public string? AppVersion { get; set; }
    public string? DeviceModel { get; set; }

    public QueryArgs Parse(DateTime relativeTo)
    {
        // Go back 1 second so that when relativeTo is 00:00:00 we start with the previous day
        // Happens when looking for previous period and "period" is "month" or "last-month"
        if (relativeTo.TimeOfDay == TimeSpan.Zero)
            relativeTo = relativeTo.AddSeconds(-1);

        DateTime? dateFrom = relativeTo.AddHours(-24), dateTo = relativeTo;
        GranularityEnum granularity = Granularity ?? GranularityEnum.Hour;
        if (StartDate != null && EndDate != null)
        {
            dateFrom = DateTime.Parse(StartDate);
            dateTo = DateTime.Parse(EndDate);
        } 
        // Kept for backwards compatibility. Currently StartDate/EndDate should always be send by client
        else if (Period != null)
        {
            (dateFrom, dateTo, granularity) = Period switch
            {
                "24h" => (relativeTo.AddHours(-24), relativeTo, GranularityEnum.Hour),
                "48h" => (relativeTo.AddHours(-48), relativeTo, GranularityEnum.Hour),
                "7d" => (relativeTo.Date.AddDays(-7), relativeTo, GranularityEnum.Day),
                "14d" => (relativeTo.Date.AddDays(-14), relativeTo, GranularityEnum.Day),
                "30d" => (relativeTo.Date.AddDays(-30), relativeTo, GranularityEnum.Day),
                "90d" => (relativeTo.Date.AddDays(-90), relativeTo, GranularityEnum.Day),
                "180d" => (relativeTo.Date.AddDays(-180), relativeTo, GranularityEnum.Month),
                "365d" => (relativeTo.Date.AddDays(-365), relativeTo, GranularityEnum.Month),
                "month" => (new DateTime(relativeTo.Year, relativeTo.Month, 1), new DateTime(relativeTo.Year, relativeTo.Month, 1).AddMonths(1).AddDays(-1), GranularityEnum.Day),
                "last-month" => (new DateTime(relativeTo.Year, relativeTo.Month, 1).AddMonths(-1), new DateTime(relativeTo.Year, relativeTo.Month, 1), GranularityEnum.Day),
                "today" => (relativeTo.Date, relativeTo.Date.AddDays(1).AddSeconds(-1), GranularityEnum.Hour),
                "yesterday" => (relativeTo.Date.AddDays(-1), relativeTo.Date.AddSeconds(-1), GranularityEnum.Hour),
                "all" => (default(DateTime?), default(DateTime?), GranularityEnum.Month),
                _ => (relativeTo.AddHours(-24), relativeTo, GranularityEnum.Hour), // default to 24 hours
            };
        }

        if ((dateFrom is null && dateTo is not null) || (dateFrom is not null && dateTo is null))
            throw new ArgumentException("Both dateFrom and dateTo must be defined, or both must be null");

        var appId = BuildMode.ToLower() switch
        {
            "debug" => $"{AppId}_DEBUG",
            _ => AppId,
        };

        return new QueryArgs
        {
            AppId = appId,
            SessionId = SessionId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Granularity = granularity,
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
        return await TopN("country_code", TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-osversions")]
    public async Task<IActionResult> TopOSVersions([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("os_version", TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-devices")]
    public async Task<IActionResult> TopDevices([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("device_model", TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-operatingsystems")]
    public async Task<IActionResult> TopOperatingSystems([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("os_name", TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-regions")]
    public async Task<IActionResult> TopRegions([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("region_name", TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-events")]
    public async Task<IActionResult> TopEvents([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("event_name", TopNValue.TotalEvents, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-appversions")]
    public async Task<IActionResult> TopAppVersions([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("app_version", TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-appbuildnumbers")]
    public async Task<IActionResult> TopAppBuildNumbers([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        return await TopN("app_build_number", TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/metrics")]
    public async Task<IActionResult> KeyMetrics([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        var currentQuery = body.Parse(DateTime.UtcNow);
        var current = GetKeyMetrics(currentQuery, cancellationToken);

        if (!currentQuery.DateFrom.HasValue) {
            return Ok(new KeyMetrics(await current));
        }

        var previousQuery = body.Parse(currentQuery.DateFrom.Value);
        var previous = await GetKeyMetrics(previousQuery, cancellationToken);
        return Ok(new KeyMetrics(await current, previous));
    }

    [HttpGet("/api/_stats/periodic")]
    public async Task<IActionResult> PeriodicStats([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        var query = body.Parse(DateTime.UtcNow);
        var rows = await _queryClient.NamedQueryAsync<PeriodicStatsRow>("key_metrics_periodic__v2", new {
            date_from = query.DateFrom?.ToString("yyyy-MM-dd HH:mm:ss"),
            date_to = query.DateTo?.ToString("yyyy-MM-dd HH:mm:ss"),
            granularity = query.Granularity.ToString(),
            app_id = query.AppId,
            event_name = query.EventName,
            os_name = query.OsName,
            app_version = query.AppVersion,
            country_code = query.CountryCode,
            device_model = query.DeviceModel,
        }, cancellationToken);

        return Ok(new PeriodicStats
        {
            Rows = rows,
            Granularity = query.Granularity.ToString().ToLower()
        });
    }

    [HttpGet("/api/_stats/top-props")]
    public async Task<IActionResult> EventProps([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        var query = body.Parse(DateTime.UtcNow);
        var rows = await _queryClient.NamedQueryAsync<EventPropsItem>("top_props__v2", new {
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

    [HttpGet("/api/_stats/live-geo")]
    public async Task<IActionResult> LiveGeo([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        var query = body.Parse(DateTime.UtcNow);

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
        var query = body.Parse(DateTime.UtcNow);

        var rows = await _queryClient.NamedQueryAsync<LiveRecentSession>("live_sessions__v1", new {
            app_id = query.AppId,
        }, cancellationToken);

        return Ok(rows);
    }

    [HttpGet("/api/_stats/live-session-details")]
    public async Task<IActionResult> LiveSessionDetails([FromQuery] QueryParams body, CancellationToken cancellationToken)
    {
        var query = body.Parse(DateTime.UtcNow);

        var row = await _queryClient.NamedQuerySingleAsync<SessionTimeline>("live_session_details__v1", new {
            app_id = query.AppId,
            session_id = query.SessionId,
        }, cancellationToken);

        return Ok(row);
    }

    private async Task<KeyMetricsRow> GetKeyMetrics(QueryArgs args, CancellationToken cancellationToken)
    {
        return await _queryClient.NamedQuerySingleAsync<KeyMetricsRow>("key_metrics__v2", new {
            date_from = args.DateFrom?.ToString("yyyy-MM-dd HH:mm:ss"),
            date_to = args.DateTo?.ToString("yyyy-MM-dd HH:mm:ss"),
            app_id = args.AppId,
            event_name = args.EventName,
            os_name = args.OsName,
            app_version = args.AppVersion,
            country_code = args.CountryCode,
            device_model = args.DeviceModel,
        }, cancellationToken);
    }

    private async Task<IActionResult> TopN(string nameColumn, TopNValue valueColumn, QueryParams body, CancellationToken cancellationToken)
    {
        var query = body.Parse(DateTime.UtcNow);
        var rows = await _queryClient.NamedQueryAsync<TopNItem>("top_n__v2", new {
            name_column = nameColumn,
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
