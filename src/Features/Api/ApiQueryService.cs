using System.Globalization;
using Aptabase.Features.Export;
using Aptabase.Features.OAuth;
using Aptabase.Features.Stats;
using Microsoft.Extensions.Caching.Memory;

namespace Aptabase.Features.Api;

public interface IApiQueryService
{
    Task<ApiResult> ListApps(ApiPrincipal principal, CancellationToken cancellationToken);
    Task<ApiResult> GetMetrics(ApiPrincipal principal, string appId, StatsRequest request, CancellationToken cancellationToken);
    Task<ApiResult> GetTimeseries(ApiPrincipal principal, string appId, StatsRequest request, CancellationToken cancellationToken);
    Task<ApiResult> GetBreakdown(ApiPrincipal principal, string appId, BreakdownRequest request, CancellationToken cancellationToken);
    Task<ApiResult> GetEventProps(ApiPrincipal principal, string appId, StatsRequest request, CancellationToken cancellationToken);
    Task<ApiResult> ListSessions(ApiPrincipal principal, string appId, SessionsRequest request, CancellationToken cancellationToken);
    Task<ApiResult> GetSession(ApiPrincipal principal, string appId, string sessionId, string? buildMode, CancellationToken cancellationToken);
    Task<ApiResult> GetUsage(ApiPrincipal principal, string appId, CancellationToken cancellationToken);
}

/// <summary>
/// The one query layer behind /api/v1 and /mcp. Callers describe what they want in a small
/// fixed shape; this class validates it, translates it to one of the existing pipes, and
/// puts every request through the same sequence: scope, app access, validation, cache,
/// concurrency, quota, query. There is no way to send SQL through it.
/// </summary>
public class ApiQueryService : IApiQueryService
{
    // Everything bearer traffic may query. The billing pipes are absent on purpose:
    // billing_usage_per_app__v1 takes only a period and returns every app of every customer.
    public static readonly IReadOnlySet<string> AllowedPipes = new HashSet<string>(StringComparer.Ordinal)
    {
        "key_metrics__v2",
        "key_metrics_periodic__v2",
        "top_n__v2",
        "top_props__v2",
        "live_sessions__v1",
        "historical_sessions__v1",
        "live_session_details__v1",
        "monthly_usage__v1",
    };

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AccessCacheLifetime = TimeSpan.FromSeconds(60);
    private const int SessionsPageSize = 10;

    private readonly IQueryClient _queryClient;
    private readonly IApiAccess _access;
    private readonly IApiTiers _tiers;
    private readonly IApiMetering _metering;
    private readonly IOAuthStore _store;
    private readonly ApiLimits _limits;
    private readonly IMemoryCache _cache;
    private readonly SemaphoreSlim _globalQueries;
    private readonly ILogger _logger;

    public ApiQueryService(
        IQueryClient queryClient,
        IApiAccess access,
        IApiTiers tiers,
        IApiMetering metering,
        IOAuthStore store,
        ApiLimits limits,
        IMemoryCache cache,
        EnvSettings env,
        ILogger<ApiQueryService> logger)
    {
        _queryClient = queryClient ?? throw new ArgumentNullException(nameof(queryClient));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _tiers = tiers ?? throw new ArgumentNullException(nameof(tiers));
        _metering = metering ?? throw new ArgumentNullException(nameof(metering));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _limits = limits ?? throw new ArgumentNullException(nameof(limits));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _globalQueries = new SemaphoreSlim(env.ApiMaxConcurrentQueries, env.ApiMaxConcurrentQueries);
    }

    // Starts from what the subject can read right now and narrows by the grant. The other
    // direction, trusting the stored list, would keep naming apps whose share was removed.
    public async Task<ApiResult> ListApps(ApiPrincipal principal, CancellationToken cancellationToken)
    {
        if (!principal.HasScope(OAuthScopes.StatsRead))
            return ApiError.InsufficientScope(OAuthScopes.StatsRead);

        var apps = await _access.ListReadableApps(principal.SubjectId, cancellationToken);
        return ApiResult.Ok(new
        {
            Data = apps.Where(a => principal.Grant.CoversApp(a.Id)).Select(a => new { a.Id, a.Name, a.HasOwnership }).ToArray(),
        });
    }

