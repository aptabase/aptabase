using Xunit;
using System.Net;
using FluentAssertions;
using Aptabase.Features.Apps;

namespace Aptabase.IntegrationTests;

[Collection("Integration Tests")]
public class IngestionTests : IAsyncLifetime
{
    private readonly HttpClient _client;

    private string _appKey = "";
    public IngestionTests(IntegrationTestsFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    public async Task InitializeAsync()
    {
        var email = $"jon.snow.{Guid.NewGuid()}@got.com";
        var registration = await _client.PostAsJsonAsync("/api/_auth/register", new { name = "Jon Snow", email });
        registration.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmUrl = await MailCatcher.GetLinkSentTo(email);
        var confirmation = await _client.GetAsync(confirmUrl);
        confirmation.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var createApp = await _client.PostAsJsonAsync("/api/_apps", new { name = "Demo App" });
        createApp.StatusCode.Should().Be(HttpStatusCode.OK);

        var apps = await _client.GetFromJsonAsync<Application[]>("/api/_apps");
        apps?.Length.Should().Be(1);
        var app = apps?.ElementAt(0) ?? throw new Exception("No apps found");

        _appKey = app.AppKey;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Can_Ingest_For_Known_apps()
    {
        var body = CreateNewEvent(_appKey);
        var newEvent = await _client.PostAsync($"/api/v0/event", body);
        newEvent.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cant_Ingest_Unknown_AppKey()
    {
        var body = CreateNewEvent("THIS-APP-KEY-DOESNT-EXIST");
        var newEvent = await _client.PostAsync($"/api/v0/event", body);
        newEvent.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Check_Rate_Limiting()
    {
        for (var i = 1; i <= 22; i++)
        {
            var body = CreateNewEvent(_appKey);
            body.Headers.Add("CloudFront-Viewer-Address", "10.0.0.0");
            var newEvent = await _client.PostAsync($"/api/v0/event", body);

            if (i <= 20)
                newEvent.StatusCode.Should().Be(HttpStatusCode.OK);
            else
                newEvent.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }

        // A different IP should succeed
        var body2 = CreateNewEvent(_appKey);
        body2.Headers.Add("CloudFront-Viewer-Address", "10.0.0.1");
        var newEvent2 = await _client.PostAsync($"/api/v0/event", body2);
        newEvent2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private JsonContent CreateNewEvent(string appKey)
    {
        var content = JsonContent.Create(new
        {
            eventName = "ButtonClicked",
            timestamp = DateTime.UtcNow.ToString("o"),
            sessionId = Guid.NewGuid().ToString(),
            systemProps = new
            {
                isDebug = false,
                osName = "macOs",
                osVersion = "13.5",
                appVersion = "1.0.0",
                sdkVersion = "aptabase-swift@0.0.0"
            }
        });
        content.Headers.Add("App-Key", appKey);
        return content;
    }
}