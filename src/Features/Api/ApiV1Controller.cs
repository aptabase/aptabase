using System.Text.Json;
using System.Text.Json.Serialization;
using Aptabase.Features.OAuth;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aptabase.Features.Api;

public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static object ErrorBody(ApiError error)
    {
        if (error.Quota is null)
            return new { Error = error.Code, error.Message };

        return new
        {
            Error = error.Code,
            error.Message,
            error.Quota.Allowance,
            Remaining = 0,
            error.Quota.ResetsAt,
        };
    }
}

/// <summary>
/// The REST surface of the query API. Authentication happened in <see cref="ApiGate"/>,
/// which runs in front of every route here except the OpenAPI document. Everything else
/// is one call into the shared query layer.
/// </summary>
[ApiController]
[EnableCors(OAuthController.PublicCorsPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ApiExplorerSettings(GroupName = "v1")]
[Produces("application/json")]
public class ApiV1Controller : Controller
{
    private readonly IApiQueryService _queries;
    private readonly EnvSettings _env;

    public ApiV1Controller(IApiQueryService queries, EnvSettings env)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _env = env ?? throw new ArgumentNullException(nameof(env));
    }

    /// <summary>Apps this token can read.</summary>
    [HttpGet("/api/v1/apps")]
    [EndpointSummary("List apps")]
    [EndpointDescription("Apps the token can read right now: covered by the grant, owned by or shared with its subject. Requires stats:read.")]
    public async Task<IActionResult> ListApps(CancellationToken cancellationToken)
        => Respond(await _queries.ListApps(HttpContext.GetApiPrincipal(), cancellationToken));

    [HttpGet("/api/v1/apps/{appId}/metrics")]
    [EndpointSummary("Totals for a window")]
    [EndpointDescription("Daily users, sessions, events and median session duration over a window. Pass period (default 24h) or from and to. Requires stats:read.")]
    public async Task<IActionResult> Metrics(string appId, [FromQuery] StatsRequest request, CancellationToken cancellationToken)
        => Respond(await _queries.GetMetrics(HttpContext.GetApiPrincipal(), appId, request, cancellationToken));

    [HttpGet("/api/v1/apps/{appId}/timeseries")]
    [EndpointSummary("Metrics per time bucket")]
    [EndpointDescription("Users, sessions and events per hour, day or month. Hourly buckets are available for windows of up to 7 days. Requires stats:read.")]
    public async Task<IActionResult> Timeseries(string appId, [FromQuery] StatsRequest request, CancellationToken cancellationToken)
        => Respond(await _queries.GetTimeseries(HttpContext.GetApiPrincipal(), appId, request, cancellationToken));

    [HttpGet("/api/v1/apps/{appId}/breakdown")]
    [EndpointSummary("Top values of a dimension")]
    [EndpointDescription("Top values of one dimension (country, region, os, os_version, device, app_version, app_build_number, event) by sessions or events. Requires stats:read.")]
    public async Task<IActionResult> Breakdown(string appId, [FromQuery] BreakdownRequest request, CancellationToken cancellationToken)
        => Respond(await _queries.GetBreakdown(HttpContext.GetApiPrincipal(), appId, request, cancellationToken));

    [HttpGet("/api/v1/apps/{appId}/event-props")]
    [EndpointSummary("Property breakdown of one event")]
    [EndpointDescription("String and numeric properties of the event named by event_name, with counts and numeric aggregates. Requires stats:read.")]
    public async Task<IActionResult> EventProps(string appId, [FromQuery] StatsRequest request, CancellationToken cancellationToken)
        => Respond(await _queries.GetEventProps(HttpContext.GetApiPrincipal(), appId, request, cancellationToken));

    [HttpGet("/api/v1/apps/{appId}/sessions")]
    [EndpointSummary("List sessions")]
    [EndpointDescription("Sessions inside a bounded window, newest first, ten per page, or the sessions of the last hour with live=true. Requires sessions:read.")]
    public async Task<IActionResult> Sessions(string appId, [FromQuery] SessionsRequest request, CancellationToken cancellationToken)
        => Respond(await _queries.ListSessions(HttpContext.GetApiPrincipal(), appId, request, cancellationToken));

    [HttpGet("/api/v1/apps/{appId}/sessions/{sessionId}")]
    [EndpointSummary("Session timeline")]
    [EndpointDescription("The events of one session with their properties. Properties may contain personal data your app sent. Requires sessions:read.")]
    public async Task<IActionResult> Session(string appId, string sessionId, [FromQuery(Name = "build_mode")] string? buildMode, CancellationToken cancellationToken)
        => Respond(await _queries.GetSession(HttpContext.GetApiPrincipal(), appId, sessionId, buildMode, cancellationToken));

    [HttpGet("/api/v1/apps/{appId}/usage")]
    [EndpointSummary("Event and API usage")]
    [EndpointDescription("Monthly event volume of the app, its API requests this month, and what is left of the owner's monthly API allowance. Requires stats:read.")]
    public async Task<IActionResult> Usage(string appId, CancellationToken cancellationToken)
        => Respond(await _queries.GetUsage(HttpContext.GetApiPrincipal(), appId, cancellationToken));

    private IActionResult Respond(ApiResult result)
    {
        if (result.Error is null)
            return new JsonResult(result.Data, ApiJson.Options);

        var error = result.Error;
        if (error.RetryAfterSeconds.HasValue)
            Response.Headers.RetryAfter = error.RetryAfterSeconds.Value.ToString();

        // Tells an OAuth client which scope to ask for in a step-up authorization
        if (error.RequiredScope is not null)
            Response.Headers.WWWAuthenticate = ApiGate.ChallengeHeader(_env, "insufficient_scope", error.RequiredScope);

        return new JsonResult(ApiJson.ErrorBody(error), ApiJson.Options) { StatusCode = error.Status };
    }
}
