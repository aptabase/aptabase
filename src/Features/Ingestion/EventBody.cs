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

public class EventBody
{
    [Required, StringLength(60)]
    public string EventName { get; set; } = "";

    private DateTime _ts;
    public DateTime Timestamp
    {
        get => _ts;
        set => _ts = value > DateTime.UtcNow ? DateTime.UtcNow : value;
    }

    public JsonElement? SessionId { get; set; }

    public SystemProperties SystemProps { get; set; } = new();

    public JsonDocument? Props { get; set; }

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
                {
                    var propertyValue = property.Value.GetString() ?? "";
                    stringValues.Add(property.Name, propertyValue.Truncate(180, "..."));
                }
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