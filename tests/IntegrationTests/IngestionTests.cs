using Xunit;
using System.Net;
using FluentAssertions;
using Aptabase.IntegrationTests.Clients;
using Aptabase.Features.Ingestion.Buffer;

namespace Aptabase.IntegrationTests;

[Collection("Integration Tests")]
public class IngestionTests
{
    private readonly IntegrationTestsFixture _fixture;
    private readonly EventBackgroundWritter _eventWritter;

    public IngestionTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
        _eventWritter = _fixture.GetHostedService<EventBackgroundWritter>();
    }

    [Fact]
    public async Task Can_Ingest_For_Known_App()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), app.AppKey);
        var code = await client.TrackEvent(DateTime.UtcNow.AddHours(-10), "Button Clicked", null);
        code.Should().Be(HttpStatusCode.OK);

        await _eventWritter.FlushEvents();

        var count = await _fixture.UserA.CountEvents(app.Id, "24h");
        count.Should().Be(1);
    }

    [Fact]
    public async Task Can_Ingest_Future_Events()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), app.AppKey);
        var code = await client.TrackEvent(DateTime.UtcNow.AddYears(10), "Button Clicked", null);
        code.Should().Be(HttpStatusCode.OK);

        await _eventWritter.FlushEvents();

        var count = await _fixture.UserA.CountEvents(app.Id, "24h");
        count.Should().Be(1);
    }

    [Fact]
    public async Task Can_Ingest_Multiple_Events()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), app.AppKey);
        var code = await client.TrackEvents(new List<(DateTime, string, object?)> {
            (DateTime.UtcNow, "App Started", null),
            (DateTime.UtcNow, "Menu Opened", null),
            (DateTime.UtcNow, "Button Clicked", null),
        });
        code.Should().Be(HttpStatusCode.OK);

        await _eventWritter.FlushEvents();

        var count = await _fixture.UserA.CountEvents(app.Id, "24h");
        count.Should().Be(3);
    }

    [Fact]
    public async Task Should_drop_events_if_invalid()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), app.AppKey);
        var code = await client.TrackEvents(new List<(DateTime, string, object?)> {
            (DateTime.UtcNow, "App Started", null),
            (DateTime.UtcNow, "Menu Opened", null),
            (DateTime.UtcNow, "Button Clicked", "INVALID PROPS"),
        });
        code.Should().Be(HttpStatusCode.OK);

        await _eventWritter.FlushEvents();

        var count = await _fixture.UserA.CountEvents(app.Id, "24h");
        count.Should().Be(2);
    }

    [Fact]
    public async Task Cant_Ingest_More_Than_25_Events_In_Batch()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), app.AppKey);
        var code = await client.TrackEvents(Enumerable.Range(1, 26).Select(i => (DateTime.UtcNow, "Button Clicked", (object?)null)));
        code.Should().Be(HttpStatusCode.BadRequest);

        await _eventWritter.FlushEvents();

        var count = await _fixture.UserA.CountEvents(app.Id, "24h");
        count.Should().Be(0);
    }

    [Fact]
    public async Task Cant_Ingest_Unknown_AppKey()
    {
        var client = new IngestionClient(_fixture.CreateClient(), "THIS-DOES-NOT-EXIST");
        var code = await client.TrackEvent(DateTime.UtcNow, "Button Clicked", null);
        code.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory, MemberData(nameof(ValidProps))]
    public async Task Cant_Ingest_With_Valid_Props(object props)
    {
        var appA = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var client = new IngestionClient(_fixture.CreateClient(), appA.AppKey);
        var code = await client.TrackEvent(DateTime.UtcNow, "Button Clicked", props);
        code.Should().Be(HttpStatusCode.OK);
    }
 
    public static IEnumerable<object[]> ValidProps => 
        new List<object[]>
        {
            new object[] { "{}" },
            new object[] { "{\"name\":\"Bob\"}" },
            new object[] { "{\"name\":\"Bob\", \"age\":20}" },
            new object[] { new { } },
            new object[] { new { age = 20 } },
            new object[] { new { name = "Bob", age = 20 } },
            new object[] { new { name = "Bob", valid = true } },
            new object[] { new { name = "Bob", surname = (object?)null } },
        };

    [Theory, MemberData(nameof(InvalidProps))]
    public async Task Cant_Ingest_With_Invalid_Props(object props)
    {
        var appA = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var client = new IngestionClient(_fixture.CreateClient(), appA.AppKey);
        var code = await client.TrackEvent(DateTime.UtcNow, "Button Clicked", props);
        code.Should().Be(HttpStatusCode.BadRequest);
    }

    public static IEnumerable<object[]> ValidSessionIds => 
        new List<object[]>
        {
            new object[] { "8ee47a56-a457-4513-a65f-2b8c3065eb95" },
            new object[] { "0ef724ce-7d46-4bde-9e0f-69303ef3f2af" },
            new object[] { "1234-5678-9012-3456" },
            new object[] { IngestionClient.NewSessionId().ToString() },
        };

    [Theory, MemberData(nameof(ValidSessionIds))]
    public async Task Can_Ingest_Valid_SessionId(string sessionId)
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), app.AppKey);
        client.SetSessionId(sessionId);

        var code = await client.TrackEvent(DateTime.UtcNow, "Button Clicked", null);
        code.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567890123456789012345678901234567890")]
    [InlineData(null)]
    public async Task Cant_Ingest_Invalid_SessionId(string sessionId)
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), app.AppKey);
        client.SetSessionId(sessionId);

        var code = await client.TrackEvent(DateTime.UtcNow, "Button Clicked", null);
        code.Should().Be(HttpStatusCode.BadRequest);
    }
 
    public static IEnumerable<object[]> InvalidProps => 
        new List<object[]>
        {
            new object[] { 1 },
            new object[] { "Something" },
            new object[] { "[1,2,3]" },
            new object[] { "" },
            new object[] { new string[]{"A", "B", "C"} },
            new object[] { new { name = "Bob", age = 20, list = new int[]{1,2,3} } },
            new object[] { "{\"name\":\"Bob\", \"age\":20, \"list\": [1,2,3]}" },
            new object[] { "{\"\":\"Bob\"}" },
        };

    [Fact]
    public async Task Cant_Read_Stats_From_Other_Users()
    {
        var appA = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), appA.AppKey);
        await client.TrackEvent(DateTime.UtcNow, "Button Clicked", null);
        
        var responseA = await _fixture.UserA.GetKeyMetrics(appA.Id, "24h");
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseB = await _fixture.UserB.GetKeyMetrics(appA.Id, "24h");
        responseB.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Check_Rate_Limiting()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), app.AppKey, "10.0.0.0");
        for (var i = 1; i <= 22; i++)
        {
            var code1 = await client.TrackEvent(DateTime.UtcNow, "Button Clicked", null);

            if (i <= 20)
                code1.Should().Be(HttpStatusCode.OK);
            else
                code1.Should().Be(HttpStatusCode.TooManyRequests);
        }

        // A different IP should succeed because it uses a different IP
        var client2 = new IngestionClient(_fixture.CreateClient(), app.AppKey, "12.0.0.0");
        var code2 = await client2.TrackEvent(DateTime.UtcNow, "Button Clicked", null);
        code2.Should().Be(HttpStatusCode.OK);

        await _eventWritter.FlushEvents();

        var count = await _fixture.UserA.CountEvents(app.Id, "24h");
        count.Should().Be(21);
    }
}