using Xunit;
using FluentAssertions;
using Aptabase.IntegrationTests.Clients;
using Aptabase.Features.Ingestion.Buffer;

namespace Aptabase.IntegrationTests;

[Collection("Integration Tests")]
public class LiveViewTests
{
    private readonly IntegrationTestsFixture _fixture;
    private readonly EventBackgroundWritter _eventWritter;

    public LiveViewTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
        _eventWritter = _fixture.GetHostedService<EventBackgroundWritter>();
    }

    public static IEnumerable<object[]> ValidSessionIds => 
        new List<object[]>
        {
            new object[] { IngestionClient.NewSessionId().ToString() },
            new object[] { Guid.NewGuid().ToString() },
        };

    [Theory, MemberData(nameof(ValidSessionIds))]
    public async Task Can_Get_Session_Details(string sessionId)
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new IngestionClient(_fixture.CreateClient(), app.AppKey);
        client.SetSessionId(sessionId);
        
        await client.TrackEvent(DateTime.UtcNow, "App Started", null);
        await client.TrackEvent(DateTime.UtcNow, "Button Clicked", null);
        await _eventWritter.FlushEvents();

        var timeline = await _fixture.UserA.GetSessionTimeline(app.Id, sessionId);
        timeline?.EventsCount.Should().Be(2);
        timeline?.EventsName.Should().BeEquivalentTo("App Started", "Button Clicked");
    }
}