using Aptabase.Data;
using Aptabase.Features.Ingestion;
using Dapper;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace Aptabase.Features.FeatureFlags;

public class GetFeatureFlagBody
{
    public SystemProperties SystemProps { get; set; } = new();
    public JsonDocument? Props { get; set; }
}

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class FeatureFlagStateController : Controller
{
    private readonly ILogger _logger;
    private readonly IIngestionCache _cache;
    private readonly IDbContext _db;

    public FeatureFlagStateController(
        IDbContext db,
        IIngestionCache cache,
        ILogger<FeatureFlagStateController> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("/api/v0/feature-flags/{featureFlagKey}")]
    [EnableCors("AllowAny")]
    [EnableRateLimiting("FeatureFlags")]
    public async Task<IActionResult> Single(
        string featureFlagKey,
        [FromHeader(Name = "App-Key")] string? appKey,
        [FromHeader(Name = "User-Agent")] string? userAgent,
        [FromBody] GetFeatureFlagBody body)
    {
        appKey = appKey?.ToUpper() ?? "";

        var app = await _cache.FindByAppKey(appKey, HttpContext.RequestAborted);

        if (string.IsNullOrEmpty(app.Id))
        {
            return NotFound($"Appplication not found with given app key: {appKey}");
        }

        if (app.IsLocked)
        {
            return BadRequest($"Owner account is locked.");
        }

        var flag = await _db.Connection.QueryFirstOrDefaultAsync<FeatureFlag>(@"
            SELECT f.key, f.value
            FROM feature_flags f
            WHERE f.app_id = @appId and f.key = @key",
            new
            {
                appId = app.Id,
                key = featureFlagKey,
            });

        return Ok(flag);
    }

    [HttpGet("/api/v0/feature-flags")]
    [EnableCors("AllowAny")]
    [EnableRateLimiting("FeatureFlags")]
    public async Task<IActionResult> All(
    [FromHeader(Name = "App-Key")] string? appKey,
    [FromHeader(Name = "User-Agent")] string? userAgent,
    [FromBody] GetFeatureFlagBody body)
    {
        appKey = appKey?.ToUpper() ?? "";

        var app = await _cache.FindByAppKey(appKey, HttpContext.RequestAborted);

        if (string.IsNullOrEmpty(app.Id))
        {
            return NotFound($"Appplication not found with given app key: {appKey}");
        }

        if (app.IsLocked)
        {
            return BadRequest($"Owner account is locked.");
        }

        var flags = await _db.Connection.QueryAsync<FeatureFlag>(@"
            SELECT f.key, f.value
            FROM feature_flags f
            WHERE f.app_id = @appId
            LIMIT 50",
            new
            {
                appId = app.Id,
            });

        return Ok(flags);
    }
}
