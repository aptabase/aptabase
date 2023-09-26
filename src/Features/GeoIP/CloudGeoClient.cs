using System.Text.Json;

namespace Aptabase.Features.GeoIP;

public class CloudGeoClient : IGeoIPClient
{
    private readonly Dictionary<string, string> _regions;

    public CloudGeoClient(EnvSettings env)
    {
        var text = File.ReadAllText(Path.Combine(env.EtcDirectoryPath, "geoip/iso3166-2.json"));
        var regions = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
        if (regions == null)
            throw new Exception("Failed to deserialize geoip/iso3166-2.json");

        _regions = regions;
    }

    public GeoLocation GetClientLocation(HttpContext httpContext)
    {
        var countryCode = GetHeader(httpContext, "cdn-requestcountrycode", "CloudFront-Viewer-Country");
        var regionCode = GetHeader(httpContext, "cdn-requeststatecode", "Cloudfront-Viewer-Country-Region");
        var regionName = _regions.TryGetValue($"{countryCode}-{regionCode}", out var name) ? name : "";

        return new GeoLocation
        {
            CountryCode = countryCode,
            RegionName = regionName
        };
    }

    private string GetHeader(HttpContext httpContext, params string[] names)
    {
        foreach (var name in names)
        {
            var value = httpContext.Request.Headers[name].ToString()?.ToUpper() ?? "";
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return "";
    }
}