    public Task<ApiResult> GetMetrics(ApiPrincipal principal, string appId, StatsRequest request, CancellationToken cancellationToken)
        => Run(principal, appId, request.BuildMode, OAuthScopes.StatsRead, "metrics", tier =>
        {
            var (window, error) = ResolveStats(request, tier);
            if (window is null) return new Plan(error!);

            return new Plan(StatsArgs(request, window), async (args, ct) =>
            {
                var rows = await Query<KeyMetricsRow>("key_metrics__v2", args, ct);
                var row = rows.FirstOrDefault() ?? new KeyMetricsRow();
                return new
                {
                    Window = window.Describe(),
                    Data = new { row.DailyUsers, row.Sessions, row.Events, row.DurationSeconds },
                };
            });
        }, cancellationToken);

    public Task<ApiResult> GetTimeseries(ApiPrincipal principal, string appId, StatsRequest request, CancellationToken cancellationToken)
        => Run(principal, appId, request.BuildMode, OAuthScopes.StatsRead, "timeseries", tier =>
        {
            var (window, error) = ResolveStats(request, tier);
            if (window is null) return new Plan(error!);

            return new Plan(StatsArgs(request, window), async (args, ct) =>
            {
                var rows = await Query<PeriodicStatsRow>("key_metrics_periodic__v2", args, ct);
                return new
                {
                    Window = window.Describe(),
                    Data = rows.Select(r => new { Period = DateTime.SpecifyKind(r.Period, DateTimeKind.Utc), r.Users, r.Sessions, r.Events }).ToArray(),
                };
            });
        }, cancellationToken);

    public Task<ApiResult> GetBreakdown(ApiPrincipal principal, string appId, BreakdownRequest request, CancellationToken cancellationToken)
        => Run(principal, appId, request.BuildMode, OAuthScopes.StatsRead, "breakdown", tier =>
        {
            if (string.IsNullOrEmpty(request.Dimension) || !ApiValidation.Dimensions.TryGetValue(request.Dimension, out var column))
                return new Plan(ApiError.BadRequest($"dimension must be one of: {string.Join(", ", ApiValidation.Dimensions.Keys)}."));

            var metric = string.IsNullOrEmpty(request.Metric) ? (request.Dimension == "event" ? "events" : "sessions") : request.Metric;
            if (metric is not ("sessions" or "events"))
                return new Plan(ApiError.BadRequest("metric must be sessions or events."));

            var limit = request.Limit ?? 10;
            if (limit is < 1 or > 100)
                return new Plan(ApiError.BadRequest("limit must be between 1 and 100."));

            var (window, error) = ResolveStats(request, tier);
            if (window is null) return new Plan(error!);

            // The daily view has no build number column
            if (column == "app_build_number" && window.Granularity != GranularityEnum.Hour)
                return new Plan(ApiError.BadRequest($"The app_build_number dimension needs hourly granularity, which is available for windows of up to {ApiValidation.MaxHourlyWindowDays} days."));

            var args = StatsArgs(request, window);
            args["name_column"] = column;
            args["value_column"] = metric == "events" ? nameof(TopNValue.TotalEvents) : nameof(TopNValue.UniqueSessions);
            args["limit"] = limit.ToString(CultureInfo.InvariantCulture);

            return new Plan(args, async (queryArgs, ct) =>
            {
                // limit is part of the cache key, not a pipe parameter
                var pipeArgs = queryArgs.Where(a => a.Key != "limit").ToDictionary(a => a.Key, a => a.Value);
                var rows = await Query<TopNItem>("top_n__v2", pipeArgs, ct);
                return new
                {
                    Window = window.Describe(),
                    request.Dimension,
                    Metric = metric,
                    Data = rows.Take(limit).Select(r => new { r.Name, r.Value }).ToArray(),
                };
            });
        }, cancellationToken);

