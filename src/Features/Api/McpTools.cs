using System.ComponentModel;
using System.Text.Json;
using Aptabase.Features.OAuth;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Aptabase.Features.Api;

/// <summary>
/// The MCP surface of the query API. Each tool is a thin shell over the same query layer
/// the REST endpoints use, so validation, access checks, caching and metering are shared
/// and a tool call counts exactly like a REST request.
/// </summary>
[McpServerToolType]
public class McpTools
{
    // Which scope each tool needs. The gate reads this to answer a call on a token without
    // the scope with an HTTP 403 and an insufficient_scope challenge, which is what lets an
    // MCP client start a step-up authorization instead of showing a tool error.
    public static readonly IReadOnlyDictionary<string, string> RequiredScopes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["list_apps"] = OAuthScopes.StatsRead,
        ["get_metrics"] = OAuthScopes.StatsRead,
        ["get_timeseries"] = OAuthScopes.StatsRead,
        ["get_breakdown"] = OAuthScopes.StatsRead,
        ["get_event_props"] = OAuthScopes.StatsRead,
        ["get_usage"] = OAuthScopes.StatsRead,
        ["list_sessions"] = OAuthScopes.SessionsRead,
        ["get_session"] = OAuthScopes.SessionsRead,
    };

    private const string AppIdHelp = "Aptabase app id (from list_apps)";
    private const string BuildModeHelp = "release (default) or debug. Debug builds are stored and queried separately.";
    private const string PeriodHelp = "Period preset: 24h, 48h, today, yesterday, 7d, 14d, 30d, month, last-month, 90d, 180d, 365d, all. Prefer this over startDate/endDate. Defaults to 24h when neither is set. Long periods may exceed what the app owner's plan allows.";
    private const string StartDateHelp = "ISO 8601 start date-time, UTC (use with endDate instead of period)";
    private const string EndDateHelp = "ISO 8601 end date-time, UTC (use with startDate instead of period)";
    private const string GranularityHelp = "hour, day or month. Derived from the period when omitted. hour is only available for windows of up to 7 days.";

    private readonly IApiQueryService _queries;
    private readonly IHttpContextAccessor _http;

    public McpTools(IApiQueryService queries, IHttpContextAccessor http)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    private ApiPrincipal Principal => (_http.HttpContext ?? throw new InvalidOperationException("No HTTP request.")).GetApiPrincipal();

    [McpServerTool(Name = "list_apps", Title = "List apps", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("List the Aptabase apps this connection can read (id, name, ownership). Call this first to get an app id.")]
    public async Task<CallToolResult> ListApps(CancellationToken cancellationToken)
        => Result(await _queries.ListApps(Principal, cancellationToken));

    [McpServerTool(Name = "get_metrics", Title = "Key metrics", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Totals for an app over a period: daily users, sessions, events and median session duration in seconds.")]
    public async Task<CallToolResult> GetMetrics(
        [Description(AppIdHelp)] string appId,
        [Description(BuildModeHelp)] string? buildMode = null,
        [Description(PeriodHelp)] string? period = null,
        [Description(StartDateHelp)] string? startDate = null,
        [Description(EndDateHelp)] string? endDate = null,
        [Description(GranularityHelp)] string? granularity = null,
        [Description("Filter by event name")] string? eventName = null,
        [Description("Filter by OS name (e.g. iOS, Android, macOS)")] string? osName = null,
        [Description("Filter by app version")] string? appVersion = null,
        [Description("Filter by ISO country code")] string? countryCode = null,
        [Description("Filter by device model")] string? deviceModel = null,
        CancellationToken cancellationToken = default)
        => Result(await _queries.GetMetrics(Principal, appId, Stats(new StatsRequest(), buildMode, period, startDate, endDate, granularity, eventName, osName, appVersion, countryCode, deviceModel), cancellationToken));

    [McpServerTool(Name = "get_timeseries", Title = "Metrics over time", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Users, sessions and events per hour, day or month, for charts and trends.")]
    public async Task<CallToolResult> GetTimeseries(
        [Description(AppIdHelp)] string appId,
        [Description(BuildModeHelp)] string? buildMode = null,
        [Description(PeriodHelp)] string? period = null,
        [Description(StartDateHelp)] string? startDate = null,
        [Description(EndDateHelp)] string? endDate = null,
        [Description(GranularityHelp)] string? granularity = null,
        [Description("Filter by event name")] string? eventName = null,
        [Description("Filter by OS name (e.g. iOS, Android, macOS)")] string? osName = null,
        [Description("Filter by app version")] string? appVersion = null,
        [Description("Filter by ISO country code")] string? countryCode = null,
        [Description("Filter by device model")] string? deviceModel = null,
        CancellationToken cancellationToken = default)
        => Result(await _queries.GetTimeseries(Principal, appId, Stats(new StatsRequest(), buildMode, period, startDate, endDate, granularity, eventName, osName, appVersion, countryCode, deviceModel), cancellationToken));

    [McpServerTool(Name = "get_breakdown", Title = "Top values of a dimension", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Top values of one dimension for the period: events, countries, regions, operating systems, OS versions, devices, app versions or build numbers.")]
    public async Task<CallToolResult> GetBreakdown(
        [Description(AppIdHelp)] string appId,
        [Description("One of: event, country, region, os, os_version, device, app_version, app_build_number")] string dimension,
        [Description("sessions (unique sessions) or events (total events). Defaults to events for the event dimension and sessions otherwise.")] string? metric = null,
        [Description("How many values to return, 1 to 100. Defaults to 10.")] int? limit = null,
        [Description(BuildModeHelp)] string? buildMode = null,
        [Description(PeriodHelp)] string? period = null,
        [Description(StartDateHelp)] string? startDate = null,
        [Description(EndDateHelp)] string? endDate = null,
        [Description(GranularityHelp)] string? granularity = null,
        [Description("Filter by event name")] string? eventName = null,
        [Description("Filter by OS name (e.g. iOS, Android, macOS)")] string? osName = null,
        [Description("Filter by app version")] string? appVersion = null,
        [Description("Filter by ISO country code")] string? countryCode = null,
        [Description("Filter by device model")] string? deviceModel = null,
        CancellationToken cancellationToken = default)
    {
        var request = Stats(new BreakdownRequest { Dimension = dimension, Metric = metric, Limit = limit }, buildMode, period, startDate, endDate, granularity, eventName, osName, appVersion, countryCode, deviceModel);
        return Result(await _queries.GetBreakdown(Principal, appId, request, cancellationToken));
    }

    [McpServerTool(Name = "get_event_props", Title = "Event properties", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Breakdown of the custom string and numeric properties of one event name.")]
    public async Task<CallToolResult> GetEventProps(
        [Description(AppIdHelp)] string appId,
        [Description("Event name whose properties to analyze")] string eventName,
        [Description(BuildModeHelp)] string? buildMode = null,
        [Description(PeriodHelp)] string? period = null,
        [Description(StartDateHelp)] string? startDate = null,
        [Description(EndDateHelp)] string? endDate = null,
        [Description(GranularityHelp)] string? granularity = null,
        [Description("Filter by OS name (e.g. iOS, Android, macOS)")] string? osName = null,
        [Description("Filter by app version")] string? appVersion = null,
        [Description("Filter by ISO country code")] string? countryCode = null,
        [Description("Filter by device model")] string? deviceModel = null,
        CancellationToken cancellationToken = default)
        => Result(await _queries.GetEventProps(Principal, appId, Stats(new StatsRequest(), buildMode, period, startDate, endDate, granularity, eventName, osName, appVersion, countryCode, deviceModel), cancellationToken));

    [McpServerTool(Name = "list_sessions", Title = "List sessions", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Individual sessions of an app: the sessions of the last hour with live=true, or past sessions inside a bounded period, newest first, ten per page. Needs the sessions permission.")]
    public async Task<CallToolResult> ListSessions(
        [Description(AppIdHelp)] string appId,
        [Description("true for the sessions of the last hour. When false or omitted, a period or a date range is required.")] bool live = false,
        [Description(BuildModeHelp)] string? buildMode = null,
        [Description("Period preset: 24h, 48h, today, yesterday, 7d, 14d, 30d, month, last-month, 90d, 180d, 365d")] string? period = null,
        [Description(StartDateHelp)] string? startDate = null,
        [Description(EndDateHelp)] string? endDate = null,
        [Description("Filter by event name")] string? eventName = null,
        [Description("Filter by OS name (e.g. iOS, Android, macOS)")] string? osName = null,
        [Description("Filter by app version")] string? appVersion = null,
        [Description("Filter by ISO country code")] string? countryCode = null,
        [Description("next_cursor from the previous page, to get the next ten")] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var request = Stats(new SessionsRequest { Live = live, Cursor = cursor }, buildMode, period, startDate, endDate, null, eventName, osName, appVersion, countryCode, null);
        return Result(await _queries.ListSessions(Principal, appId, request, cancellationToken));
    }

    [McpServerTool(Name = "get_session", Title = "Session timeline", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("The timeline of one session: its events with their properties, plus device and location metadata. Needs the sessions permission.")]
    public async Task<CallToolResult> GetSession(
        [Description(AppIdHelp)] string appId,
        [Description("Session id from list_sessions")] string sessionId,
        [Description(BuildModeHelp)] string? buildMode = null,
        CancellationToken cancellationToken = default)
        => Result(await _queries.GetSession(Principal, appId, sessionId, buildMode, cancellationToken));

    [McpServerTool(Name = "get_usage", Title = "Usage", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Monthly event volume of an app (release and debug), its API requests this month, and how much of the owner's monthly API allowance is left.")]
    public async Task<CallToolResult> GetUsage(
        [Description(AppIdHelp)] string appId,
        CancellationToken cancellationToken = default)
        => Result(await _queries.GetUsage(Principal, appId, cancellationToken));

    private static T Stats<T>(T request, string? buildMode, string? period, string? startDate, string? endDate, string? granularity,
        string? eventName, string? osName, string? appVersion, string? countryCode, string? deviceModel) where T : StatsRequest
    {
        request.BuildMode = buildMode;
        request.Period = period;
        request.From = startDate;
        request.To = endDate;
        request.Granularity = granularity;
        request.EventName = eventName;
        request.OsName = osName;
        request.AppVersion = appVersion;
        request.CountryCode = countryCode;
        request.DeviceModel = deviceModel;
        return request;
    }

    private static CallToolResult Result(ApiResult result)
    {
        if (result.Error is null)
            return new CallToolResult { Content = [new TextContentBlock { Text = JsonSerializer.Serialize(result.Data, ApiJson.Options) }] };

        return new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = JsonSerializer.Serialize(ApiJson.ErrorBody(result.Error), ApiJson.Options) }],
        };
    }
}
