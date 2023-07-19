using MaxMind.GeoIP2;

namespace Aptabase.Application.GeoIP;

public class DatabaseGeoClient : IGeoIPClient
{
    private DatabaseReader db = new DatabaseReader("./etc/geoip/GeoLite2-City.mmdb");

    public GeoLocation GetClientLocation(HttpContext httpContext)
    {
        var ip = httpContext.ResolveClientIpAddress();
        
        if (string.IsNullOrEmpty(ip))
            return GeoLocation.Empty;

        return db.TryCity(ip, out var city) ? new GeoLocation
        {
            CountryCode = city?.Country?.IsoCode?.ToUpper() ?? "",
            RegionName = city?.MostSpecificSubdivision.Name ?? ""
        } : GeoLocation.Empty;
    }
}