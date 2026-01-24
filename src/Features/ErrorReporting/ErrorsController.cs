using Aptabase.Data;
using Aptabase.Features.Authentication;
using Aptabase.Features.ErrorReporting.Buffer;
using Aptabase.Features.Ingestion;
using Aptabase.Features.Stats;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aptabase.Features.ErrorReporting;

[ApiController]
[ResponseCache(NoStore = true)]
public class ErrorsController : ControllerBase
{
    private readonly IErrorBuffer _buffer;
    private readonly IIngestionCache _cache;
    private readonly IPiiSanitizer _piiSanitizer;
    private readonly IErrorQueryClient _errorQueryClient;
    private readonly IDbContext _db;
    private readonly ILogger<ErrorsController> _logger;

    public ErrorsController(
        IErrorBuffer buffer,
        IIngestionCache cache,
        IPiiSanitizer piiSanitizer,
        IErrorQueryClient errorQueryClient,
        IDbContext db,
        ILogger<ErrorsController> logger)
    {
        _buffer = buffer;
        _cache = cache;
        _piiSanitizer = piiSanitizer;
        _errorQueryClient = errorQueryClient;
        _db = db;
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
            SessionId = body.SessionId
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
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? errorType,
        [FromQuery] string? platform,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        // Check if user has access to the app
        var user = HttpContext.GetCurrentUserIdentity();
        var hasAccess = await _db.HasReadAccessToApp(appId, user, cancellationToken);
        if (!hasAccess)
        {
            return StatusCode(403);
        }

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
            appId,
            start,
            end,
            errorType,
            platform,
            offset,
            limit,
            cancellationToken);

        // Get total count for pagination
        var totalCount = await _errorQueryClient.GetErrorCountAsync(
            appId,
            start,
            end,
            errorType,
            platform,
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

    [HttpOptions]
    [EnableCors("AllowAny")]
    [Route("/api/v0/error")]
    public IActionResult OptionsError()
    {
        return Ok();
    }
}
