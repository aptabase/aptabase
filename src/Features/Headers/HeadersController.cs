using Microsoft.AspNetCore.Mvc;
using Aptabase.Features.GeoIP;

namespace Aptabase.Features.Headers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class HeadersController : Controller
{
    private readonly IGeoIPClient _geoIP;

    public HeadersController(IGeoIPClient geoIP)
    {
        _geoIP = geoIP ?? throw new ArgumentNullException(nameof(geoIP));
    }

    [HttpGet("/api/headers")]
    public IActionResult Headers()
    {
        var location = _geoIP.GetClientLocation(HttpContext);
        var ipAddress = HttpContext.ResolveClientIpAddress();
        var remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var headers = HttpContext.Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString());

        return Ok(new {
            remoteIpAddress,
            ipAddress,
            location,
            headers
        });
    }
}
