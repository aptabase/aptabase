using System.Net;
using System.Text.Json;
using System.Web;
using Aptabase.Data;
using Aptabase.IntegrationTests.Clients;
using AwesomeAssertions;
using Dapper;
using Xunit;

namespace Aptabase.IntegrationTests;

[Collection("Integration Tests")]
public class OAuthFlowTests
{
    private readonly IntegrationTestsFixture _fixture;
    private readonly OAuthTestClient _oauth;

    public OAuthFlowTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
        _oauth = new OAuthTestClient(fixture.CreateClient());
    }

    [Fact]
    public async Task Metadata_Uses_One_Issuer_String_Everywhere()
    {
        var http = _fixture.CreateClient();
        var server = await http.GetFromJsonAsync<JsonElement>("/.well-known/oauth-authorization-server");
        var resource = await http.GetFromJsonAsync<JsonElement>("/.well-known/oauth-protected-resource");

        server.GetProperty("issuer").GetString().Should().Be(OAuthTestClient.Origin);
        server.GetProperty("client_id_metadata_document_supported").GetBoolean().Should().BeTrue();
        server.GetProperty("authorization_response_iss_parameter_supported").GetBoolean().Should().BeTrue();
        server.GetProperty("token_endpoint_auth_methods_supported").EnumerateArray().Select(x => x.GetString()).Should().Contain("none");
        server.GetProperty("code_challenge_methods_supported").EnumerateArray().Select(x => x.GetString()).Should().Equal("S256");
        resource.GetProperty("resource").GetString().Should().Be(OAuthTestClient.Origin);
        resource.GetProperty("authorization_servers")[0].GetString().Should().Be(OAuthTestClient.Origin);
        resource.GetProperty("scopes_supported").EnumerateArray().Select(x => x.GetString()).Should().Equal("stats:read");
    }

    [Theory]
    [InlineData("/.well-known/anything-else")]
    [InlineData("/.well-known/openid-configuration")]
    [InlineData("/oauth/register")]
    [InlineData("/oauth/revoke")]
    [InlineData("/mcp/extra")]
    public async Task Unmapped_Protocol_Paths_Are_404(string path)
    {
        var response = await _fixture.CreateClient().GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public static IEnumerable<object[]> BadAuthorizeRequests()
    {
        var (_, challenge) = OAuthTestClient.NewPkce();
        yield return new object[] { OAuthTestClient.AuthorizeUrl(challenge).Replace("response_type=code", "response_type=token") };
        yield return new object[] { OAuthTestClient.AuthorizeUrl(challenge, clientId: "not-a-url") };
        yield return new object[] { OAuthTestClient.AuthorizeUrl(challenge, clientId: "http://client.test/metadata.json") };
        yield return new object[] { OAuthTestClient.AuthorizeUrl(challenge, clientId: "https://client.test") };
        yield return new object[] { OAuthTestClient.AuthorizeUrl(challenge, redirectUri: "http://evil.example/callback") };
        yield return new object[] { OAuthTestClient.AuthorizeUrl(challenge, redirectUri: "https://client.test/callback#frag") };
        yield return new object[] { OAuthTestClient.AuthorizeUrl(challenge).Replace("code_challenge_method=S256", "code_challenge_method=plain") };
        yield return new object[] { OAuthTestClient.AuthorizeUrl("short") };
        yield return new object[] { OAuthTestClient.AuthorizeUrl(challenge, scope: "stats:read admin") };
        yield return new object[] { OAuthTestClient.AuthorizeUrl(challenge, resource: "https://other.example") };
    }

    // None of these may redirect anywhere: the redirect URI is not verified yet
    [Theory, MemberData(nameof(BadAuthorizeRequests))]
    public async Task Authorize_Answers_Bad_Requests_With_A_First_Party_Page(string url)
    {
        var response = await _fixture.CreateClient().GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Location.Should().BeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        response.Headers.Contains("Set-Cookie").Should().BeFalse();
    }

    [Fact]
    public async Task Authorize_Rejects_A_Service_Client_Id()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var (clientId, _) = await OAuthTestClient.CreateServiceClient(_fixture.UserA, app.Id);
        var (_, challenge) = OAuthTestClient.NewPkce();

        var response = await _fixture.CreateClient().GetAsync(OAuthTestClient.AuthorizeUrl(challenge, clientId: clientId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Consent_Only_Works_In_The_Browser_That_Started_The_Flow()
    {
        var (_, challenge) = OAuthTestClient.NewPkce();
        var started = await _fixture.CreateClient().GetAsync(OAuthTestClient.AuthorizeUrl(challenge));
        started.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // User B is signed in, but the request cookie lives in another browser
        var response = await _fixture.UserB.Http.GetAsync("/api/_oauth/consent");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var decision = await _fixture.UserB.Http.PostAsJsonAsync("/api/_oauth/consent", new { allow = true, scopes = new[] { "stats:read" } });
        decision.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Consent_Requires_A_Session()
    {
        var response = await _fixture.CreateClient().GetAsync("/api/_oauth/consent");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Consent_Refuses_A_Redirect_Uri_The_Client_Did_Not_Register()
    {
        var (_, challenge) = OAuthTestClient.NewPkce();
        var started = await _fixture.UserA.Http.GetAsync(OAuthTestClient.AuthorizeUrl(challenge, redirectUri: "https://attacker.example/callback"));
        started.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var response = await _fixture.UserA.Http.GetAsync("/api/_oauth/consent");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // The page never got a request id to send back, so a decision posted anyway is
        // refused before the client is even looked at
        var decision = await _fixture.UserA.Http.PostAsJsonAsync("/api/_oauth/consent", new { allow = true, scopes = new[] { "stats:read" } });
        decision.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await decision.Content.ReadAsStringAsync()).Should().NotContain("attacker.example/callback?");
    }

    [Fact]
    public async Task Consent_Rejects_Apps_The_User_Cannot_Read()
    {
        var foreign = await _fixture.UserB.CreateApp(Guid.NewGuid().ToString());
        var (_, challenge) = OAuthTestClient.NewPkce();
        await _fixture.UserA.Http.GetAsync(OAuthTestClient.AuthorizeUrl(challenge));

        var requestId = await OAuthTestClient.ShownRequestId(_fixture.UserA);

        var decision = await _fixture.UserA.Http.PostAsJsonAsync("/api/_oauth/consent", new { requestId, allow = true, scopes = new[] { "stats:read" }, appIds = new[] { foreign.Id } });

        decision.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deny_Returns_Access_Denied_To_The_Verified_Redirect_Uri()
    {
        var (_, challenge) = OAuthTestClient.NewPkce();
        await _fixture.UserA.Http.GetAsync(OAuthTestClient.AuthorizeUrl(challenge));

        var requestId = await OAuthTestClient.ShownRequestId(_fixture.UserA);

        var decision = await _fixture.UserA.Http.PostAsJsonAsync("/api/_oauth/consent", new { requestId, allow = false });
        decision.StatusCode.Should().Be(HttpStatusCode.OK);
        var redirect = (await decision.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("redirectUrl").GetString()!;

        redirect.Should().StartWith("http://127.0.0.1:53124/callback?");
        var query = HttpUtility.ParseQueryString(new Uri(redirect).Query);
        query["error"].Should().Be("access_denied");
        query["state"].Should().Be("xyz");
        query["iss"].Should().Be(OAuthTestClient.Origin);
        query["code"].Should().BeNull();

        // The request is spent
        (await _fixture.UserA.Http.GetAsync("/api/_oauth/consent")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // The page shows one client; another tab starts a request for a different one, which
    // replaces the cookie. The click on the first page must not approve the second request.
    [Fact]
    public async Task A_Decision_Only_Applies_To_The_Request_That_Was_Displayed()
    {
        var (_, shownChallenge) = OAuthTestClient.NewPkce();
        await _fixture.UserA.Http.GetAsync(OAuthTestClient.AuthorizeUrl(shownChallenge));
        var shown = await OAuthTestClient.ShownRequestId(_fixture.UserA);

        var (attackerVerifier, attackerChallenge) = OAuthTestClient.NewPkce();
        var swapped = await _fixture.UserA.Http.GetAsync(OAuthTestClient.AuthorizeUrl(attackerChallenge, clientId: "cursor", redirectUri: "http://localhost:8787/callback"));
        swapped.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var allow = await _fixture.UserA.Http.PostAsJsonAsync("/api/_oauth/consent", new { requestId = shown, allow = true, scopes = new[] { "stats:read" } });
        allow.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await allow.Content.ReadAsStringAsync()).Should().NotContain("code=");

        var deny = await _fixture.UserA.Http.PostAsJsonAsync("/api/_oauth/consent", new { requestId = shown, allow = false });
        deny.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var missing = await _fixture.UserA.Http.PostAsJsonAsync("/api/_oauth/consent", new { allow = true, scopes = new[] { "stats:read" } });
        missing.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Nothing was granted to the client that slipped in
        var grants = await _fixture.UserA.Http.GetFromJsonAsync<JsonElement>("/api/_oauth/grants");
        grants.EnumerateArray().Should().NotContain(g => g.GetProperty("clientName").GetString() == "Cursor" && g.GetProperty("revokedAt").ValueKind == JsonValueKind.Null);

        // After a reload the page shows the new request, and deciding on that one works
        var reloaded = await OAuthTestClient.ShownRequestId(_fixture.UserA);
        reloaded.Should().NotBe(shown);
        (await _fixture.UserA.Http.PostAsJsonAsync("/api/_oauth/consent", new { requestId = reloaded, allow = false })).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Code_Exchange_Issues_Tokens_Once()
    {
        var (verifier, challenge) = OAuthTestClient.NewPkce();
        var code = await OAuthTestClient.Authorize(_fixture.UserA, challenge);

        var tokens = await _oauth.ExchangeCode(code, verifier);
        tokens.AccessToken.Should().NotBeNullOrEmpty();
        tokens.RefreshToken.Should().NotBeNullOrEmpty();
        tokens.Scope.Should().Be("stats:read");
        tokens.ExpiresIn.Should().Be(3600);

        var replay = await _oauth.ExchangeCodeRaw(code, verifier);
        replay.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString().Should().Be("invalid_grant");
    }

    [Theory]
    [InlineData("verifier")]
    [InlineData("client")]
    [InlineData("redirect")]
    [InlineData("resource")]
    public async Task Code_Exchange_Fails_Uniformly_On_Any_Mismatch(string what)
    {
        var (verifier, challenge) = OAuthTestClient.NewPkce();
        var code = await OAuthTestClient.Authorize(_fixture.UserA, challenge);

        var response = what switch
        {
            "verifier" => await _oauth.ExchangeCodeRaw(code, OAuthTestClient.NewPkce().Verifier),
            "client" => await _oauth.ExchangeCodeRaw(code, verifier, clientId: "https://other.test/metadata.json"),
            "redirect" => await _oauth.ExchangeCodeRaw(code, verifier, redirectUri: "http://127.0.0.1:9/callback"),
            _ => await _oauth.ExchangeCodeRaw(code, verifier, resource: OAuthTestClient.Origin + "/mcp"),
        };

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString().Should().Be("invalid_grant");

        // and the failed attempt burned the code
        (await _oauth.ExchangeCodeRaw(code, verifier)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_Rotates_And_A_Quick_Retry_Gets_A_Fresh_Pair()
    {
        var tokens = await _oauth.Connect(_fixture.UserA);

        var first = await OAuthTestClient.ReadTokens(await _oauth.RefreshRaw(tokens.RefreshToken!));
        first.RefreshToken.Should().NotBe(tokens.RefreshToken);

        // The client never saw the answer and retries with the token it still has
        var retryResponse = await _oauth.RefreshRaw(tokens.RefreshToken!);
        retryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retry = await OAuthTestClient.ReadTokens(retryResponse);
        retry.RefreshToken.Should().NotBe(first.RefreshToken);

        // Exactly one live refresh token afterwards: the first replacement is dead
        // without counting as theft, and the newest one works
        (await _oauth.RefreshRaw(first.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _oauth.RefreshRaw(retry.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reusing_A_Refresh_Token_After_Its_Replacement_Was_Used_Revokes_The_Grant()
    {
        var app = await _fixture.UserB.CreateApp(Guid.NewGuid().ToString());
        var tokens = await _oauth.Connect(_fixture.UserB);

        var second = await OAuthTestClient.ReadTokens(await _oauth.RefreshRaw(tokens.RefreshToken!));
        var third = await OAuthTestClient.ReadTokens(await _oauth.RefreshRaw(second.RefreshToken!));

        // The original shows up again although its replacement has been used: theft
        (await _oauth.RefreshRaw(tokens.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await _oauth.RefreshRaw(third.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _oauth.Get($"/api/v1/apps/{app.Id}/metrics", third.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var grants = await _fixture.UserB.Http.GetFromJsonAsync<JsonElement>("/api/_oauth/grants");
        grants.EnumerateArray().Should().Contain(g => g.GetProperty("revokedReason").GetString() == "token_reuse");
    }

    [Fact]
    public async Task Refresh_After_The_Grace_Window_Revokes_The_Grant()
    {
        var tokens = await _oauth.Connect(_fixture.UserB);
        var replacement = await OAuthTestClient.ReadTokens(await _oauth.RefreshRaw(tokens.RefreshToken!));

        using (var cn = _fixture.GetService<IDbContext>().Connection)
            await cn.ExecuteAsync("UPDATE oauth_tokens SET used_at = now() - interval '2 minutes' WHERE used_at IS NOT NULL AND replaced_by_hash IS NOT NULL AND used_at > now() - interval '10 seconds'");

        (await _oauth.RefreshRaw(tokens.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _oauth.RefreshRaw(replacement.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_Requires_The_Client_The_Token_Was_Issued_To()
    {
        var tokens = await _oauth.Connect(_fixture.UserA);

        (await _oauth.RefreshRaw(tokens.RefreshToken!, clientId: "cursor")).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // A wrong client is not reuse: the rightful client can still refresh
        (await _oauth.RefreshRaw(tokens.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reconsent_Replaces_The_Grant_And_Kills_Older_Refresh_Tokens()
    {
        var appOne = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var appTwo = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var before = await _oauth.Connect(_fixture.UserA, appIds: [appOne.Id]);
        var after = await _oauth.Connect(_fixture.UserA, appIds: [appTwo.Id]);

        (await _oauth.RefreshRaw(before.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // The older access token still authenticates, but against what the grant says now
        (await _oauth.Get($"/api/v1/apps/{appOne.Id}/metrics", before.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _oauth.Get($"/api/v1/apps/{appTwo.Id}/metrics", before.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _oauth.Get($"/api/v1/apps/{appTwo.Id}/metrics", after.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        var grants = await _fixture.UserA.Http.GetFromJsonAsync<JsonElement>("/api/_oauth/grants");
        grants.EnumerateArray().Count(g => g.GetProperty("revokedAt").ValueKind == JsonValueKind.Null && g.GetProperty("clientName").GetString() == "Test Client").Should().Be(1);
    }

    [Fact]
    public async Task Client_Credentials_Fail_The_Same_Way_For_Unknown_Client_And_Wrong_Secret()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var (clientId, _) = await OAuthTestClient.CreateServiceClient(_fixture.UserA, app.Id);

        var wrongSecret = await _oauth.ClientCredentialsRaw(clientId, "aptsk_nope");
        var unknownClient = await _oauth.ClientCredentialsRaw("AAAAAAAAAAAAAAAAAAAAAA", "aptsk_nope");

        wrongSecret.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownClient.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await wrongSecret.Content.ReadAsStringAsync()).Should().Be(await unknownClient.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Service_Clients_Are_Owner_Only_And_Limited_By_Tier()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        await _fixture.UserA.ShareApp(app.Id, _fixture.UserB.Email);

        // A shared user can read the app but not manage its clients
        (await _fixture.UserB.Http.GetAsync($"/api/_apps/{app.Id}/clients")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _fixture.UserB.Http.PostAsJsonAsync($"/api/_apps/{app.Id}/clients", new { name = "mine", scopes = new[] { "stats:read" } })).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Free plan: one service client per app
        await OAuthTestClient.CreateServiceClient(_fixture.UserA, app.Id);
        var second = await _fixture.UserA.Http.PostAsJsonAsync($"/api/_apps/{app.Id}/clients", new { name = "second", scopes = new[] { "stats:read" } });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Rotating_A_Service_Client_Secret_Invalidates_The_Old_One()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var (clientId, secret) = await OAuthTestClient.CreateServiceClient(_fixture.UserA, app.Id);

        var rotate = await _fixture.UserA.Http.PostAsync($"/api/_apps/{app.Id}/clients/{clientId}/rotate", null);
        rotate.StatusCode.Should().Be(HttpStatusCode.OK);
        var newSecret = (await rotate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("clientSecret").GetString()!;

        (await _oauth.ClientCredentialsRaw(clientId, secret)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _oauth.ClientCredentialsRaw(clientId, newSecret)).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
