using FluentAssertions;
using Xunit;

namespace Aptabase.Application.Ingestion;

public class EventBodyTests
{

    [Fact]
    public void EventBody_PastDatesShouldNotBeChanged()
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
    public void EventBody_FutureDatesShouldBeNormalizedToNow()
    {
        var body = new EventBody
        {
            Timestamp = DateTime.UtcNow.AddDays(5),
        };

        body.Normalize();
        body.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}