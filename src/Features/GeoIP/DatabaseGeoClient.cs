using MaxMind.GeoIP2;

namespace Aptabase.Features.GeoIP;

public class DatabaseGeoClient : IGeoIPClient
{
    private readonly DatabaseReader _db;

    public DatabaseGeoClient(EnvSettings env)
    {
        _db = new DatabaseReader(Path.Combine(env.EtcDirectoryPath, "geoip/GeoLite2-City.mmdb"));
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