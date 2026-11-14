using System.Globalization;
using Aptabase.Features.Stats;
using Microsoft.AspNetCore.Mvc;

namespace Aptabase.Features.Api;

public class ApiError
{
    public int Status { get; init; }
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";
    public int? RetryAfterSeconds { get; init; }
    public string? RequiredScope { get; init; }
    public QuotaDecision? Quota { get; init; }

    public static ApiError BadRequest(string message) => new() { Status = 400, Code = "invalid_request", Message = message };

    // One answer for a missing app, a foreign app and an app that dropped out of the
    // grant, so app ids cannot be probed
    public static ApiError Forbidden() => new() { Status = 403, Code = "forbidden", Message = "This token cannot access the requested app." };

    public static ApiError InsufficientScope(string scope) => new()
    {
        Status = 403,
        Code = "insufficient_scope",
        Message = $"This token does not have the {scope} permission.",
        RequiredScope = scope,
    };

    public static ApiError Busy(string message) => new() { Status = 429, Code = "busy", Message = message, RetryAfterSeconds = 1 };

    public static ApiError NotFound(string message) => new() { Status = 404, Code = "not_found", Message = message };
}

public class ApiResult
{
    public object? Data { get; init; }
    public ApiError? Error { get; init; }

    public static ApiResult Ok(object data) => new() { Data = data };
    public static implicit operator ApiResult(ApiError error) => new() { Error = error };
}

/// <summary>
/// The filters every stats request shares. The same class binds REST query strings and
/// carries MCP tool arguments, so both surfaces go through one validation.
/// </summary>
public class StatsRequest
{
    [FromQuery(Name = "build_mode")] public string? BuildMode { get; set; }
    [FromQuery(Name = "period")] public string? Period { get; set; }
    [FromQuery(Name = "from")] public string? From { get; set; }
    [FromQuery(Name = "to")] public string? To { get; set; }
    [FromQuery(Name = "granularity")] public string? Granularity { get; set; }
    [FromQuery(Name = "country_code")] public string? CountryCode { get; set; }
    [FromQuery(Name = "os_name")] public string? OsName { get; set; }
    [FromQuery(Name = "device_model")] public string? DeviceModel { get; set; }
    [FromQuery(Name = "event_name")] public string? EventName { get; set; }
    [FromQuery(Name = "app_version")] public string? AppVersion { get; set; }
}

public class BreakdownRequest : StatsRequest
{
    [FromQuery(Name = "dimension")] public string? Dimension { get; set; }
    [FromQuery(Name = "metric")] public string? Metric { get; set; }
    [FromQuery(Name = "limit")] public int? Limit { get; set; }
}

public class SessionsRequest : StatsRequest
{
    [FromQuery(Name = "live")] public bool Live { get; set; }
    [FromQuery(Name = "cursor")] public string? Cursor { get; set; }
}

public record ApiWindow(DateTime? From, DateTime? To, GranularityEnum Granularity)
{
    public const string DateFormat = "yyyy-MM-dd HH:mm:ss";

    public string? FromArg => From?.ToString(DateFormat, CultureInfo.InvariantCulture);
    public string? ToArg => To?.ToString(DateFormat, CultureInfo.InvariantCulture);

    public object Describe() => new
    {
        From = From.HasValue ? DateTime.SpecifyKind(From.Value, DateTimeKind.Utc) : (DateTime?)null,
        To = To.HasValue ? DateTime.SpecifyKind(To.Value, DateTimeKind.Utc) : (DateTime?)null,
        Granularity = Granularity.ToString().ToLowerInvariant(),
    };
}

public static class ApiValidation
{
    public const int MaxHourlyWindowDays = 7;
    public const int MaxFilterLength = 200;

    public static readonly string[] Periods =
        ["24h", "48h", "today", "yesterday", "7d", "14d", "30d", "month", "last-month", "90d", "180d", "365d", "all"];

