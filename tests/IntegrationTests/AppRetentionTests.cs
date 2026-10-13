using System.Net;
using Aptabase.Features.Apps;
using Aptabase.Features.Authentication;
using Aptabase.Features.Ingestion.Buffer;
using Aptabase.IntegrationTests.Clients;
using AwesomeAssertions;
using ClickHouse.Client.ADO;
using Dapper;
using Xunit;

namespace Aptabase.IntegrationTests;

[Collection("Integration Tests")]
public class AppRetentionTests(IntegrationTestsFixture fixture)
{
    [Theory]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(24)]
    [InlineData(60)]
    public async Task Can_Choose_Retention_When_Creating_And_Editing(int months)
    {
        var app = await fixture.UserA.CreateApp(Guid.NewGuid().ToString(), months);
        app.RetentionMonths.Should().Be(months);
        var edited = months == 12 ? 24 : 12;

        var response = await fixture.UserA.Put($"/api/_apps/{app.Id}", new { name = app.Name, retentionMonths = edited });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<Application>())!.RetentionMonths.Should().Be(edited);

        // An older client changing only the name must preserve the saved policy.
        response = await fixture.UserA.Put($"/api/_apps/{app.Id}", new { name = "Renamed app" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<Application>())!.RetentionMonths.Should().Be(edited);

        response = await fixture.UserA.Get($"/api/_apps/{app.Id}");
        (await response.Content.ReadFromJsonAsync<Application>())!.RetentionMonths.Should().Be(edited);
    }

    [Fact]
    public async Task Creation_Without_A_Policy_Defaults_To_Five_Years()
    {
        var response = await fixture.UserA.Post("/api/_apps", new { name = Guid.NewGuid().ToString() });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<Application>())!.RetentionMonths.Should().Be(60);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(61)]
    public async Task Rejects_Unsupported_Retention(int months)
    {
        var response = await fixture.UserA.Post("/api/_apps", new { name = "Invalid app", retentionMonths = months });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var app = await fixture.UserA.CreateApp(Guid.NewGuid().ToString(), 24);
        response = await fixture.UserA.Put($"/api/_apps/{app.Id}", new { name = app.Name, retentionMonths = months });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response = await fixture.UserA.Get($"/api/_apps/{app.Id}");
        (await response.Content.ReadFromJsonAsync<Application>())!.RetentionMonths.Should().Be(24);
    }

    [Fact]
    public async Task Another_User_Cannot_Change_Retention()
    {
        var app = await fixture.UserA.CreateApp(Guid.NewGuid().ToString(), 24);

        var response = await fixture.UserB.Put($"/api/_apps/{app.Id}", new { name = app.Name, retentionMonths = 6 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var me = await fixture.UserB.Get("/api/_auth/me");
        var sharedUser = (await me.Content.ReadFromJsonAsync<UserAccount>())!;
        response = await fixture.UserA.Put($"/api/_apps/{app.Id}/shares/{Uri.EscapeDataString(sharedUser.Email)}", new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response = await fixture.UserB.Get($"/api/_apps/{app.Id}");
        (await response.Content.ReadFromJsonAsync<Application>())!.RetentionMonths.Should().Be(24);
        response = await fixture.UserB.Put($"/api/_apps/{app.Id}", new { name = app.Name, retentionMonths = 6 });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        response = await fixture.UserA.Get($"/api/_apps/{app.Id}");
        (await response.Content.ReadFromJsonAsync<Application>())!.RetentionMonths.Should().Be(24);
    }

    [Fact]
    public async Task Policy_Changes_Affect_New_Requests_While_Buffered_Events_Keep_Their_Deadline()
    {
        var app = await fixture.UserA.CreateApp(Guid.NewGuid().ToString(), 24);
        var client = new IngestionClient(fixture.CreateClient(), app.AppKey);
        var now = DateTime.UtcNow.AddMinutes(-20);
        var timestamp = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

        (await client.TrackEvent(timestamp, "two_years", null)).Should().Be(HttpStatusCode.OK);
        var response = await fixture.UserA.Put($"/api/_apps/{app.Id}", new { name = app.Name, retentionMonths = 12 });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.TrackEvents([(timestamp, "one_year_a", null), (timestamp, "one_year_b", null)]))
            .Should().Be(HttpStatusCode.OK);
        response = await fixture.UserA.Put($"/api/_apps/{app.Id}", new { name = app.Name, retentionMonths = 60 });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.TrackEvent(timestamp, "five_years", null)).Should().Be(HttpStatusCode.OK);

        await fixture.GetHostedService<EventBackgroundWritter>().FlushEvents();
        var connection = fixture.GetService<ClickHouseConnection>();
        var rows = (await connection.QueryAsync<StoredEvent>(
            "SELECT event_name, ttl FROM events WHERE app_id = @appId", new { appId = app.Id })).ToArray();

        rows.Should().HaveCount(4);
        rows.Single(r => r.EventName == "two_years").TTL.Should().Be(timestamp.AddMonths(24));
        rows.Where(r => r.EventName.StartsWith("one_year")).Should().AllSatisfy(r => r.TTL.Should().Be(timestamp.AddMonths(12)));
        rows.Single(r => r.EventName == "five_years").TTL.Should().Be(timestamp.AddMonths(60));
    }

    private class StoredEvent
    {
        public string EventName { get; set; } = "";
        public DateTime TTL { get; set; }
    }
}
