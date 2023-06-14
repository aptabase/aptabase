using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Aptabase.Application.Ingestion;

public class EventBodyTests
{

    [Fact]
    public void Past_dates_should_bot_be_changed()
    {
        var ts = DateTime.UtcNow.AddMinutes(-10);
        var body = new EventBody
        {
            Timestamp = ts,
        };

        body.Normalize();
        body.Timestamp.Should().Be(ts);
    }

    [Fact]
    public void Future_dates_should_be_normalized_to_now()
    {
        var body = new EventBody
        {
            Timestamp = DateTime.UtcNow.AddDays(5),
        };

        body.Normalize();
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
        stringProps.Count.Should().Be(2);
        stringProps["name"]!.GetValue<String>().Should().Be("Bob");
        stringProps["isAdult"]!.GetValue<String>().Should().Be("true");

        numericProps.Count.Should().Be(1);
        numericProps["age"]!.GetValue<decimal>().Should().Be(10);
    }
}