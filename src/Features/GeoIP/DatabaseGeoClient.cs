using MaxMind.GeoIP2;

namespace Aptabase.Features.GeoIP;

public class DatabaseGeoClient : IGeoIPClient
{
    private readonly DatabaseReader _db;
    private readonly string[] paths = new string[] {
        "./etc/geoip/GeoLite2-City.mmdb",
        "../etc/geoip/GeoLite2-City.mmdb"
    };

    public DatabaseGeoClient()
    {
        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                _db = new DatabaseReader(path);
                return;
            }
        }

        throw new FileNotFoundException("Could not find GeoIP database");
    }

    public GeoLocation GetClientLocation(HttpContext httpContext)
    {
        var ip = httpContext.ResolveClientIpAddress();
        
        if (string.IsNullOrEmpty(ip))
            return GeoLocation.Empty;

        return _db.TryCity(ip, out var city) ? new GeoLocation
        {
            CountryCode = city?.Country?.IsoCode?.ToUpper() ?? "",
            RegionName = city?.MostSpecificSubdivision.Name ?? ""
        } : GeoLocation.Empty;
    }
}