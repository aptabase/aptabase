namespace Aptabase.Features.GeoIP;

public struct GeoLocation
{
    public string CountryCode { get; init; }
    public string RegionName { get; init; }

    public static GeoLocation Empty => new();
}

public interface IGeoIPClient
{
    GeoLocation GetClientLocation(HttpContext httpContext);
}