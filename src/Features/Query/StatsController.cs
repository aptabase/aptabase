using Dapper;
using Aptabase.Data;
using Aptabase.Features.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Aptabase.Features.Query;

public class PeriodicStats
{
    public IEnumerable<PeriodicStatsRow> Rows { get; set; } = Array.Empty<PeriodicStatsRow>();
    public string Granularity { get; set; } = "";
}

public class PeriodicStatsRow
{
    public string Period { get; set; } = "";
    public int Users { get; set; } = 0;
    public int Sessions { get; set; } = 0;
    public int Events { get; set; } = 0;
}

public class KeyMetrics
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

public class KeyMetricsRow
{
    public double DailyUsers { get; set; } = 0;
    public int Sessions { get; set; } = 0;
    public int Events { get; set; } = 0;
    public double DurationSeconds { get; set; } = 0;
}

public class TopNItem
{
    public string Name { get; set; } = "";
    public double Value { get; set; }
}

public class EventPropKeys
{
    public string[] StringKeys { get; set; } = Array.Empty<string>();
    public string[] NumericKeys { get; set; } = Array.Empty<string>();
}

public class EventPropsItem
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

public enum TopNValue
{
    UniqueSessions,
    TotalEvents
}

public enum Granularity
{
    Hour,
    Day,
    Month
}

public class QueryParams
{
    public string AppId { get; set; } = "";
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public Granularity Granularity { get; set; }
    public string? CountryCode { get; set; }
    public string? OsName { get; set; }
    public string? EventName { get; set; }
    public string? AppVersion { get; set; }
}

public class QueryRequestBody
{
    public string BuildMode { get; set; } = "";
    public string AppId { get; set; } = "";
    public string? Period { get; set; }
    public string? CountryCode { get; set; }
    public string? OsName { get; set; }
    public string? EventName { get; set; }
    public string? AppVersion { get; set; }
}

