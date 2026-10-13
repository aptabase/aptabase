using System.Net;
using System.Text.Json;
using Aptabase.Data;
using Aptabase.Features.Apps;
using Aptabase.Features.Authentication;
using Aptabase.Features.Blob;
using Aptabase.Features.Ingestion.Buffer;
using Aptabase.IntegrationTests.Clients;
using AwesomeAssertions;
using ClickHouse.Client.ADO;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sdk_Cannot_Override_App_Retention_And_Offsets_Use_Utc(bool debug)
    {
        var app = await fixture.UserA.CreateApp(Guid.NewGuid().ToString(), 12);
        var client = new IngestionClient(fixture.CreateClient(), app.AppKey);
        var now = DateTime.UtcNow.AddMinutes(-20);
        var timestamp = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        var offsetTimestamp = new DateTimeOffset(timestamp).ToOffset(TimeSpan.FromHours(5.5));
        var body = JsonSerializer.Serialize(new
        {
            eventName = "policy_from_server", timestamp = offsetTimestamp.ToString("o"), sessionId = "session",
            systemProps = new { isDebug = debug, osName = "iOS", sdkVersion = "audit@1.0" },
            retentionMonths = 60, ttl = "2099-01-01T00:00:00Z", appId = "another_app",
        });

        (await client.PostRaw("/api/v0/event", body)).Should().Be(HttpStatusCode.OK);
        await fixture.GetHostedService<EventBackgroundWritter>().FlushEvents();
        var row = await fixture.GetService<ClickHouseConnection>().QuerySingleAsync<StoredEvent>(
            "SELECT event_name, timestamp, ttl FROM events WHERE app_id = @appId",
            new { appId = debug ? app.Id + "_DEBUG" : app.Id });
        row.Timestamp.Should().Be(timestamp);
        row.TTL.Should().Be(debug ? timestamp.AddDays(182) : timestamp.AddMonths(12));
    }

    [Theory]
    [InlineData("transfer")]
    [InlineData("delete")]
    [InlineData("change_policy")]
    public async Task Edit_Rechecks_Ownership_And_Returns_The_Committed_Policy(string concurrentChange)
    {
        var appId = "";
        var otherUser = (await (await fixture.UserB.Get("/api/_auth/me")).Content.ReadFromJsonAsync<UserAccount>())!;
        var blob = new DuringUploadBlobService(async () =>
        {
            using var connection = fixture.GetService<IDbContext>().Connection;
            var sql = concurrentChange switch
            {
                "transfer" => "UPDATE apps SET owner_id = @userId WHERE id = @appId",
                "delete" => "UPDATE apps SET deleted_at = now() WHERE id = @appId",
                _ => "UPDATE apps SET retention_months = 6 WHERE id = @appId",
            };
            await connection.ExecuteAsync(sql, new { appId, userId = otherUser.Id });
        });
        using var factory = new CustomWebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IBlobService>(blob)));
        var owner = new AccountClient(factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }));
        await owner.CreateAccount("Retention audit", $"retention.{Guid.NewGuid()}@example.test");
        var app = await owner.CreateApp(Guid.NewGuid().ToString(), 24);
        appId = app.Id;

        // The upload pauses an authorized edit while another request transfers/deletes the app,
        // or changes its policy. Omitting retention must preserve the concurrent policy change.
        var response = await owner.Put($"/api/_apps/{app.Id}", new
        {
            name = "Edited app", icon = Convert.ToBase64String([1, 2, 3]),
            retentionMonths = concurrentChange == "change_policy" ? (int?)null : 12,
        });

        response.StatusCode.Should().Be(concurrentChange == "change_policy" ? HttpStatusCode.OK : HttpStatusCode.NotFound);
        using var checkConnection = fixture.GetService<IDbContext>().Connection;
        var months = await checkConnection.QuerySingleAsync<int>("SELECT retention_months FROM apps WHERE id = @appId", new { appId });
        months.Should().Be(concurrentChange == "change_policy" ? 6 : 24);
        if (concurrentChange == "change_policy")
            (await response.Content.ReadFromJsonAsync<Application>())!.RetentionMonths.Should().Be(6);
    }

    [Fact]
    public async Task Invalid_And_Empty_Event_Requests_Do_Not_Query_Retention()
    {
        using var factory = new CustomWebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IAppQueries>(provider =>
                new ObservedAppQueries(new AppQueries(provider.GetRequiredService<IDbContext>())))));
        var owner = new AccountClient(factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }));
        await owner.CreateAccount("Retention audit", $"retention.{Guid.NewGuid()}@example.test");
        var app = await owner.CreateApp(Guid.NewGuid().ToString(), 24);
        var client = new IngestionClient(factory.CreateClient(), app.AppKey);
        var queries = (ObservedAppQueries)factory.Services.GetRequiredService<IAppQueries>();

        (await client.PostRaw("/api/v0/event", "invalid-json")).Should().Be(HttpStatusCode.BadRequest);
        (await client.TrackEvent(DateTime.UtcNow.AddDays(-10), "too_old", null)).Should().Be(HttpStatusCode.BadRequest);
        (await client.TrackEvents([])).Should().Be(HttpStatusCode.OK);
        (await client.TrackEvents([(DateTime.UtcNow.AddDays(-10), "too_old", null)])).Should().Be(HttpStatusCode.OK);
        (await client.TrackEvents(Enumerable.Repeat((DateTime.UtcNow, "too_many", (object?)null), 26)))
            .Should().Be(HttpStatusCode.BadRequest);
        queries.RetentionReads.Should().Be(0);

        (await client.TrackEvents([(DateTime.UtcNow, "valid_a", null), (DateTime.UtcNow, "valid_b", null)]))
            .Should().Be(HttpStatusCode.OK);
        queries.RetentionReads.Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_Failed_Policy_Read_Does_Not_Buffer_Events_Using_The_Cached_Policy(bool batch)
    {
        using var factory = new CustomWebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IAppQueries>(provider =>
                new ObservedAppQueries(new AppQueries(provider.GetRequiredService<IDbContext>())))));
        var owner = new AccountClient(factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }));
        await owner.CreateAccount("Retention audit", $"retention.{Guid.NewGuid()}@example.test");
        var app = await owner.CreateApp(Guid.NewGuid().ToString(), 60);
        var client = new IngestionClient(factory.CreateClient(), app.AppKey);
        var timestamp = DateTime.UtcNow.AddMinutes(-5);
        var queries = (ObservedAppQueries)factory.Services.GetRequiredService<IAppQueries>();

        // Warm the status cache with the old policy, then save a shorter policy.
        (await client.TrackEvent(timestamp, "before_change", null)).Should().Be(HttpStatusCode.OK);
        (await owner.Put($"/api/_apps/{app.Id}", new { name = app.Name, retentionMonths = 6 }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        queries.FailRetentionRead = true;
        var status = batch
            ? await client.TrackEvents([(timestamp, "failed_read", null)])
            : await client.TrackEvent(timestamp, "failed_read", null);
        status.Should().Be(HttpStatusCode.InternalServerError);

        queries.FailRetentionRead = false;
        (await client.TrackEvent(timestamp, "after_recovery", null)).Should().Be(HttpStatusCode.OK);
        var writer = factory.Services.GetServices<IHostedService>().OfType<EventBackgroundWritter>().Single();
        await writer.FlushEvents();
        var rows = (await factory.Services.GetRequiredService<ClickHouseConnection>().QueryAsync<StoredEvent>(
            "SELECT event_name, ttl FROM events WHERE app_id = @appId", new { appId = app.Id })).ToArray();
        rows.Should().HaveCount(2);
        rows.Should().NotContain(r => r.EventName == "failed_read");
        // ClickHouse DateTime stores whole seconds.
        var expiry = timestamp.AddMonths(6);
        rows.Single(r => r.EventName == "after_recovery").TTL.Should()
            .Be(new DateTime(expiry.Ticks - expiry.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc));
    }

    private class DuringUploadBlobService(Func<Task> change) : IBlobService
    {
        public async Task<string> UploadAsync(string prefix, byte[] content, string contentType, CancellationToken cancellationToken)
        {
            await change();
            return "icons/audit.png";
        }

        public Task<Blob?> DownloadAsync(string path, CancellationToken cancellationToken) => Task.FromResult<Blob?>(null);
    }

    private class ObservedAppQueries(IAppQueries queries) : IAppQueries
    {
        public int RetentionReads { get; private set; }
        public bool FailRetentionRead { get; set; }
        public Task<Application?> GetActiveAppByAppKey(string appKey, CancellationToken cancellationToken) => queries.GetActiveAppByAppKey(appKey, cancellationToken);
        public Task MaskAsOnboarded(string appId, CancellationToken cancellationToken) => queries.MaskAsOnboarded(appId, cancellationToken);
        public Task<Application?> GetOwnedAppAsync(string appId, string userId) => queries.GetOwnedAppAsync(appId, userId);
        public Task<int?> GetRetentionMonthsAsync(string appId, CancellationToken cancellationToken)
        {
            RetentionReads++;
            if (FailRetentionRead)
                throw new InvalidOperationException("Simulated retention database outage");
            return queries.GetRetentionMonthsAsync(appId, cancellationToken);
        }
    }

    private class StoredEvent
    {
        public string EventName { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public DateTime TTL { get; set; }
    }
}
