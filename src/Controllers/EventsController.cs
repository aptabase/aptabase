using Aptabase.Application;
using Aptabase.Application.Ingestion;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aptabase.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class EventsController : Controller
{
    private readonly ILogger _logger;
    private readonly IIngestionValidator _validator;
    private readonly ITinybirdClient _tinybirdClient;

    public EventsController(IIngestionValidator validator, ITinybirdClient tinybirdClient, ILogger<EventsController> logger)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _tinybirdClient = tinybirdClient ?? throw new ArgumentNullException(nameof(tinybirdClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("/v0/event")]
    [HttpPost("/api/v0/event")]
    [EnableCors("AllowAny")]
    [EnableRateLimiting("EventIngestion")]
    public async Task<IActionResult> Single(
        [FromHeader(Name = "App-Key")] string? appKey,
        [FromHeader(Name = "CloudFront-Viewer-Country")] string? countryCode,
        [FromHeader(Name = "CloudFront-Viewer-Country-Region-Name")] string? regionName,
        [FromHeader(Name = "CloudFront-Viewer-City")] string? city,
        [FromHeader(Name = "User-Agent")] string? userAgent,
        [FromBody] EventBody body,
        CancellationToken cancellationToken
    )
    {
        appKey = appKey?.ToUpper() ?? "";
        countryCode = countryCode?.ToUpper() ?? "";
        regionName = Uri.UnescapeDataString(regionName ?? "");

        var (valid, errorMessage) = _validator.IsValidBody(body);
        if (!valid)
            return BadRequest(errorMessage);

        var (appId, result) = await ValidateAppKey(appKey);
        if (result is not null)
            return result;

        body.EnrichWith(userAgent);

        var header = new EventHeader(appId, countryCode, regionName, city);
        await _tinybirdClient.SendSingleAsync(header, body, cancellationToken);

        // TODO: return how many rows were inserted, how many invalid
        return Ok(new { });
    }

    // Disabled, not yet used. Revisit this in future and think about Rate Limiting
    // [HttpPost("/v0/events")]
    // [EnableCors("AllowAny")]
    // public async Task<IActionResult> Multiple(
    //     [FromHeader(Name = "App-Key")] string? appKey,
    //     [FromHeader(Name = "CloudFront-Viewer-Country")] string? countryCode,
    //     [FromHeader(Name = "CloudFront-Viewer-Country-Region-Name")] string? regionName,
    //     [FromHeader(Name = "CloudFront-Viewer-City")] string? city,
    //     [FromHeader(Name = "User-Agent")] string? userAgent,
    //     [FromBody] EventBody[] body,
    //     CancellationToken cancellationToken
    // )
    // {
    //     appKey = appKey?.ToUpper() ?? "";
    //     countryCode = countryCode?.ToUpper() ?? "";

    //     var (appId, result) = await ValidateAppKey(appKey);
    //     if (result is not null)
    //         return result;

    //     var events = body
    //         .Where(e => _validator.IsValidBody(e).Item1)
    //         .Select(e =>
    //         {
    //             e.EnrichWith(userAgent);
    //             return e;
    //         })
    //         .ToArray();

    //     var header = new EventHeader(appId, countryCode, regionName, city);
    //     await _tinybirdClient.SendMultipleAsync(header, events, cancellationToken);

    //     // TODO: return how many rows were inserted, how many invalid
    //     return Ok(new { });
    // }

    private async Task<(string, IActionResult?)> ValidateAppKey(string appKey)
    {
        var (appId, status) = await _validator.IsAppKeyValid(appKey);
        IActionResult? result = status switch
        {
            AppKeyStatus.Missing => BadRequest("Missing App-Key header. Find your app key on Aptabase console."),
            AppKeyStatus.InvalidFormat => BadRequest($"Invalid format for app key '{appKey}'. Find your app key on Aptabase console."),
            AppKeyStatus.InvalidRegion => BadRequest("Invalid App Key region. This key is meant for another region. Find your app key on Aptabase console."),
            AppKeyStatus.NotFound => NotFound($"Appplication not found with given app key '{appKey}'. Find your app key on Aptabase console."),
            _ => null
        };

        return (appId, result);
    }
}