    public Task<ApiResult> GetEventProps(ApiPrincipal principal, string appId, StatsRequest request, CancellationToken cancellationToken)
        => Run(principal, appId, request.BuildMode, OAuthScopes.StatsRead, "event-props", tier =>
        {
            if (string.IsNullOrEmpty(request.EventName))
                return new Plan(ApiError.BadRequest("event_name is required."));

            var (window, error) = ResolveStats(request, tier);
            if (window is null) return new Plan(error!);

            return new Plan(StatsArgs(request, window), async (args, ct) =>
            {
                var rows = await Query<EventPropsItem>("top_props__v2", args, ct);
                return new
                {
                    Window = window.Describe(),
                    Data = rows.Select(r => new { r.StringKey, r.StringValue, r.NumericKey, r.Events, r.Median, r.Min, r.Max, r.Sum }).ToArray(),
                };
            });
        }, cancellationToken);

    // Live sessions, or a page of past sessions inside a bounded window, newest first.
    // The backing pipe is keyset-paged, so the window's end, or the cursor, is where a
    // page starts, and the window's start is where the listing stops.
    public Task<ApiResult> ListSessions(ApiPrincipal principal, string appId, SessionsRequest request, CancellationToken cancellationToken)
        => Run(principal, appId, request.BuildMode, OAuthScopes.SessionsRead, "sessions", tier =>
        {
            if (request.Live)
            {
                return new Plan(new Dictionary<string, string?> { ["live"] = "true" }, async (args, ct) =>
                {
                    var rows = await Query<LiveRecentSession>("live_sessions__v1", new Dictionary<string, string?> { ["app_id"] = args["app_id"] }, ct);
                    return new { Data = rows.Select(DescribeSession).ToArray(), NextCursor = (string?)null };
                });
            }

            if (string.IsNullOrEmpty(request.Period) && string.IsNullOrEmpty(request.From) && string.IsNullOrEmpty(request.To))
                return new Plan(ApiError.BadRequest("Sessions need a bounded window: pass period, or from and to, or live=true."));

            if (!string.IsNullOrEmpty(request.DeviceModel))
                return new Plan(ApiError.BadRequest("device_model is not available as a session filter."));

            var (window, error) = ResolveStats(request, tier, ignoreGranularity: true);
            if (window is null) return new Plan(error!);
            if (window.From is null || window.To is null)
                return new Plan(ApiError.BadRequest("Sessions need a bounded window, period=all is not available here."));

            var before = window.ToArg!;
            var beforeId = "";
            if (!string.IsNullOrEmpty(request.Cursor))
            {
                if (!TryParseCursor(request.Cursor, out before, out beforeId))
                    return new Plan(ApiError.BadRequest("cursor is not valid."));
            }

            var args = new Dictionary<string, string?>
            {
                ["date_to"] = before,
                ["session_id"] = beforeId,
                ["window_from"] = window.FromArg,
                ["country_code"] = request.CountryCode,
                ["os_name"] = request.OsName,
                ["event_name"] = request.EventName,
                ["app_version"] = request.AppVersion,
            };

            return new Plan(args, async (queryArgs, ct) =>
            {
                var pipeArgs = queryArgs.Where(a => a.Key != "window_from").ToDictionary(a => a.Key, a => a.Value);
                var rows = await Query<LiveRecentSession>("historical_sessions__v1", pipeArgs, ct);
                var inWindow = rows.Where(r => r.StartedAt >= window.From.Value).ToArray();
                var last = inWindow.LastOrDefault();
                var hasMore = rows.Count == SessionsPageSize && inWindow.Length == rows.Count && last is not null;

                return new
                {
                    Window = window.DescribeRange(),
                    Data = inWindow.Select(DescribeSession).ToArray(),
                    NextCursor = hasMore ? $"{last!.StartedAt.ToString(ApiWindow.DateFormat, CultureInfo.InvariantCulture)}|{last.Id}" : null,
                };
            });
        }, cancellationToken);

