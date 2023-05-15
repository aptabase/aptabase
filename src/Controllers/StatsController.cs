using System.Data;
using Aptabase.Application;
using Aptabase.Application.Authentication;
using Aptabase.Application.Query;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace Aptabase.Controllers;

public class PeriodicStats
{
    public IEnumerable<PeriodicStatsRow> Rows { get; set; } = Array.Empty<PeriodicStatsRow>();
    public string Granularity { get; set; } = "";
}

public class PeriodicStatsRow
{
    public string Period { get; set; } = "";
    public int Sessions { get; set; } = 0;
    public int Events { get; set; } = 0;
}

public class KeyMetrics
{
    public int Sessions { get; set; } = 0;
    public int Events { get; set; } = 0;
    public double DurationSeconds { get; set; } = 0;
}

public class TopNItem
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}

public class EventPropsItem
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public int Events { get; set; }
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
    private const string DATE_FORMAT = "yyyy-MM-dd";
    private const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";

    public string AppId { get; set; } = "";
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public Granularity Granularity { get; set; }
    public string? CountryCode { get; set; }
    public string? OsName { get; set; }
    public string? EventName { get; set; }
    public string? AppVersion { get; set; }

    public string ToFilter()
    {
        var dateFilter = (DateFrom, DateTo) switch
        {
            (null, null) => "",
            (null, DateTime to) => $"timestamp < '{to.ToString(DATE_TIME_FORMAT)}'",
            (DateTime from, null) => $"timestamp >= '{from.ToString(DATE_TIME_FORMAT)}'",
            (DateTime from, DateTime to) => $" timestamp BETWEEN '{from.ToString(DATE_TIME_FORMAT)}' AND '{to.ToString(DATE_TIME_FORMAT)}'"
        };

        List<String> conditions = new();
        conditions.Add($"app_id = '{AppId}'");

        if (!string.IsNullOrEmpty(dateFilter))
            conditions.Add(dateFilter);

        if (!string.IsNullOrEmpty(EventName))
            conditions.Add($"event_name = '{EventName}'");

        if (!string.IsNullOrEmpty(CountryCode))
            conditions.Add($"country_code = '{CountryCode}'");

        if (!string.IsNullOrEmpty(OsName))
            conditions.Add($"os_name = '{OsName}'");

        if (!string.IsNullOrEmpty(AppVersion))
            conditions.Add($"app_version = '{AppVersion}'");

        return String.Join(" AND ", conditions);
    }

    public string ToGranularPeriod(string columnOrValue)
    {
        return Granularity switch
        {
            Granularity.Hour => $"toStartOfHour({columnOrValue})",
            Granularity.Day => $"toStartOfDay({columnOrValue})",
            Granularity.Month => $"toStartOfMonth({columnOrValue})",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public string ToFill()
    {
        var toDate = (DateTime date) => ToGranularPeriod($"toDate('{date.ToString(DATE_FORMAT)}')");
        var toDateTime = (DateTime date) => ToGranularPeriod($"toDateTime('{date.ToString(DATE_TIME_FORMAT)}')");

        var (step, fn, lastStep) = Granularity switch
        {
            Granularity.Hour => ("toIntervalHour(1)", toDateTime, DateTime.UtcNow.AddHours(1)),
            Granularity.Day => ("toIntervalDay(1)", toDate, DateTime.UtcNow.AddDays(1)),
            Granularity.Month => ("toIntervalMonth(1)", toDate, DateTime.UtcNow.AddMonths(1)),
            _ => throw new ArgumentOutOfRangeException()
        };

        return (DateFrom, DateTo) switch
        {
            (null, null) => $"WITH FILL STEP {step}",
            (null, DateTime to) => $"WITH FILL TO {fn(to)} STEP {step}",
            (DateTime from, null) => $"WITH FILL FROM {fn(from)} TO {fn(lastStep)} STEP {step}",
            (DateTime from, DateTime to) => $"WITH FILL FROM {fn(from)} TO {fn(to)} STEP {step}"
        };
    }
}

public class QueryRequestBody
{
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
    private readonly IDbConnection _db;

    public StatsController(IDbConnection db, IQueryClient queryClient)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _queryClient = queryClient ?? throw new ArgumentNullException(nameof(queryClient));
    }

    [HttpGet("/api/_stats/top-countries")]
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
        var query = await ToParams(body);
        if (query == null)
            return BadRequest();


        var (row, stats) = await _queryClient.QuerySingleAsync<KeyMetrics>(
            $@"SELECT uniq(session_id) as Sessions,
                      count(*) as Events,
                      (SELECT if(isNaN(median(duration_seconds)), 0, median(duration_seconds)) FROM (
                          SELECT session_id, max(timestamp) - min(timestamp) as duration_seconds
                          FROM events
                          WHERE {query.ToFilter()}
                          GROUP BY session_id
                      )) as DurationSeconds
                FROM events
                WHERE {query.ToFilter()}", cancellationToken);

        return OkWithStats(row, stats);
    }

    [HttpGet("/api/_stats/periodic")]
    public async Task<IActionResult> PeriodicStats([FromQuery] QueryRequestBody body, CancellationToken cancellationToken)
    {
        var query = await ToParams(body);
        if (query == null)
            return BadRequest();

        var (rows, stats) = await _queryClient.QueryAsync<PeriodicStatsRow>(
            $@"SELECT
                    {query.ToGranularPeriod("timestamp")} as Period,
                    uniq(session_id) as Sessions,
                    count() as Events
                FROM events
                WHERE {query.ToFilter()}
                GROUP by {query.ToGranularPeriod("timestamp")}
                ORDER BY {query.ToGranularPeriod("timestamp")} ASC
                {query.ToFill()}", cancellationToken);

        var result = new PeriodicStats
        {
            Rows = rows,
            Granularity = query.Granularity.ToString().ToLower()
        };
        return OkWithStats(result, stats);
    }

    [HttpGet("/api/_stats/top-props")]
    public async Task<IActionResult> EventProps([FromQuery] QueryRequestBody body, CancellationToken cancellationToken)
    {
        var query = await ToParams(body);
        if (query == null)
            return BadRequest();

        var (rows, stats) = await _queryClient.QueryAsync<EventPropsItem>(
            $@"SELECT row.1 AS Key,
                      row.2 AS Value,
                      count() as Events
               FROM (
                 SELECT arrayJoin(JSONExtractKeysAndValuesRaw(string_props)) as row
                 FROM events
                 WHERE {query.ToFilter()}
               )
               GROUP BY Key, Value
               ORDER BY Key, Events DESC", cancellationToken);

        return OkWithStats(rows, stats);
    }

    private async Task<IActionResult> TopN(string fieldName, TopNValue value, QueryRequestBody body, CancellationToken cancellationToken)
    {
        var query = await ToParams(body);
        if (query == null)
            return BadRequest();

        var valueField = value switch
        {
            TopNValue.TotalEvents => "count()",
            TopNValue.UniqueSessions => "uniq(session_id)",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        var (rows, stats) = await _queryClient.QueryAsync<TopNItem>(
            $@"SELECT {fieldName} as Name,
                      {valueField} as Value
              FROM events
              WHERE {query.ToFilter()}
              GROUP BY Name
              ORDER BY Value DESC", cancellationToken);

        return OkWithStats(rows, stats);
    }

    private IActionResult OkWithStats(object value, QueryStatistics stats)
    {
        this.Response.Headers.Add("x-stats-bytes", stats.BytesRead.ToString());
        this.Response.Headers.Add("x-stats-rows", stats.RowsRead.ToString());
        return Ok(value);
    }

    private async Task<QueryParams?> ToParams(QueryRequestBody body)
    {
        var allowed = await HasAccessTo(body.AppId);
        if (!allowed)
            return null;

        (DateTime? dateFrom, DateTime? dateTo, Granularity granularity) = body.Period switch
        {
            "24h" => (DateTime.UtcNow.AddHours(-24), null, Granularity.Hour),
            "48h" => (DateTime.UtcNow.AddHours(-48), null, Granularity.Hour),
            "7d" => (DateTime.UtcNow.Date.AddDays(-7), null, Granularity.Day),
            "14d" => (DateTime.UtcNow.Date.AddDays(-14), null, Granularity.Day),
            "30d" => (DateTime.UtcNow.Date.AddDays(-30), null, Granularity.Day),
            "90d" => (DateTime.UtcNow.Date.AddDays(-90), null, Granularity.Day),
            "180d" => (DateTime.UtcNow.Date.AddDays(-180), null, Granularity.Month),
            "365d" => (DateTime.UtcNow.Date.AddDays(-365), null, Granularity.Month),
            "month" => (new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1), null, Granularity.Day),
            "last-month" => (new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-1), new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1), Granularity.Day),
            "all" => (default(DateTime?), default(DateTime?), Granularity.Month),
            _ => (DateTime.UtcNow.AddHours(-24), null, Granularity.Hour), // default to 24 hours
        };

        return new QueryParams
        {
            AppId = body.AppId,
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
        var id = await _db.ExecuteScalarAsync<string>(
            @"SELECT id
              FROM apps
              WHERE id = @appId
              AND owner_id = @userId",
              new { appId, userId = user.Id }
        );
        return id == appId;
    }
}
