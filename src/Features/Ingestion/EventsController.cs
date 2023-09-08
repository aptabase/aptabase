using System.Text.Json;
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
    private readonly IIngestionCache _cache;
    private readonly IIngestionClient _ingestionClient;
    private readonly IUserHashService _userHashService;
    private readonly IGeoIPClient _geoIP;

    public EventsController(IIngestionCache cache,
                            IIngestionClient ingestionClient,
                            IUserHashService userHashService,
                            IGeoIPClient geoIP,
                            ILogger<EventsController> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
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

        var (valid, validationMessage) = IsValidBody(body);
        if (!valid)
        {
            _logger.LogWarning($"Dropping event from {appKey} because: {validationMessage}.");
            return BadRequest(validationMessage);
        }

        var app = await _cache.FindByAppKey(appKey, cancellationToken);
        if (string.IsNullOrEmpty(app.Id))
            return AppNotFound(appKey);

        if (app.IsLocked) 
            return BadRequest($"Owner account is locked.");

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
        var header = new EventHeader(app.Id, location.CountryCode, location.RegionName);
        var userId = await _userHashService.CalculateHash(body.Timestamp, app.Id, body.SessionId, userAgent ?? "");
        var row = NewEventRow(userId, header, body);
        await _ingestionClient.SendSingleAsync(row, cancellationToken);

        return Ok(new { });
    }

    [HttpPost("/api/v0/events")]
    [EnableRateLimiting("EventIngestion")]
    public async Task<IActionResult> Multiple(
        [FromHeader(Name = "App-Key")] string? appKey,
        [FromHeader(Name = "User-Agent")] string? userAgent,
        [FromBody] EventBody[] events,
        CancellationToken cancellationToken
    )
    {
        appKey = appKey?.ToUpper() ?? "";

        if (events.Length > 25)
            return BadRequest($"Too many events ({events.Length}) in a single request. Maximum is 25.");

        var validEvents = events.Where(e => { 
            var (valid, validationMessage) = IsValidBody(e);
            if (!valid)
                _logger.LogWarning($"Dropping event from {appKey}. {validationMessage}");
            return valid;
        }).ToArray();

        if (!validEvents.Any())
            return Ok(new { });

        var app = await _cache.FindByAppKey(appKey, cancellationToken);
        if (string.IsNullOrEmpty(app.Id))
            return AppNotFound(appKey);

        if (app.IsLocked) 
            return BadRequest($"Owner account is locked.");

        var location = _geoIP.GetClientLocation(HttpContext);
        var header = new EventHeader(app.Id, location.CountryCode, location.RegionName);

        var rows = await Task.WhenAll(validEvents.Select(async e => {
            var userId = await _userHashService.CalculateHash(e.Timestamp, app.Id, e.SessionId, userAgent ?? "");
            return NewEventRow(userId, header, e);
        }));

        await _ingestionClient.SendMultipleAsync(rows, cancellationToken);

        return Ok(new { });
    }

    private IActionResult AppNotFound(string appKey)
    {
        var msg = $"Appplication not found with given app key: {appKey}";
        _logger.LogWarning(msg);
        return NotFound(msg);
    }

    private (bool, string) IsValidBody(EventBody? body)
    {
        if (body is null)
            return (false, "Missing event body.");

        if (body.Timestamp > DateTime.UtcNow.AddMinutes(1))
            return (false, "Future events are not allowed.");

        if (body.Timestamp < DateTime.UtcNow.AddDays(-1))
            return (false, "Event is too old.");

        if (body.Props is not null)
        {
            if (body.Props.RootElement.ValueKind == JsonValueKind.String)
            {
                var valueAsString = body.Props.RootElement.GetString() ?? "";
                if (TryParseDocument(valueAsString, out var doc) && doc is not null)
                    body.Props = doc;
                else 
                    return (false, $"Props must be an object or a valid stringified JSON, was: {body.Props.RootElement.GetRawText()}");
            }

            if (body.Props.RootElement.ValueKind != JsonValueKind.Object)
                return (false, $"Props must be an object or a valid stringified JSON, was: {body.Props.RootElement.GetRawText()}");

            foreach (var prop in body.Props.RootElement.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(prop.Name))
                    return (false, "Property key must not be empty.");

                if (prop.Name.Length > 40)
                    return (false, $"Property key '{prop.Name}' must be less than or equal to 40 characters. Props was: {body.Props.RootElement.GetRawText()}");

                if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                    return (false, $"Value of key '{prop.Name}' must be a primitive type. Props was: {body.Props.RootElement.GetRawText()}");

                if (prop.Value.ToString().Length > 200)
                    return (false, $"Value of key '{prop.Name}' must be less than or equal to 200 characters. Props was: {body.Props.RootElement.GetRawText()}");
            }
        }

        return (true, string.Empty);
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

    private bool TryParseDocument(string json, out JsonDocument? doc)
    {
        try
        {
            doc = JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            doc = null;
            return false;
        }
    }
}