    public Task<ApiResult> GetSession(ApiPrincipal principal, string appId, string sessionId, string? buildMode, CancellationToken cancellationToken)
        => Run(principal, appId, buildMode, OAuthScopes.SessionsRead, "session", tier =>
        {
            if (string.IsNullOrEmpty(sessionId) || sessionId.Length > 100)
                return new Plan(ApiError.BadRequest("session_id is not valid."));

            return new Plan(new Dictionary<string, string?> { ["session_id"] = sessionId }, async (args, ct) =>
            {
                var rows = await Query<SessionTimeline>("live_session_details__v1", args, ct);
                var s = rows.FirstOrDefault();
                if (s is null || string.IsNullOrEmpty(s.Id))
                    return new { Data = (object?)null };

                // Never the user id: it is not selected by the pipe and must not be added here
                return new
                {
                    Data = (object?)new
                    {
                        s.Id,
                        StartedAt = DateTime.SpecifyKind(s.StartedAt, DateTimeKind.Utc),
                        DurationSeconds = s.Duration,
                        s.EventsCount,
                        s.AppVersion,
                        s.CountryCode,
                        s.RegionName,
                        s.OsName,
                        s.OsVersion,
                        Events = s.EventsName.Select((name, i) => new
                        {
                            Name = name,
                            Timestamp = i < s.EventsTimestamp.Length ? DateTime.SpecifyKind(s.EventsTimestamp[i], DateTimeKind.Utc) : (DateTime?)null,
                            StringProps = i < s.EventsStringProps.Length ? s.EventsStringProps[i] : null,
                            NumericProps = i < s.EventsNumericProps.Length ? s.EventsNumericProps[i] : null,
                        }).ToArray(),
                    },
                };
            });
        }, cancellationToken);

    // Event volume for this one app through monthly_usage__v1, which requires app_id, plus
    // the app's API usage and what is left of the owner's allowance. Never the billing
    // pipes, and never the owner's total event usage or plan.
    public async Task<ApiResult> GetUsage(ApiPrincipal principal, string appId, CancellationToken cancellationToken)
    {
        ReadableApp? app = null;
        var result = await Run(principal, appId, "release", OAuthScopes.StatsRead, "usage", _ =>
            new Plan(new Dictionary<string, string?>(), async (args, ct) =>
            {
                var release = await Query<MonthlyUsage>("monthly_usage__v1", new Dictionary<string, string?> { ["app_id"] = appId }, ct);
                var debug = await Query<MonthlyUsage>("monthly_usage__v1", new Dictionary<string, string?> { ["app_id"] = StoredAppId.Resolve(appId, "debug") }, ct);

                return new
                {
                    Events = new
                    {
                        Release = release.Take(12).Select(r => new { r.Year, r.Month, r.Events }).ToArray(),
                        Debug = debug.Take(12).Select(r => new { r.Year, r.Month, r.Events }).ToArray(),
                    },
                };
            }), cancellationToken, resolved => app = resolved);

        if (result.Error is not null || app is null)
            return result;

        // The API half is read fresh on every call, it is two cheap Postgres reads
        var tier = await _tiers.ResolveAsync(app.OwnerId, cancellationToken);
        var quota = await _metering.PeekAsync(app.OwnerId, tier.MonthlyRequests, cancellationToken);
        var appRows = await _metering.GetAppUsage(appId, app.OwnerId, cancellationToken);

        return ApiResult.Ok(new
        {
            Data = new
            {
                ((dynamic)result.Data!).Events,
                Api = new
                {
                    AppRequestsThisMonth = appRows.Sum(r => r.Requests),
                    Allowance = quota.Allowance,
                    Remaining = quota.Remaining,
                    ResetsAt = quota.ResetsAt,
                },
            },
        });
    }

    private record Plan
    {
        public ApiError? Error { get; }
        public Dictionary<string, string?> Args { get; } = new();
        public Func<IReadOnlyDictionary<string, string?>, CancellationToken, Task<object>>? Execute { get; }

        public Plan(ApiError error) => Error = error;

