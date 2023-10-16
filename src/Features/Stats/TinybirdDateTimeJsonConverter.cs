using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aptabase.Features.Stats;

public class TinybirdDateTimeJsonConverter : JsonConverter<DateTime>
{
    private readonly static string[] FORMATS = new[]
    {
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd"
    };

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var input = reader.GetString() ?? string.Empty;
        foreach (var format in FORMATS) 
        {
            if (DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime))
                return dateTime.ToUniversalTime();
        }

        throw new NotSupportedException($"Could not parse {input} to DateTime.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}