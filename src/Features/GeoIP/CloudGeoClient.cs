namespace Aptabase.Features.GeoIP;

public class CloudGeoClient : IGeoIPClient
{
    public GeoLocation GetClientLocation(HttpContext httpContext)
    {
        var countryCode = httpContext.Request.Headers["CloudFront-Viewer-Country"].ToString();
        var regionName = httpContext.Request.Headers["CloudFront-Viewer-Country-Region-Name"].ToString();

        return new GeoLocation
        {
            CountryCode = countryCode?.ToUpper() ?? "",
            RegionName = Uri.UnescapeDataString(regionName ?? "")
        };
    }
}