        public Plan(Dictionary<string, string?> args, Func<IReadOnlyDictionary<string, string?>, CancellationToken, Task<object>> execute)
        {
            Args = args;
            Execute = execute;
        }
    }

    private async Task<ApiResult> Run(
        ApiPrincipal principal,
        string appId,
        string? buildMode,
        string scope,
        string endpoint,
        Func<ApiTier, Plan> plan,
        CancellationToken cancellationToken,
        Action<ReadableApp>? onAuthorized = null)
    {
        if (!principal.HasScope(scope))
            return ApiError.InsufficientScope(scope);

        // 1. The app must be covered by the grant and readable by its subject right now
        var app = await AuthorizeApp(principal, appId, cancellationToken);
        if (app is null)
            return ApiError.Forbidden();

        onAuthorized?.Invoke(app);

        if (!ApiValidation.TryParseBuildMode(buildMode, out var parsedBuildMode))
            return ApiError.BadRequest("build_mode must be release or debug.");

        // 2. Validate and translate. Limits that vary by plan come from the app's owner
        var tier = await _tiers.ResolveAsync(app.OwnerId, cancellationToken);
        var planned = plan(tier);
        if (planned.Error is not null)
            return planned.Error;

        // The stored id is built from the authorized app id and a parsed build mode,
        // never from a caller string
        var storedAppId = StoredAppId.Resolve(app.Id, parsedBuildMode);
        planned.Args["app_id"] = storedAppId;

        // 3. Only now the cache. It is shared between grants on the same app, which is safe
        // because access and scope were checked above and the endpoint is part of the key.
        // A hit costs nothing at the analytics backend, so it is neither counted nor
        // checked against the allowance.
        var cacheKey = $"api:{storedAppId}:{endpoint}:{string.Join('&', planned.Args.Where(a => a.Value is not null).OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => $"{a.Key}={a.Value}"))}";
        if (_cache.TryGetValue<object>(cacheKey, out var cached) && cached is not null)
            return ApiResult.Ok(cached);

        // 4. Peak load is what moves our bill, so both caps come before the allowance:
        // a request turned away here has cost its owner nothing
        if (!_limits.TryEnterQuery(principal.GrantId))
            return ApiError.Busy($"This token already has {ApiLimits.ConcurrentQueriesPerGrant} queries in flight.");

        try
        {
            if (!await _globalQueries.WaitAsync(0, cancellationToken))
                return ApiError.Busy("The query API is at capacity, retry shortly.");

            try
            {
                // 5. Count against the owner's month, or stop here
                var quota = await _metering.CountAsync(principal.GrantId, app.Id, app.OwnerId, tier.MonthlyRequests, cancellationToken);
                if (!quota.Allowed)
                {
                    return new ApiError
                    {
                        Status = 429,
                        Code = "quota_exceeded",
                        Message = $"The owner of this app has used all {quota.Allowance} API requests for this month. The allowance resets on {quota.ResetsAt:yyyy-MM-dd} (UTC).",
                        RetryAfterSeconds = Math.Max(1, (int)(quota.ResetsAt - DateTime.UtcNow).TotalSeconds),
                        Quota = quota,
                    };
                }

                // 6. One request counts once, however many queries it fans out into
                var data = await planned.Execute!(planned.Args, cancellationToken);
                _cache.Set(cacheKey, data, CacheLifetime);
                return ApiResult.Ok(data);
            }
            finally
            {
                _globalQueries.Release();
            }
        }
        finally
        {
            _limits.ExitQuery(principal.GrantId);
        }
    }

    private async Task<ReadableApp?> AuthorizeApp(ApiPrincipal principal, string appId, CancellationToken cancellationToken)
    {
        ReadableApp? app = null;

        // The stored list is an upper bound, checked in memory first. It is never pruned
        // when a share goes away, which is why the live check below always follows.
        if (ApiValidation.IsValidId(appId) && principal.Grant.CoversApp(appId))
        {
            var cacheKey = ApiAccessCache.Key(_cache, principal.GrantId, appId);
            if (_cache.TryGetValue<ReadableApp>(cacheKey, out var cached) && cached is not null)
                return cached;

            app = await _access.GetReadableApp(principal.SubjectId, appId, cancellationToken);
            if (app is not null)
            {
                _cache.Set(cacheKey, app, AccessCacheLifetime);
                return app;
            }
        }

        await RecordDenied(principal, appId, cancellationToken);
        return null;
    }

