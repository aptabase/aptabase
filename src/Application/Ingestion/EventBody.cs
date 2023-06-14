using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aptabase.Application.Ingestion;

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
    public string? City { get; private set; }

    public EventHeader(string appId, string? countryCode = null, string? regionName = null, string? city = null)
    {
        AppId = appId;
        CountryCode = countryCode;
        RegionName = regionName;
        City = city;
    }
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

    public void Normalize()
    {
        // if the timestamp is in the future, normalize it to now
        if (Timestamp > DateTime.UtcNow)
            Timestamp = DateTime.UtcNow;
    }

    public void EnrichWith(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return;

        // if the request doesn't have OS Name/Version, we can try to parse it from the user agent
        if (string.IsNullOrEmpty(this.SystemProps.OSName))
        {
            var (osName, osVersion) = UserAgentParser.ParseOperatingSystem(userAgent);
            this.SystemProps.OSName = osName;
            this.SystemProps.OSVersion = osVersion;

            var (engineName, engineVersion) = UserAgentParser.ParseBrowser(userAgent);
            this.SystemProps.EngineName = engineName;
            this.SystemProps.EngineVersion = engineVersion;
        }
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