using Aptabase.Data;
using Aptabase.Features.Authentication;
using Aptabase.Features.ErrorReporting.Buffer;
using Aptabase.Features.Ingestion;
using Aptabase.Features.Stats;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace Aptabase.Features.ErrorReporting;

[ApiController]
[ResponseCache(NoStore = true)]
public class ErrorsController : ControllerBase
{
    // How long an app stays in the "quota exhausted" fast path before we re-check Postgres.
    // Trade-off: after a mid-month quota raise or the monthly reset, error ingestion for an
    // exhausted app can keep getting rejected for up to this duration.
    private static readonly TimeSpan QuotaExhaustedCacheDuration = TimeSpan.FromMinutes(5);

    private readonly IErrorBuffer _buffer;
    private readonly IIngestionCache _cache;
    private readonly IPiiSanitizer _piiSanitizer;
    private readonly IErrorQueryClient _errorQueryClient;
    private readonly IDbContext _db;
    private readonly EnvSettings _env;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ErrorsController> _logger;

    public ErrorsController(
        IErrorBuffer buffer,
        IIngestionCache cache,
        IPiiSanitizer piiSanitizer,
        IErrorQueryClient errorQueryClient,
        IDbContext db,
        EnvSettings env,
        IMemoryCache memoryCache,
        ILogger<ErrorsController> logger)
    {
        _buffer = buffer;
        _cache = cache;
        _piiSanitizer = piiSanitizer;
        _errorQueryClient = errorQueryClient;
        _db = db;
        _env = env;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    [HttpPost]
    [EnableCors("AllowAny")]
    [EnableRateLimiting("EventIngestion")]
    [Route("/api/v0/error")]
    public async Task<IActionResult> PostError([FromBody] ErrorBody body, CancellationToken cancellationToken)
    {
        // Validate the error body
        if (!body.IsValid())
        {
            return BadRequest(new { error = "Invalid error payload" });
        }

        // Normalize timestamp (convert to UTC, clamp to now if in future)
        body.NormalizeTimestamp();

        // Normalize severity/kind against the known values (unknown values become empty)
        body.NormalizeSeverityAndKind();

        // Extract App-Key header
        if (!Request.Headers.TryGetValue("App-Key", out var appKey) || string.IsNullOrWhiteSpace(appKey))
        {
            return BadRequest(new { error = "Missing App-Key header" });
        }

        var normalizedAppKey = appKey.ToString().ToUpperInvariant();

        // Look up app by key
        var app = await _cache.FindByAppKey(normalizedAppKey, cancellationToken);
        if (string.IsNullOrEmpty(app.Id))
        {
            _logger.LogWarning("Error rejected: Invalid App-Key {AppKey}", normalizedAppKey);
            return Unauthorized(new { error = "Invalid App-Key" });
        }

        if (app.IsLocked)
        {
            _logger.LogWarning("Error rejected: App {AppId} is locked", app.Id);
            return BadRequest(new { error = "Owner account is locked" });
        }

        // Enforce the per-app error quota (atomically increments the counter).
        // Disabled by default; enable via ERROR_QUOTA_ENABLED once quota tiers are finalized.
        //
        // Exhaustion returns 403, NOT 429: deployed SDKs (e.g. aptabase-maui) treat 429 as
        // retryable and would re-send the same queued error every 30 seconds until the monthly
        // reset, while any other 4xx is logged and dropped client-side. 403 makes SDKs drop the
        // report (matching how billing overuse locks are handled), and 429 stays reserved for
        // the per-IP rate limiter, where retrying IS appropriate.
        if (_env.ErrorQuotaEnabled)
        {
            // Short-TTL "quota exhausted" cache: once the quota is known to be gone, a
            // crash-storming fleet shouldn't cost one Postgres UPDATE attempt per rejected
            // request. See QuotaExhaustedCacheDuration for the staleness trade-off.
            var quotaCacheKey = $"ERROR-QUOTA-EXHAUSTED-{app.Id}";
            if (_memoryCache.TryGetValue(quotaCacheKey, out _))
            {
                // Debug (not Warning) to avoid log spam: the first detection below already
                // logged a Warning for this app within the last few minutes.
                _logger.LogDebug("Error rejected: App {AppId} exceeded its error quota (cached)", app.Id);
                return StatusCode(403, new { error = "Error quota exceeded" });
            }

            var withinQuota = await _db.TryConsumeErrorQuota(app.Id, cancellationToken);
            if (!withinQuota)
            {
                _memoryCache.Set(quotaCacheKey, true, QuotaExhaustedCacheDuration);
                _logger.LogWarning("Error rejected: App {AppId} exceeded its error quota", app.Id);
                return StatusCode(403, new { error = "Error quota exceeded" });
            }
        }

        // Generate error ID
        var errorId = Guid.NewGuid().ToString();

        // Sanitize PII from error message and stack trace
        var sanitizedErrorMessage = _piiSanitizer.Sanitize(body.ErrorMessage);
        var sanitizedStackTrace = _piiSanitizer.Sanitize(body.StackTrace);

        // Create tracking error
        var trackingError = new TrackingError
        {
            ErrorId = errorId,
            AppId = app.Id,
            Timestamp = body.Timestamp!.Value,
            ErrorMessage = sanitizedErrorMessage,
            ErrorType = body.ErrorType!,
            StackTrace = sanitizedStackTrace,
            Platform = body.Platform,
            OsName = body.OsName,
            OsVersion = body.OsVersion,
            AppVersion = body.AppVersion,
            SdkVersion = body.SdkVersion,
            SessionId = body.SessionId,
            Severity = body.Severity,
            Kind = body.Kind,
            IsDebug = body.IsDebug
        };

        // Add to buffer
        _buffer.Add(ref trackingError);

        // Return 202 Accepted
        return Accepted();
    }

    [HttpGet]
    [IsAuthenticated]
    [EnableRateLimiting("Stats")]
    [Route("/api/v0/apps/{appId}/errors")]
    public async Task<IActionResult> GetErrors(
        string appId,
        [FromQuery] string? buildMode,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? errorType,
        [FromQuery] string? osName,
        [FromQuery] string? severity,
        [FromQuery] string? kind,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        // Check if user has access to the app (always against the raw app id)
        var user = HttpContext.GetCurrentUserIdentity();
        var hasAccess = await _db.HasReadAccessToApp(appId, user, cancellationToken);
        if (!hasAccess)
        {
            return StatusCode(403);
        }

        // Debug builds are stored under a suffixed app id, same as analytics events
        var queryAppId = ResolveAppId(appId, buildMode);

        // Set default date range if not provided (last 7 days)
        var end = endDate ?? DateTime.UtcNow;
        var start = startDate ?? end.AddDays(-7);

        // Validate limit
        if (limit < 1 || limit > 100)
        {
            limit = 50;
        }

        // Validate offset
        if (offset < 0)
        {
            offset = 0;
        }

        // Query errors
        var errors = await _errorQueryClient.GetErrorsAsync(
            queryAppId,
            start,
            end,
            errorType,
            osName,
            severity,
            kind,
            offset,
            limit,
            cancellationToken);

        // Get total count for pagination
        var totalCount = await _errorQueryClient.GetErrorCountAsync(
            queryAppId,
            start,
            end,
            errorType,
            osName,
            severity,
            kind,
            cancellationToken);

        return Ok(new
        {
            errors = errors,
            pagination = new
            {
                offset = offset,
                limit = limit,
                total = totalCount
            }
        });
    }

    [HttpGet]
    [IsAuthenticated]
    [EnableRateLimiting("Stats")]
    [Route("/api/v0/apps/{appId}/errors/{errorId}")]
    public async Task<IActionResult> GetErrorById(
        string appId,
        string errorId,
        [FromQuery] string? buildMode = null,
        CancellationToken cancellationToken = default)
    {
        // Check if user has access to the app (always against the raw app id)
        var user = HttpContext.GetCurrentUserIdentity();
        var hasAccess = await _db.HasReadAccessToApp(appId, user, cancellationToken);
        if (!hasAccess)
        {
            return StatusCode(403);
        }

        // Debug builds are stored under a suffixed app id, same as analytics events
        var queryAppId = ResolveAppId(appId, buildMode);

        // Query error by ID
        var error = await _errorQueryClient.GetErrorByIdAsync(queryAppId, errorId, cancellationToken);

        // Return 404 if error not found or doesn't belong to app
        if (error == null)
        {
            return NotFound(new { error = "Error not found" });
        }

        return Ok(error);
    }

    [HttpOptions]
    [EnableCors("AllowAny")]
    [Route("/api/v0/error")]
    public IActionResult OptionsError()
    {
        return Ok();
    }

    // Maps the buildMode query parameter to the stored app id, mirroring Stats.QueryParams.Parse()
    private static string ResolveAppId(string appId, string? buildMode)
    {
        return buildMode?.ToLower() switch
        {
            "debug" => $"{appId}_DEBUG",
            _ => appId,
        };
    }
}
