using Aptabase.Features.GeoIP;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aptabase.Features.Ingestion;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class EventsController : Controller
{
    private readonly ILogger _logger;
    private readonly IIngestionValidator _validator;
    private readonly IIngestionClient _ingestionClient;
    private readonly IUserHashService _userHashService;
    private readonly IGeoIPClient _geoIP;

    public EventsController(IIngestionValidator validator,
                            IIngestionClient ingestionClient,
                            IUserHashService userHashService,
                            IGeoIPClient geoIP,
                            ILogger<EventsController> logger)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _ingestionClient = ingestionClient ?? throw new ArgumentNullException(nameof(ingestionClient));
        _userHashService = userHashService ?? throw new ArgumentNullException(nameof(userHashService));
        _geoIP = geoIP ?? throw new ArgumentNullException(nameof(geoIP));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("/api/v0/event")]
    [EnableCors("AllowAny")]
    [EnableRateLimiting("EventIngestion")]
    public async Task<IActionResult> Single(
        [FromHeader(Name = "App-Key")] string? appKey,
        [FromHeader(Name = "User-Agent")] string? userAgent,
        [FromBody] EventBody body,
        CancellationToken cancellationToken
    )
    {
        appKey = appKey?.ToUpper() ?? "";

        body.Normalize();

        var (valid, errorMessage) = _validator.IsValidBody(body);
        if (!valid)
        {
            _logger.LogWarning(errorMessage);
            return BadRequest(errorMessage);
        }

        var (appId, result) = await ValidateAppKey(appKey);
        if (result is not null)
            return result;

        // We never expect the Web SDK to send the OS name, so it's safe to assume that if it's missing the event is coming from a browser
        var isWeb = string.IsNullOrEmpty(body.SystemProps.OSName);

        // For web events, we need to parse the user agent to get the OS name and version
        if (isWeb && !string.IsNullOrEmpty(userAgent))
        {
            var (osName, osVersion) = UserAgentParser.ParseOperatingSystem(userAgent);
            body.SystemProps.OSName = osName;
            body.SystemProps.OSVersion = osVersion;

            var (engineName, engineVersion) = UserAgentParser.ParseBrowser(userAgent);
            body.SystemProps.EngineName = engineName;
            body.SystemProps.EngineVersion = engineVersion;
        }

        // We can't rely on User-Agent header sent by the SDK for non-web events, so we fabricate one
        // This can be removed when this issue is implemented: https://github.com/aptabase/aptabase/issues/23
        if (!isWeb)
            userAgent = $"{body.SystemProps.OSName}/{body.SystemProps.OSVersion} {body.SystemProps.EngineName}/{body.SystemProps.EngineVersion} {body.SystemProps.Locale}";

        var location = _geoIP.GetClientLocation(HttpContext);
        var header = new EventHeader(appId, location.CountryCode, location.RegionName);
        var userId = await _userHashService.CalculateHash(body.Timestamp, appId, body.SessionId, userAgent ?? "");
        var row = NewEventRow(userId, header, body);
        await _ingestionClient.SendSingleAsync(row, cancellationToken);

        return Ok(new { });
    }

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

    private EventRow NewEventRow(string userId, EventHeader header, EventBody body)
    {
        var appId = body.SystemProps.IsDebug ? $"{header.AppId}_DEBUG" : header.AppId;

        var locale = LocaleFormatter.FormatLocale(body.SystemProps.Locale);
        if (locale is null)
            _logger.LogWarning("Invalid locale {Locale} received from {OS} using {SdkVersion}", locale, body.SystemProps.OSName, body.SystemProps.SdkVersion);

        var (stringProps, numericProps) = body.SplitProps();
        return new EventRow
        {
            AppId = appId,
            EventName = body.EventName,
            Timestamp = body.Timestamp.ToUniversalTime().ToString("o"),
            UserId = userId,
            SessionId = body.SessionId,
            OSName = body.SystemProps.OSName ?? "",
            OSVersion = body.SystemProps.OSVersion ?? "",
            Locale = locale ?? "",
            AppVersion = body.SystemProps.AppVersion ?? "",
            EngineName = body.SystemProps.EngineName ?? "",
            EngineVersion = body.SystemProps.EngineVersion ?? "",
            AppBuildNumber = body.SystemProps.AppBuildNumber ?? "",
            SdkVersion = body.SystemProps.SdkVersion ?? "",
            CountryCode = header.CountryCode ?? "",
            RegionName = header.RegionName ?? "",
            City = "",
            StringProps = stringProps.ToJsonString(),
            NumericProps = numericProps.ToJsonString(),
            TTL = body.Timestamp.ToUniversalTime().Add(body.TTL).ToString("o"),
        };
    }

    // Disabled, not yet used. Revisit this in future and think about Rate Limiting, Max Payload Size, etc.
    // [HttpPost("/api/v0/events")]
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
}
