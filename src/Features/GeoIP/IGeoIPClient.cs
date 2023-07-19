namespace Aptabase.Features.GeoIP;

public class GeoLocation
{
    public string CountryCode { get; set; } = "";
    public string RegionName { get; set; } =  "";

    public static GeoLocation Empty => new GeoLocation();
}

public interface IGeoIPClient
{
    GeoLocation GetClientLocation(HttpContext httpContext);
}