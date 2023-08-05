using Xunit;
using System.Net;
using FluentAssertions;
using Aptabase.IntegrationTests.Clients;

namespace Aptabase.IntegrationTests;

[Collection("Integration Tests")]
public class IngestionTests
{
    private readonly IntegrationTestsFixture _fixture;

    public IngestionTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Can_Ingest_For_Known_App()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), app.AppKey, "127.0.0.1");
        var code = await client.TrackEvent(DateTime.UtcNow.AddHours(-10), "Button Clicked");
        code.Should().Be(HttpStatusCode.OK);

        var count = await _fixture.UserA.CountEvents(app.Id, "24h");
        count.Should().Be(1);
    }

    [Fact]
    public async Task Can_Ingest_Future_Events()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), app.AppKey, "127.0.0.1");
        var code = await client.TrackEvent(DateTime.UtcNow.AddYears(10), "Button Clicked");
        code.Should().Be(HttpStatusCode.OK);

        var count = await _fixture.UserA.CountEvents(app.Id, "24h");
        count.Should().Be(1);
    }

    [Fact]
    public async Task Can_Ingest_Multiple_Events()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), app.AppKey, "127.0.0.1");
        var code = await client.TrackEvents(new List<(DateTime, string)> {
            (DateTime.UtcNow, "App Started"),
            (DateTime.UtcNow, "Menu Opened"),
            (DateTime.UtcNow, "Button Clicked"),
        });
        code.Should().Be(HttpStatusCode.OK);

        var count = await _fixture.UserA.CountEvents(app.Id, "24h");
        count.Should().Be(3);
    }

    [Fact]
    public async Task Cant_Ingest_More_Than_25_Events_In_Batch()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), app.AppKey, "127.0.0.1");
        var code = await client.TrackEvents(Enumerable.Range(1, 26).Select(i => (DateTime.UtcNow, "Button Clicked")));
        code.Should().Be(HttpStatusCode.BadRequest);

        var count = await _fixture.UserA.CountEvents(app.Id, "24h");
        count.Should().Be(0);
    }

    [Fact]
    public async Task Cant_Ingest_Unknown_AppKey()
    {
        var client = new IngestionClient(_fixture.CreateClient(), "THIS-DOES-NOT-EXIST", "127.0.0.1");
        var code = await client.TrackEvent(DateTime.UtcNow, "Button Clicked");
        code.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cant_Read_Stats_From_Other_Users()
    {
        var appA = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), appA.AppKey, "127.0.0.1");
        await client.TrackEvent(DateTime.UtcNow, "Button Clicked");
        
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
            var code1 = await client.TrackEvent(DateTime.UtcNow, "Button Clicked");

            if (i <= 20)
                code1.Should().Be(HttpStatusCode.OK);
            else
                code1.Should().Be(HttpStatusCode.TooManyRequests);
        }

        // A different IP should succeed because it uses a different IP
        var client2 = new IngestionClient(_fixture.CreateClient(), app.AppKey, "12.0.0.0");
        var code2 = await client2.TrackEvent(DateTime.UtcNow, "Button Clicked");
        code2.Should().Be(HttpStatusCode.OK);

        var count = await _fixture.UserA.CountEvents(app.Id, "24h");
        count.Should().Be(21);
    }
}