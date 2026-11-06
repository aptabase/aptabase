using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Aptabase.Features.Billing.LemonSqueezy;

public class WebhookEvent
{
    public WebhookEventMeta Meta { get; set; } = new WebhookEventMeta();
    public WebhookEventData Data { get; set; } = new WebhookEventData();
}

public class WebhookEventData
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public JsonObject Attributes { get; set; } = new JsonObject();
}

public class WebhookEventMeta
{
    public string EventName { get; set; } = "";

    /// <summary>
    /// The custom data passed at checkout (user_id, region). LemonSqueezy only includes it when the
    /// checkout had any, and serializes an empty one as "[]" rather than "{}", so it is read leniently:
    /// anything that is not an object of scalars becomes an empty dictionary instead of a parse failure.
    /// </summary>
    [JsonConverter(typeof(CustomDataConverter))]
    public Dictionary<string, string> CustomData { get; set; } = new Dictionary<string, string>();
}

public class CustomDataConverter : JsonConverter<Dictionary<string, string>>
{
    // Without this a JSON null bypasses the converter and leaves the property null.
    public override bool HandleNull => true;

    public override Dictionary<string, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new Dictionary<string, string>();
        if (reader.TokenType == JsonTokenType.Null)
            return result;

        var node = JsonNode.Parse(ref reader);
        if (node is not JsonObject obj)
            return result;

        foreach (var (key, value) in obj)
        {
            if (value is JsonValue scalar)
                result[key] = scalar.ToString();
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, string> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