    // Denials do not consume the allowance, so they get a budget of their own. A burst
    // slows the subject down here; the gate revokes a user grant that keeps going while
    // slowed. A service grant is never revoked by a machine: a CI job or a dashboard panel
    // stuck on a stale app id must not take a customer's integration down.
    private async Task RecordDenied(ApiPrincipal principal, string appId, CancellationToken cancellationToken)
    {
        var total = await _store.RecordDenied(principal.GrantId, cancellationToken);
        _logger.LogWarning("Grant {GrantId} was denied access to app {AppId} ({Total} denials for subject {SubjectId} in this window)",
            principal.GrantId, appId.Length <= 22 ? appId : "(invalid)", total, principal.SubjectId);

        if (total >= ApiLimits.DeniedSlowDownThreshold)
            _limits.SlowDown(principal.SubjectId);

    }

    private async Task<IReadOnlyList<T>> Query<T>(string pipe, IReadOnlyDictionary<string, string?> args, CancellationToken cancellationToken)
    {
        if (!AllowedPipes.Contains(pipe))
            throw new InvalidOperationException($"Pipe {pipe} is not available to the query API.");

        var response = await _queryClient.ApiQueryAsync<T>(pipe, args, cancellationToken);

        if (response.Statistics is not null)
        {
            _logger.LogInformation("Query API pipe {Pipe} for {AppId}: {Elapsed}s, {RowsRead} rows, {BytesRead} bytes",
                pipe, args.GetValueOrDefault("app_id"), response.Statistics.Elapsed, response.Statistics.RowsRead, response.Statistics.BytesRead);
        }

        return response.Rows;
    }

    private static (ApiWindow? Window, ApiError? Error) ResolveStats(StatsRequest request, ApiTier tier, bool ignoreGranularity = false)
    {
        var filterError = ApiValidation.ValidateFilters(request);
        if (filterError is not null)
            return (null, filterError);

        if (ignoreGranularity)
            request = new StatsRequest { Period = request.Period, From = request.From, To = request.To, Granularity = "month" };

        return ApiValidation.ResolveWindow(request, DateTime.UtcNow, tier.MaxWindowDays);
    }

    private static Dictionary<string, string?> StatsArgs(StatsRequest request, ApiWindow window) => new()
    {
        ["date_from"] = window.FromArg,
        ["date_to"] = window.ToArg,
        ["granularity"] = window.Granularity.ToString(),
        ["country_code"] = NullIfEmpty(request.CountryCode),
        ["os_name"] = NullIfEmpty(request.OsName),
        ["device_model"] = NullIfEmpty(request.DeviceModel),
        ["event_name"] = NullIfEmpty(request.EventName),
        ["app_version"] = NullIfEmpty(request.AppVersion),
    };

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    private static object DescribeSession(LiveRecentSession s) => new
    {
        s.Id,
        StartedAt = DateTime.SpecifyKind(s.StartedAt, DateTimeKind.Utc),
        DurationSeconds = s.Duration,
        s.EventsCount,
        s.AppVersion,
        s.CountryCode,
        s.RegionName,
        s.OsName,
        s.OsVersion,
    };

    private static bool TryParseCursor(string cursor, out string before, out string beforeId)
    {
        before = "";
        beforeId = "";

        var separator = cursor.IndexOf('|');
        if (separator <= 0 || cursor.Length > 200)
            return false;

        if (!DateTime.TryParseExact(cursor[..separator], ApiWindow.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return false;

        before = cursor[..separator];
        beforeId = cursor[(separator + 1)..];
        return beforeId.Length <= 100;
    }
}
