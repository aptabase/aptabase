using Xunit;
using System.Text.Json;
using Aptabase.Features.Ingestion;
using FluentAssertions;

namespace Aptabase.UnitTests.Features.Ingestion;

public class EventBodyTests
{
    [Fact]
    public void Past_dates_should_bot_be_changed()
    {
        var ts = DateTime.UtcNow.AddMinutes(-10);
        var body = new EventBody { Timestamp = ts };

        body.Timestamp.Should().Be(ts);
    }

    [Fact]
    public void Future_dates_should_be_normalized_to_now()
    {
        var body = new EventBody { Timestamp = DateTime.UtcNow.AddDays(5) };

        body.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SplitProps_should_ignore_nonprimitive_types()
    {
        var body = new EventBody
        {
            Timestamp = DateTime.UtcNow.AddDays(5),
            Props = JsonDocument.Parse(@"{
                ""name"": ""Bob"",
                ""age"": 10,
                ""isAdult"": true,
                ""array"": [1,2,3],
                ""object"": { ""foo"": ""bar"" }
            }")
        };

        var (stringProps, numericProps) = body.SplitProps();
        stringProps.Count.Should().Be(4);
        stringProps["name"]!.GetValue<string>().Should().Be("Bob");
        stringProps["isAdult"]!.GetValue<string>().Should().Be("true");
        stringProps["array"]!.GetValue<string>().Should().Be("[Array]");
        stringProps["object"]!.GetValue<string>().Should().Be("{Object}");

        numericProps.Count.Should().Be(1);
        numericProps["age"]!.GetValue<decimal>().Should().Be(10);
    }

    [Fact]
    public void SplitProps_should_truncate_long_values()
    {
        var body = new EventBody
        {
            Timestamp = DateTime.UtcNow.AddDays(5),
            Props = JsonDocument.Parse(@"{
                ""type"": ""ERROR"",
                ""value"": ""12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890""
            }")
        };

        var (stringProps, numericProps) = body.SplitProps();
        stringProps.Count.Should().Be(2);
        stringProps["type"]!.GetValue<string>().Should().Be("ERROR");
        stringProps["value"]!.GetValue<string>().Should().Be("123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567...");
    }
}