    // The only way a column name reaches a query: from this table, never from the caller
    public static readonly IReadOnlyDictionary<string, string> Dimensions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["country"] = "country_code",
        ["region"] = "region_name",
        ["os"] = "os_name",
        ["os_version"] = "os_version",
        ["device"] = "device_model",
        ["app_version"] = "app_version",
        ["app_build_number"] = "app_build_number",
        ["event"] = "event_name",
    };

    public static bool IsValidId(string? value)
        => value is { Length: > 0 and <= 22 } && value.All(char.IsAsciiLetterOrDigit);

    public static bool TryParseBuildMode(string? value, out string buildMode)
    {
        buildMode = string.IsNullOrEmpty(value) ? "release" : value.ToLowerInvariant();
        return buildMode is "release" or "debug";
    }

    /// <summary>
    /// Filters are passed on as plain strings. Commas are refused because the pipes would
    /// read them as a list, and there is no array parameter in the public shape yet.
    /// </summary>
    public static ApiError? ValidateFilters(StatsRequest request)
    {
        foreach (var (name, value) in new[]
        {
            ("country_code", request.CountryCode), ("os_name", request.OsName), ("device_model", request.DeviceModel),
            ("event_name", request.EventName), ("app_version", request.AppVersion),
        })
        {
            if (value is null) continue;
            if (value.Length > MaxFilterLength) return ApiError.BadRequest($"{name} is too long.");
            if (value.Contains(',')) return ApiError.BadRequest($"{name} must be a single value, commas are not supported.");
        }

        return null;
    }

    /// <summary>
    /// Resolves a period preset or an explicit range to a window, and enforces the two
    /// caps that keep a query cheap: hourly buckets only for short windows, and the
    /// longest window the app owner's plan allows.
    /// </summary>
    public static (ApiWindow? Window, ApiError? Error) ResolveWindow(StatsRequest request, DateTime utcNow, int? maxWindowDays)
    {
        DateTime? from, to;
        GranularityEnum granularity;

        var hasRange = !string.IsNullOrEmpty(request.From) || !string.IsNullOrEmpty(request.To);
        if (hasRange)
        {
            if (!string.IsNullOrEmpty(request.Period))
                return (null, ApiError.BadRequest("Use either period or from and to, not both."));

            if (!TryParseDate(request.From, out var parsedFrom) || !TryParseDate(request.To, out var parsedTo))
                return (null, ApiError.BadRequest("from and to must both be ISO 8601 date-times."));

            if (parsedTo <= parsedFrom)
                return (null, ApiError.BadRequest("to must be later than from."));

            from = parsedFrom;
            to = parsedTo;
            var span = parsedTo - parsedFrom;
            granularity = span.TotalHours < 72 ? GranularityEnum.Hour : span.TotalDays > 90 ? GranularityEnum.Month : GranularityEnum.Day;
        }
        else
        {
            var period = string.IsNullOrEmpty(request.Period) ? "24h" : request.Period;
            if (!Periods.Contains(period))
                return (null, ApiError.BadRequest($"period must be one of: {string.Join(", ", Periods)}."));

            (from, to, granularity) = ResolvePeriod(period, utcNow);
        }

        if (!string.IsNullOrEmpty(request.Granularity))
        {
            if (!Enum.TryParse(request.Granularity, ignoreCase: true, out granularity) || !Enum.IsDefined(granularity))
                return (null, ApiError.BadRequest("granularity must be hour, day or month."));
        }

        var days = from.HasValue && to.HasValue ? (to.Value - from.Value).TotalDays : double.PositiveInfinity;

        // The hourly branch of every pipe scans raw events
        if (granularity == GranularityEnum.Hour && days > MaxHourlyWindowDays)
            return (null, ApiError.BadRequest($"Hourly granularity is available for windows of up to {MaxHourlyWindowDays} days."));

        // A day of slack, so that a calendar month fits a 30-day cap
        if (maxWindowDays.HasValue && days > maxWindowDays.Value + 1)
            return (null, ApiError.BadRequest($"The plan of this app's owner allows queries over at most {maxWindowDays} days at a time."));

        return (new ApiWindow(from, to, granularity), null);
    }

    private static bool TryParseDate(string? value, out DateTime result)
    {
        result = default;
        if (string.IsNullOrEmpty(value) || value.Length > 40)
            return false;

        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return false;

        result = parsed.UtcDateTime;
        return true;
    }

    // The dashboard's presets, in UTC. Rolling presets are anchored to the last five-minute
    // mark, so the same question asked twice in a row is the same query and can be
    // answered from the cache.
    public static (DateTime? From, DateTime? To, GranularityEnum Granularity) ResolvePeriod(string period, DateTime utcNow)
    {
        var now = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, utcNow.Minute / 5 * 5, 0, DateTimeKind.Utc);
        var startOfDay = now.Date;
        var endOfDay = startOfDay.AddDays(1).AddSeconds(-1);
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return period switch
        {
            "24h" => (now.AddHours(-24), now, GranularityEnum.Hour),
            "48h" => (now.AddHours(-48), now, GranularityEnum.Hour),
            "today" => (startOfDay, now, GranularityEnum.Hour),
            "yesterday" => (startOfDay.AddDays(-1), startOfDay.AddSeconds(-1), GranularityEnum.Hour),
            "7d" => (endOfDay.AddDays(-7), endOfDay, GranularityEnum.Day),
            "14d" => (endOfDay.AddDays(-14), endOfDay, GranularityEnum.Day),
            "30d" => (endOfDay.AddDays(-30), endOfDay, GranularityEnum.Day),
            "month" => (startOfMonth, endOfDay, GranularityEnum.Day),
            "last-month" => (startOfMonth.AddMonths(-1), startOfMonth.AddSeconds(-1), GranularityEnum.Day),
            "90d" => (endOfDay.AddDays(-90), endOfDay, GranularityEnum.Day),
            "180d" => (endOfDay.AddDays(-180), endOfDay, GranularityEnum.Month),
            "365d" => (endOfDay.AddDays(-365), endOfDay, GranularityEnum.Month),
            _ => (null, null, GranularityEnum.Month),
        };
    }
}
