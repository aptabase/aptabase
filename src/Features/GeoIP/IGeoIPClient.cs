namespace Aptabase.Features.GeoIP;

public readonly struct GeoLocation
{
    public readonly string CountryCode { get; init; }
    public readonly string RegionName { get; init; }

    public static GeoLocation Empty => new()
    {
        CountryCode = "",
        RegionName = ""
    };
}

public interface IGeoIPClient
{
    GeoLocation GetClientLocation(HttpContext httpContext);
}