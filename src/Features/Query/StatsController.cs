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

        var where = conditions.Count > 0 ? $"WHERE {String.Join(" AND ", conditions)}" : string.Empty;

        return $"PREWHERE app_id = '{AppId}' {where}";
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

        var rows = await _queryClient.QueryAsync<PeriodicStatsRow>(
            $@"SELECT
                    {query.ToGranularPeriod("timestamp")} as Period,
                    uniqExact(user_id) as Users,
                    uniqExact(session_id) as Sessions,
                    count() as Events
                FROM events
                {query.ToFilter()}
                GROUP by {query.ToGranularPeriod("timestamp")}
                ORDER BY {query.ToGranularPeriod("timestamp")} ASC
                {query.ToFill()}", cancellationToken);

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

        var rows = await _queryClient.QueryAsync<EventPropsItem>(
            $@"SELECT string_arr.1 as StringKey,
                      string_arr.2 as StringValue,
                      numeric_arr.1 as NumericKey,
                      count() as Events,
                      median(numeric_arr.2) as Median,
                      min(numeric_arr.2) as Min,
                      max(numeric_arr.2) as Max,
                      sum(numeric_arr.2) as Sum
                FROM (
                    SELECT * FROM (
                        SELECT JSONExtractKeysAndValues(string_props, 'String') as string_arr, JSONExtractKeysAndValues(numeric_props, 'Float') as numeric_arr
                        FROM events
                        {query.ToFilter()}
                    ) LEFT ARRAY JOIN string_arr
                ) LEFT ARRAY JOIN numeric_arr
                GROUP BY StringKey, StringValue, NumericKey
                ORDER BY StringKey, Events DESC", cancellationToken);

        return Ok(rows);
    }

    private async Task<KeyMetricsRow> GetKeyMetrics(QueryParams query, CancellationToken cancellationToken)
    {
        return await _queryClient.QuerySingleAsync<KeyMetricsRow>(
            $@"SELECT uniqExact(user_id) / (date_diff('day', min(min), max(max)) + 1) as DailyUsers,
                      uniqExact(session_id) as Sessions,
                      if(isNaN(median(max - min)), 0, median(max - min)) as DurationSeconds,
                      sum(count) as Events
            FROM (
                    SELECT min(timestamp) as min, max(timestamp) as max, user_id, session_id, count(*) as count
                    FROM events
                    {query.ToFilter()}
                    GROUP BY user_id, session_id
            )", cancellationToken);
    }

    private async Task<IActionResult> TopN(string fieldName, TopNValue value, QueryRequestBody body, CancellationToken cancellationToken)
    {
        var query = await ToParams(body, DateTime.UtcNow);
        if (query == null)
            return BadRequest();

        var valueField = value switch
        {
            TopNValue.TotalEvents => "count()",
            TopNValue.UniqueSessions => "uniqExact(session_id)",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        var rows = await _queryClient.QueryAsync<TopNItem>(
            $@"SELECT {fieldName} as Name,
                      {valueField} as Value
              FROM events
              {query.ToFilter()}
              GROUP BY Name
              ORDER BY Value DESC", cancellationToken);

        return Ok(rows);
    }

    private async Task<QueryParams?> ToParams(QueryRequestBody body, DateTime relativeTo)
    {
        var allowed = await HasAccessTo(body.AppId);
        if (!allowed)
            return null;

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
