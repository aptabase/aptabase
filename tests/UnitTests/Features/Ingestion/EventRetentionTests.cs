using System.Globalization;
using Aptabase.Features.Ingestion;
using Aptabase.Features.Ingestion.Buffer;
using AwesomeAssertions;
using Xunit;

namespace Aptabase.UnitTests.Features.Ingestion;

public class EventRetentionTests
{
    [Theory]
    [InlineData(6, "2025-02-28T12:00:00Z")]
    [InlineData(12, "2025-08-31T12:00:00Z")]
    [InlineData(24, "2026-08-31T12:00:00Z")]
    [InlineData(60, "2029-08-31T12:00:00Z")]
    public void Production_Expiry_Uses_Calendar_Months(int months, string expected)
    {
        var tracking = new TrackingEvent { Timestamp = Utc("2024-08-31T12:00:00Z"), RetentionMonths = months };

        new EventRow(ref tracking, "user").TTL.Should().Be(Utc(expected));
    }

    [Theory]
    [InlineData(6, "2025-02-28T12:00:00Z")]
    [InlineData(60, "2025-03-01T12:00:00Z")]
    public void Debug_Expiry_Uses_The_Earlier_Of_App_Policy_And_Debug_Cap(int months, string expected)
    {
        var tracking = new TrackingEvent
        {
            AppId = "app", Timestamp = Utc("2024-08-31T12:00:00Z"), RetentionMonths = months, IsDebug = true
        };

        var row = new EventRow(ref tracking, "user");
        row.TTL.Should().Be(Utc(expected));
        row.AppId.Should().Be("app_DEBUG");
    }

    [Fact]
    public void Leap_Day_Expiry_Clamps_To_The_Last_Day_Of_The_Month()
    {
        var tracking = new TrackingEvent { Timestamp = Utc("2024-02-29T00:00:00Z"), RetentionMonths = 12 };

        new EventRow(ref tracking, "user").TTL.Should().Be(Utc("2025-02-28T00:00:00Z"));
    }

    private static DateTime Utc(string value) => DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);
}
