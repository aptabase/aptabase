using System.Text.Json;
using System.Text.Json.Serialization;

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

public readonly struct Coordinates
{
    [JsonPropertyName("lat")]
    public readonly double Latitude { get; init; }
    [JsonPropertyName("lng")]
    public readonly double Longitude { get; init; }
}

public abstract class GeoIPClient
{
    public abstract GeoLocation GetClientLocation(HttpContext httpContext);

    private readonly Dictionary<string, Dictionary<string, Coordinates>> _coordinates;

    public GeoIPClient(EnvSettings env)
    {
        var text = File.ReadAllText(Path.Combine(env.EtcDirectoryPath, "geoip/coordinates.json"));
        var coordinates = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Coordinates>>>(text);
        if (coordinates == null)
            throw new Exception("Failed to deserialize coordinates.json");

        _coordinates = coordinates;
    }

    public (double, double) GetLatLng(string countryCode, string regionName)
    {
        if (_coordinates.TryGetValue(countryCode, out var regions))
        {
            if (regions?.TryGetValue(regionName, out var coordinates) == true)
            {
                return (coordinates.Latitude, coordinates.Longitude);
            }
        }

        return (0, 0);
    }
}