[ApiController, IsAuthenticated]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class StatsController : Controller
{
    private readonly IQueryClient _queryClient;
    private readonly IDbContext _db;

    public StatsController(IDbContext db, IQueryClient queryClient)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _queryClient = queryClient ?? throw new ArgumentNullException(nameof(queryClient));
    }

    [HttpGet("/api/_stats/top-countries"), AccessControl]
    public async Task<IActionResult> TopCountries([FromQuery] QueryRequestBody body, CancellationToken cancellationToken)
    {
        return await TopN("country_code", TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-osversions")]
    public async Task<IActionResult> TopOSVersions([FromQuery] QueryRequestBody body, CancellationToken cancellationToken)
    {
        return await TopN("os_version", TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-operatingsystems")]
    public async Task<IActionResult> TopOperatingSystems([FromQuery] QueryRequestBody body, CancellationToken cancellationToken)
    {
        return await TopN("os_name", TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-regions")]
    public async Task<IActionResult> TopRegions([FromQuery] QueryRequestBody body, CancellationToken cancellationToken)
    {
        return await TopN("region_name", TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-events")]
    public async Task<IActionResult> TopEvents([FromQuery] QueryRequestBody body, CancellationToken cancellationToken)
    {
        return await TopN("event_name", TopNValue.TotalEvents, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-appversions")]
    public async Task<IActionResult> TopAppVersions([FromQuery] QueryRequestBody body, CancellationToken cancellationToken)
    {
        return await TopN("app_version", TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/top-appbuildnumbers")]
    public async Task<IActionResult> TopAppBuildNumbers([FromQuery] QueryRequestBody body, CancellationToken cancellationToken)
    {
        return await TopN("app_build_number", TopNValue.UniqueSessions, body, cancellationToken);
    }

    [HttpGet("/api/_stats/metrics")]
    public async Task<IActionResult> KeyMetrics([FromQuery] QueryRequestBody body, CancellationToken cancellationToken)
    {
        var currentQuery = await ToParams(body, DateTime.UtcNow);
        if (currentQuery == null)
            return BadRequest();

        var current = await GetKeyMetrics(currentQuery, cancellationToken);

        if (!currentQuery.DateFrom.HasValue) {
            return Ok(new KeyMetrics(current));
        }

        var previousQuery = await ToParams(body, currentQuery.DateFrom.Value);
        if (previousQuery == null)
            return BadRequest();

        var previous = await GetKeyMetrics(previousQuery, cancellationToken);
        return Ok(new KeyMetrics(current, previous));
    }

    [HttpGet("/api/_stats/periodic")]
    public async Task<IActionResult> PeriodicStats([FromQuery] QueryRequestBody body, CancellationToken cancellationToken)
    {
        var query = await ToParams(body, DateTime.UtcNow);
        if (query == null)
            return BadRequest();

        var rows = await _queryClient.NamedQueryAsync<PeriodicStatsRow>("key_metrics_periodic__v1", new {
            date_from = query.DateFrom?.ToString("yyyy-MM-dd HH:mm:ss"),
            date_to = query.DateTo?.ToString("yyyy-MM-dd HH:mm:ss"),
            granularity = query.Granularity.ToString(),
            app_id = query.AppId,
            event_name = query.EventName,
            os_name = query.OsName,
            app_version = query.AppVersion,
            country_code = query.CountryCode,
        }, cancellationToken);

        return Ok(new PeriodicStats
        {
            Rows = rows,
            Granularity = query.Granularity.ToString().ToLower()
        });
    }

    [HttpGet("/api/_stats/top-props")]
    public async Task<IActionResult> EventProps([FromQuery] QueryRequestBody body, CancellationToken cancellationToken)
    {
        var query = await ToParams(body, DateTime.UtcNow);
        if (query == null)
            return BadRequest();

        var rows = await _queryClient.NamedQueryAsync<EventPropsItem>("top_props__v1", new {
            date_from = query.DateFrom?.ToString("yyyy-MM-dd HH:mm:ss"),
            date_to = query.DateTo?.ToString("yyyy-MM-dd HH:mm:ss"),
            app_id = query.AppId,
            event_name = query.EventName,
            os_name = query.OsName,
            app_version = query.AppVersion,
            country_code = query.CountryCode,
        }, cancellationToken);

        return Ok(rows);
    }

    private async Task<KeyMetricsRow> GetKeyMetrics(QueryParams query, CancellationToken cancellationToken)
    {
        return await _queryClient.NamedQuerySingleAsync<KeyMetricsRow>("key_metrics__v1", new {
            date_from = query.DateFrom?.ToString("yyyy-MM-dd HH:mm:ss"),
            date_to = query.DateTo?.ToString("yyyy-MM-dd HH:mm:ss"),
            app_id = query.AppId,
                event_name = query.EventName,
            os_name = query.OsName,
            app_version = query.AppVersion,
            country_code = query.CountryCode,
        }, cancellationToken);
    }

    private async Task<IActionResult> TopN(string nameColumn, TopNValue valueColumn, QueryRequestBody body, CancellationToken cancellationToken)
    {
        var query = await ToParams(body, DateTime.UtcNow);
        if (query == null)
            return BadRequest();

        var rows = await _queryClient.NamedQueryAsync<TopNItem>("top_n__v1", new {
            name_column = nameColumn,
            value_column = valueColumn.ToString(),
            date_from = query.DateFrom?.ToString("yyyy-MM-dd HH:mm:ss"),
            date_to = query.DateTo?.ToString("yyyy-MM-dd HH:mm:ss"),
            app_id = query.AppId,
            event_name = query.EventName,
            os_name = query.OsName,
            app_version = query.AppVersion,
            country_code = query.CountryCode,
        }, cancellationToken);

        return Ok(rows);
    }

    private async Task<QueryParams> ToParams(QueryRequestBody body, DateTime relativeTo)
    {
        // Rest of the method...
    }

        // Go back 1 second so that when relativeTo is 00:00:00 we start with the previous day
        // Happens when looking for previous period and "period" is "month" or "last-month"
        if (relativeTo.TimeOfDay == TimeSpan.Zero)
            relativeTo = relativeTo.AddSeconds(-1);

        (DateTime? dateFrom, DateTime? dateTo, Granularity granularity) = body.Period switch
        {
            "24h" => (relativeTo.AddHours(-24), relativeTo, Granularity.Hour),
            "48h" => (relativeTo.AddHours(-48), relativeTo, Granularity.Hour),
            "7d" => (relativeTo.Date.AddDays(-7), relativeTo, Granularity.Day),
            "14d" => (relativeTo.Date.AddDays(-14), relativeTo, Granularity.Day),
            "30d" => (relativeTo.Date.AddDays(-30), relativeTo, Granularity.Day),
            "90d" => (relativeTo.Date.AddDays(-90), relativeTo, Granularity.Day),
            "180d" => (relativeTo.Date.AddDays(-180), relativeTo, Granularity.Month),
            "365d" => (relativeTo.Date.AddDays(-365), relativeTo, Granularity.Month),
            "month" => (new DateTime(relativeTo.Year, relativeTo.Month, 1), new DateTime(relativeTo.Year, relativeTo.Month, 1).AddMonths(1).AddDays(-1), Granularity.Day),
            "last-month" => (new DateTime(relativeTo.Year, relativeTo.Month, 1).AddMonths(-1), new DateTime(relativeTo.Year, relativeTo.Month, 1), Granularity.Day),
            "all" => (default(DateTime?), default(DateTime?), Granularity.Month),
            _ => (relativeTo.AddHours(-24), relativeTo, Granularity.Hour), // default to 24 hours
        };

        if ((dateFrom is null && dateTo is not null) || (dateFrom is not null && dateTo is null))
            throw new ArgumentException("Both dateFrom and dateTo must be defined, or both must be null");

        var appId = body.BuildMode.ToLower() switch
        {
            "debug" => $"{body.AppId}_DEBUG",
            _ => body.AppId,
        };

        return new QueryParams
        {
            AppId = appId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Granularity = granularity,
            CountryCode = body.CountryCode,
            OsName = body.OsName,
            EventName = body.EventName,
            AppVersion = body.AppVersion,
        };
    }

    private async Task<bool> HasAccessTo(string appId)
    {
        var user = this.GetCurrentUser();
        var id = await _db.Connection.ExecuteScalarAsync<string>(
            @"SELECT a.id
              FROM apps a
              LEFT JOIN app_shares s
              ON s.app_id = a.id
              WHERE a.id = @appId
              AND (a.owner_id = @userId OR s.email = @userEmail)
              LIMIT 1",
              new { appId, userId = user.Id, userEmail = user.Email }
        );
        return id == appId;
    }
}
