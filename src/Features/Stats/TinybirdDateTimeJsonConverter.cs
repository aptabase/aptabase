using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aptabase.Features.Stats;

public class TinybirdDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var input = reader.GetString() ?? string.Empty;
        if (DateTime.TryParseExact(input, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime))
            return dateTime.ToUniversalTime();

        throw new NotSupportedException($"Could not parse {input} to DateTime. Value must be in yyyy-MM-dd HH:mm:ss format.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}