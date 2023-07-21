using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aptabase.Features.Ingestion;

public class SystemProperties
{
    public bool IsDebug { get; set; }

    [StringLength(30)]
    public string? OSName { get; set; }

    [StringLength(30)]
    public string? OSVersion { get; set; }

    [StringLength(10)]
    public string? Locale { get; set; }

    [StringLength(20)]
    public string? AppVersion { get; set; }

    [StringLength(20)]
    public string? AppBuildNumber { get; set; }

    [StringLength(30)]
    public string? EngineName { get; set; }

    [StringLength(30)]
    public string? EngineVersion { get; set; }

    [Required, StringLength(40)]
    public string SdkVersion { get; set; } = "";
}

public struct EventHeader
{
    public string AppId { get; private set; }
    public string? CountryCode { get; private set; }
    public string? RegionName { get; private set; }

    public EventHeader(string appId, string? countryCode = null, string? regionName = null)
    {
        AppId = appId;
        CountryCode = countryCode;
        RegionName = regionName;
    }
}

public static class EventsTTL
{
    // Events from Debug builds are kept for 6 months
    public static readonly TimeSpan Debug = TimeSpan.FromDays(182);
    // Events from Release builds are kept for 5 years
    public static readonly TimeSpan Release = TimeSpan.FromDays(5 * 365);
}

public class EventBody
{

    [Required, StringLength(60)]
    public string EventName { get; set; } = "";

    public DateTime Timestamp { get; set; }

    [Required, StringLength(36)]
    public string SessionId { get; set; } = "";

    public SystemProperties SystemProps { get; set; } = new();

    public JsonDocument? Props { get; set; }

    public TimeSpan TTL => SystemProps.IsDebug ? EventsTTL.Debug : EventsTTL.Release;

    public void Normalize()
    {
        // if the timestamp is in the future, normalize it to now
        if (Timestamp > DateTime.UtcNow)
            Timestamp = DateTime.UtcNow;
    }

    public (JsonObject, JsonObject) SplitProps()
    {
        var stringValues = new JsonObject();
        var numericValues = new JsonObject();

        if (Props != null)
        {
            // Sort by key to ensure consistent order might be useful in future!
            foreach (var property in Props.RootElement.EnumerateObject().OrderBy(x => x.Name))
            {
                if (property.Value.ValueKind == JsonValueKind.Number)
                    numericValues.Add(property.Name, property.Value.GetDecimal());
                else if (property.Value.ValueKind == JsonValueKind.String)
                    stringValues.Add(property.Name, property.Value.GetString());
                else if (property.Value.ValueKind == JsonValueKind.True)
                    stringValues.Add(property.Name, "true");
                else if (property.Value.ValueKind == JsonValueKind.False)
                    stringValues.Add(property.Name, "false");
                else if (property.Value.ValueKind == JsonValueKind.Null)
                    stringValues.Add(property.Name, "");
            }
        }

        return (stringValues, numericValues);
    }
}