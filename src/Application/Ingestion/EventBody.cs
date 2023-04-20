using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aptabase.Application.Ingestion;

public class SystemProperties
{
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

    public JsonObject? Props { get; set; }


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
            foreach (var (key, node) in Props.OrderBy(x => x.Key))
            {
                if (key is null || node is null)
                    continue;

                var element = node.GetValue<JsonElement>();
                if (element.ValueKind == JsonValueKind.Number)
                    numericValues.Add(key, JsonValue.Create(element));
                else if (element.ValueKind == JsonValueKind.String)
                    stringValues.Add(key, JsonValue.Create(element));
                else if (element.ValueKind == JsonValueKind.True)
                    stringValues.Add(key, JsonValue.Create("true"));
                else if (element.ValueKind == JsonValueKind.False)
                    stringValues.Add(key, JsonValue.Create("false"));
                else if (element.ValueKind == JsonValueKind.Null)
                    stringValues.Add(key, JsonValue.Create(""));
            }
        }

        return (stringValues, numericValues);
    }
}