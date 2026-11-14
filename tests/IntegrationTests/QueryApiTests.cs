using System.Net;
using System.Text.Json;
using Aptabase.Data;
using Aptabase.IntegrationTests.Clients;
using AwesomeAssertions;
using Dapper;
using Xunit;

namespace Aptabase.IntegrationTests;

[Collection("Integration Tests")]
public class QueryApiTests
{
    private readonly IntegrationTestsFixture _fixture;
    private readonly OAuthTestClient _oauth;

    public QueryApiTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
        _oauth = new OAuthTestClient(fixture.CreateClient());
    }

    [Theory]
    [InlineData("/api/v1/apps")]
    [InlineData("/mcp")]
    public async Task Missing_Token_Is_Challenged_With_The_Resource_Metadata_Url(string path)
    {
        var response = await _oauth.Get(path, null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var challenge = response.Headers.WwwAuthenticate.ToString();
        challenge.Should().Contain($"resource_metadata=\"{OAuthTestClient.Origin}/.well-known/oauth-protected-resource\"");
        challenge.Should().Contain("scope=\"stats:read\"");
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("eyJhbGciOiJub25lIiwidHlwIjoiYXQrand0In0.eyJncmFudF9pZCI6IngifQ.")]
    public async Task Invalid_Tokens_Are_401(string token)
    {
        (await _oauth.Get("/api/v1/apps", token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Session_Cookies_Do_Not_Authenticate_The_Query_Api()
    {
        (await _fixture.UserA.Http.GetAsync("/api/v1/apps")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Magic_Link_Tokens_Are_Not_Access_Tokens()
    {
        var manager = _fixture.GetService<Aptabase.Features.Authentication.IAuthTokenManager>();
        var token = manager.CreateAuthToken(Aptabase.Features.Authentication.AuthTokenType.SignIn, "x", _fixture.UserA.Email);

        (await _oauth.Get("/api/v1/apps", token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Service_Client_Reads_Its_App_And_Nothing_Else()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var other = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var foreign = await _fixture.UserB.CreateApp(Guid.NewGuid().ToString());
        var (clientId, secret) = await OAuthTestClient.CreateServiceClient(_fixture.UserA, app.Id);
        var tokens = await _oauth.ClientCredentials(clientId, secret);

        tokens.RefreshToken.Should().BeNull();

        var metrics = await _oauth.GetJson($"/api/v1/apps/{app.Id}/metrics?period=7d", tokens.AccessToken);
        metrics.GetProperty("data").GetProperty("sessions").GetInt32().Should().Be(0);
        metrics.GetProperty("window").GetProperty("granularity").GetString().Should().Be("day");

        // Same owner, different app: the client covers exactly one
        var sameOwner = await _oauth.Get($"/api/v1/apps/{other.Id}/metrics", tokens.AccessToken);
        var differentOwner = await _oauth.Get($"/api/v1/apps/{foreign.Id}/metrics", tokens.AccessToken);
        var missing = await _oauth.Get("/api/v1/apps/doesNotExist0000000000/metrics", tokens.AccessToken);

        sameOwner.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        differentOwner.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        missing.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // One answer for all three, so ids cannot be told apart
        var body = await sameOwner.Content.ReadAsStringAsync();
        (await differentOwner.Content.ReadAsStringAsync()).Should().Be(body);
        (await missing.Content.ReadAsStringAsync()).Should().Be(body);

        var apps = await _oauth.GetJson("/api/v1/apps", tokens.AccessToken);
        apps.GetProperty("data").EnumerateArray().Select(a => a.GetProperty("id").GetString()).Should().Equal(app.Id);
    }

    [Fact]
    public async Task Revoked_Service_Client_Stops_Working()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var (clientId, secret) = await OAuthTestClient.CreateServiceClient(_fixture.UserA, app.Id);
        var tokens = await _oauth.ClientCredentials(clientId, secret);
        (await _oauth.Get($"/api/v1/apps/{app.Id}/metrics", tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await _fixture.UserA.Http.DeleteAsync($"/api/_apps/{app.Id}/clients/{clientId}")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await _oauth.Get($"/api/v1/apps/{app.Id}/metrics", tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _oauth.ClientCredentialsRaw(clientId, secret)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_For_The_Mcp_Resource_Is_Not_Valid_On_The_Rest_Api()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var (clientId, secret) = await OAuthTestClient.CreateServiceClient(_fixture.UserA, app.Id);

        var mcpOnly = await _oauth.ClientCredentials(clientId, secret, OAuthTestClient.Origin + "/mcp");
        var origin = await _oauth.ClientCredentials(clientId, secret, OAuthTestClient.Origin + "/");

        (await _oauth.Get("/api/v1/apps", mcpOnly.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _oauth.Get("/api/v1/apps", origin.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await _oauth.ClientCredentialsRaw(clientId, secret, "https://elsewhere.example")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task User_Grant_Follows_Live_Sharing_Not_The_Stored_List()
    {
        var owned = await _fixture.UserB.CreateApp(Guid.NewGuid().ToString());
        var shared = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        await _fixture.UserA.ShareApp(shared.Id, _fixture.UserB.Email);

        // B picks both apps explicitly, so the shared one is stored in the grant
        var tokens = await _oauth.Connect(_fixture.UserB, appIds: [owned.Id, shared.Id]);

        var before = await _oauth.GetJson("/api/v1/apps", tokens.AccessToken);
        before.GetProperty("data").EnumerateArray().Select(a => a.GetProperty("id").GetString()).Should().BeEquivalentTo([owned.Id, shared.Id]);

        (await _fixture.UserA.Http.DeleteAsync($"/api/_apps/{shared.Id}/shares/{Uri.EscapeDataString(_fixture.UserB.Email)}")).StatusCode.Should().Be(HttpStatusCode.OK);

        // The grant still lists the app. It must be invisible, not merely unqueryable.
        var after = await _oauth.GetJson("/api/v1/apps", tokens.AccessToken);
        after.GetProperty("data").EnumerateArray().Select(a => a.GetProperty("id").GetString()).Should().Equal(owned.Id);
        (await _oauth.Get($"/api/v1/apps/{shared.Id}/metrics", tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var grants = await _fixture.UserB.Http.GetFromJsonAsync<JsonElement>("/api/_oauth/grants");
        grants.ToString().Should().NotContain(shared.Name);
    }

    [Fact]
    public async Task Deleted_App_Is_Forbidden()
    {
        var app = await _fixture.UserB.CreateApp(Guid.NewGuid().ToString());
        var tokens = await _oauth.Connect(_fixture.UserB);

        await _fixture.UserB.DeleteApp(app.Id);

        (await _oauth.Get($"/api/v1/apps/{app.Id}/metrics", tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Sessions_Need_The_Sessions_Scope()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var (statsId, statsSecret) = await OAuthTestClient.CreateServiceClient(_fixture.UserA, app.Id);
        var stats = await _oauth.ClientCredentials(statsId, statsSecret);

        var response = await _oauth.Get($"/api/v1/apps/{app.Id}/sessions?live=true", stats.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var challenge = response.Headers.WwwAuthenticate.ToString();
        challenge.Should().Contain("error=\"insufficient_scope\"");
        challenge.Should().Contain("scope=\"sessions:read\"");
    }

    [Theory]
    [InlineData("metrics?period=nope")]
    [InlineData("metrics?from=2026-01-01")]
    [InlineData("metrics?from=2026-02-01&to=2026-01-01")]
    [InlineData("metrics?period=7d&from=2026-01-01&to=2026-01-02")]
    [InlineData("metrics?period=30d&granularity=hour")]
    [InlineData("metrics?build_mode=staging")]
    [InlineData("metrics?country_code=US,DE")]
    [InlineData("metrics?period=90d")]
    [InlineData("metrics?period=all")]
    [InlineData("breakdown?dimension=user_id")]
    [InlineData("breakdown")]
    [InlineData("breakdown?dimension=country&metric=revenue")]
    [InlineData("breakdown?dimension=country&limit=1000")]
    [InlineData("breakdown?dimension=app_build_number&period=30d")]
    [InlineData("event-props")]
    public async Task Invalid_Requests_Are_400_Before_Any_Query(string query)
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var (clientId, secret) = await OAuthTestClient.CreateServiceClient(_fixture.UserA, app.Id);
        var tokens = await _oauth.ClientCredentials(clientId, secret);

        var response = await _oauth.Get($"/api/v1/apps/{app.Id}/{query}", tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Every_Endpoint_Answers_For_An_Authorized_App()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var (clientId, secret) = await OAuthTestClient.CreateServiceClient(_fixture.UserA, app.Id, "stats:read sessions:read");
        var tokens = await _oauth.ClientCredentials(clientId, secret);

        foreach (var path in new[]
        {
            "metrics", "metrics?build_mode=debug", "metrics?from=2026-11-01T00:00:00Z&to=2026-11-02T00:00:00Z",
            "timeseries?period=7d", "timeseries?period=24h",
            "breakdown?dimension=country", "breakdown?dimension=event&metric=events&limit=5", "breakdown?dimension=app_build_number&period=24h",
            "event-props?event_name=app_started",
            "sessions?live=true", "sessions?period=7d", "sessions/00000000-0000-0000-0000-000000000000",
            "usage",
        })
        {
            var response = await _oauth.Get($"/api/v1/apps/{app.Id}/{path}", tokens.AccessToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"{path}: {await response.Content.ReadAsStringAsync()}");
        }
    }

    [Fact]
    public async Task Session_Listing_Describes_Its_Window_Without_A_Granularity()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var (clientId, secret) = await OAuthTestClient.CreateServiceClient(_fixture.UserA, app.Id, "stats:read sessions:read");
        var tokens = await _oauth.ClientCredentials(clientId, secret);

        var sessions = await _oauth.GetJson($"/api/v1/apps/{app.Id}/sessions?period=24h", tokens.AccessToken);

        sessions.GetProperty("window").TryGetProperty("from", out _).Should().BeTrue();
        sessions.GetProperty("window").TryGetProperty("granularity", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Injection_Attempts_In_Filters_Stay_Inside_String_Literals()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var (clientId, secret) = await OAuthTestClient.CreateServiceClient(_fixture.UserA, app.Id);
        var tokens = await _oauth.ClientCredentials(clientId, secret);
        var payload = Uri.EscapeDataString("x' OR app_id != '");

        var response = await _oauth.Get($"/api/v1/apps/{app.Id}/metrics?event_name={payload}", tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("events").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Monthly_Allowance_Is_Exact_And_Cache_Hits_Are_Free()
    {
        var app = await _fixture.UserB.CreateApp(Guid.NewGuid().ToString());
        var (clientId, secret) = await OAuthTestClient.CreateServiceClient(_fixture.UserB, app.Id);
        var tokens = await _oauth.ClientCredentials(clientId, secret);
        var ownerId = await OwnerOf(app.Id);

        (await _oauth.Get($"/api/v1/apps/{app.Id}/metrics?period=7d", tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Put the owner one request short of the Free allowance of 100
        using (var cn = _fixture.GetService<IDbContext>().Connection)
            await cn.ExecuteAsync(
                "UPDATE api_grant_usage SET requests = 99 - (SELECT COALESCE(SUM(requests), 0) FROM api_grant_usage WHERE owner_id = @ownerId AND grant_id <> @clientId) WHERE grant_id = @clientId",
                new { ownerId, clientId });

        (await _oauth.Get($"/api/v1/apps/{app.Id}/metrics?period=14d", tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        var refused = await _oauth.Get($"/api/v1/apps/{app.Id}/metrics?period=30d", tokens.AccessToken);
        refused.StatusCode.Should().Be((HttpStatusCode)429);
        refused.Headers.RetryAfter.Should().NotBeNull();
        var body = await refused.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetString().Should().Be("quota_exceeded");
        body.GetProperty("allowance").GetInt64().Should().Be(100);
        body.GetProperty("remaining").GetInt64().Should().Be(0);

        // A question somebody asked in the last few minutes is still answered, and not counted
        (await _oauth.Get($"/api/v1/apps/{app.Id}/metrics?period=7d", tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        using (var cn = _fixture.GetService<IDbContext>().Connection)
        {
            var total = await cn.ExecuteScalarAsync<long>("SELECT SUM(requests) FROM api_grant_usage WHERE owner_id = @ownerId", new { ownerId });
            var rejected = await cn.ExecuteScalarAsync<long>("SELECT SUM(rejected) FROM api_grant_usage WHERE owner_id = @ownerId", new { ownerId });
            total.Should().Be(100);
            rejected.Should().Be(1);
            await cn.ExecuteAsync("UPDATE api_grant_usage SET requests = 0 WHERE owner_id = @ownerId", new { ownerId });
        }
    }

    [Fact]
    public async Task Usage_Is_Visible_To_Readers_Without_Collaborator_Emails()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        await _fixture.UserA.ShareApp(app.Id, _fixture.UserB.Email);
        var (clientId, secret) = await OAuthTestClient.CreateServiceClient(_fixture.UserA, app.Id);
        var service = await _oauth.ClientCredentials(clientId, secret);
        var userGrant = await _oauth.Connect(_fixture.UserA, appIds: [app.Id]);

        (await _oauth.Get($"/api/v1/apps/{app.Id}/metrics?period=7d", service.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _oauth.Get($"/api/v1/apps/{app.Id}/timeseries?period=7d", userGrant.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        var asOwner = await _fixture.UserA.Http.GetFromJsonAsync<JsonElement>($"/api/_apps/{app.Id}/api-usage");
        asOwner.GetProperty("isOwner").GetBoolean().Should().BeTrue();
        asOwner.GetProperty("clients").GetArrayLength().Should().Be(2);
        asOwner.GetProperty("allowance").GetInt64().Should().Be(100);

        var asShared = await _fixture.UserB.Http.GetAsync($"/api/_apps/{app.Id}/api-usage");
        asShared.StatusCode.Should().Be(HttpStatusCode.OK);
        var sharedBody = await asShared.Content.ReadAsStringAsync();
        sharedBody.Should().NotContain(_fixture.UserA.Email);
        sharedBody.Should().Contain("Jon Snow");
        sharedBody.Should().NotContain("Free");

        // The tool reports the same numbers the page does
        var usage = await _oauth.GetJson($"/api/v1/apps/{app.Id}/usage", service.AccessToken);
        usage.GetProperty("data").GetProperty("api").GetProperty("allowance").GetInt64().Should().Be(100);
        usage.GetProperty("data").GetProperty("api").GetProperty("app_requests_this_month").GetInt64().Should().BeGreaterThanOrEqualTo(2);

        var stranger = await _fixture.UserB.CreateApp(Guid.NewGuid().ToString());
        (await _fixture.UserA.Http.GetAsync($"/api/_apps/{stranger.Id}/api-usage")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OpenApi_Document_Is_Public_And_Lists_Only_V1()
    {
        var doc = await _fixture.CreateClient().GetFromJsonAsync<JsonElement>("/api/v1/openapi.json");

        var paths = doc.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToArray();
        paths.Should().Contain("/api/v1/apps/{appId}/metrics");
        paths.Should().OnlyContain(p => p.StartsWith("/api/v1/"));
        doc.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("oauth2", out _).Should().BeTrue();
    }

    private async Task<string> OwnerOf(string appId)
    {
        using var cn = _fixture.GetService<IDbContext>().Connection;
        return (await cn.ExecuteScalarAsync<string>("SELECT owner_id FROM apps WHERE id = @appId", new { appId }))!;
    }
}
