using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Aptabase.IntegrationTests.Clients;
using AwesomeAssertions;
using Xunit;

namespace Aptabase.IntegrationTests;

[Collection("Integration Tests")]
public class McpTests
{
    private readonly IntegrationTestsFixture _fixture;
    private readonly OAuthTestClient _oauth;
    private readonly HttpClient _http;

    public McpTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
        _http = fixture.CreateClient();
        _oauth = new OAuthTestClient(fixture.CreateClient());
    }

    private async Task<HttpResponseMessage> Rpc(string token, string method, object? parameters = null, int id = 1)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters ?? new { } }), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        return await _http.SendAsync(request);
    }

    // The transport may answer as plain JSON or as a one-message event stream
    private static async Task<JsonElement> ReadResult(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);

        if (response.Content.Headers.ContentType?.MediaType == "text/event-stream")
        {
            var data = text.Split('\n').Last(line => line.StartsWith("data:"));
            text = data["data:".Length..];
        }

        var message = JsonDocument.Parse(text).RootElement;
        message.TryGetProperty("error", out var error).Should().BeFalse(error.ToString());
        return message.GetProperty("result");
    }

    private async Task<JsonElement> CallTool(string token, string name, object arguments)
    {
        var result = await ReadResult(await Rpc(token, "tools/call", new { name, arguments }));
        return result;
    }

    private static JsonElement ToolJson(JsonElement result)
        => JsonDocument.Parse(result.GetProperty("content")[0].GetProperty("text").GetString()!).RootElement;

    private static object Initialize => new
    {
        protocolVersion = "2025-06-18",
        capabilities = new { },
        clientInfo = new { name = "integration-tests", version = "1.0" },
    };

    [Fact]
    public async Task Initialize_And_List_The_Eight_Tools()
    {
        var tokens = await _oauth.Connect(_fixture.UserA);

        var init = await ReadResult(await Rpc(tokens.AccessToken, "initialize", Initialize));
        init.GetProperty("serverInfo").GetProperty("name").GetString().Should().Be("aptabase");

        var tools = await ReadResult(await Rpc(tokens.AccessToken, "tools/list"));
        var names = tools.GetProperty("tools").EnumerateArray().Select(t => t.GetProperty("name").GetString()).ToArray();
        names.Should().BeEquivalentTo(["list_apps", "get_metrics", "get_timeseries", "get_breakdown", "get_event_props", "list_sessions", "get_session", "get_usage"]);

        var metrics = tools.GetProperty("tools").EnumerateArray().First(t => t.GetProperty("name").GetString() == "get_metrics");
        metrics.GetProperty("inputSchema").GetProperty("required").EnumerateArray().Select(x => x.GetString()).Should().Equal("appId");
        metrics.GetProperty("inputSchema").GetProperty("properties").TryGetProperty("period", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Tools_Go_Through_The_Same_Access_Checks_As_Rest()
    {
        var mine = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var foreign = await _fixture.UserB.CreateApp(Guid.NewGuid().ToString());
        var tokens = await _oauth.Connect(_fixture.UserA);

        var apps = ToolJson(await CallTool(tokens.AccessToken, "list_apps", new { }));
        var ids = apps.GetProperty("data").EnumerateArray().Select(a => a.GetProperty("id").GetString()).ToArray();
        ids.Should().Contain(mine.Id);
        ids.Should().NotContain(foreign.Id);

        var ok = await CallTool(tokens.AccessToken, "get_metrics", new { appId = mine.Id, period = "7d" });
        (ok.TryGetProperty("isError", out var isError) && isError.GetBoolean()).Should().BeFalse();
        ToolJson(ok).GetProperty("data").GetProperty("sessions").GetInt32().Should().Be(0);

        var denied = await CallTool(tokens.AccessToken, "get_metrics", new { appId = foreign.Id });
        denied.GetProperty("isError").GetBoolean().Should().BeTrue();
        ToolJson(denied).GetProperty("error").GetString().Should().Be("forbidden");

        var invalid = await CallTool(tokens.AccessToken, "get_breakdown", new { appId = mine.Id, dimension = "user_id" });
        invalid.GetProperty("isError").GetBoolean().Should().BeTrue();

        var breakdown = await CallTool(tokens.AccessToken, "get_breakdown", new { appId = mine.Id, dimension = "country", period = "30d" });
        ToolJson(breakdown).GetProperty("data").GetArrayLength().Should().Be(0);

        var usage = ToolJson(await CallTool(tokens.AccessToken, "get_usage", new { appId = mine.Id }));
        usage.GetProperty("data").GetProperty("api").GetProperty("allowance").GetInt64().Should().Be(100);
    }

    [Fact]
    public async Task Session_Tools_Ask_For_Step_Up_Then_Work_After_Reconsent()
    {
        var app = await _fixture.UserB.CreateApp(Guid.NewGuid().ToString());
        var tokens = await _oauth.Connect(_fixture.UserB);

        var response = await Rpc(tokens.AccessToken, "tools/call", new { name = "list_sessions", arguments = new { appId = app.Id, live = true } });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var challenge = response.Headers.WwwAuthenticate.ToString();
        challenge.Should().Contain("error=\"insufficient_scope\"");
        challenge.Should().Contain("scope=\"sessions:read\"");

        // The client re-authorizes asking for the missing scope. The consent page offers
        // what the grant already had plus what is asked now.
        var upgraded = await _oauth.Connect(_fixture.UserB, scope: "stats:read sessions:read");
        upgraded.Scope.Should().Be("stats:read sessions:read");

        var sessions = await CallTool(upgraded.AccessToken, "list_sessions", new { appId = app.Id, live = true });
        ToolJson(sessions).GetProperty("data").GetArrayLength().Should().Be(0);

        // Same grant row, so the token issued before the step-up has the scope now too
        var older = await CallTool(tokens.AccessToken, "list_sessions", new { appId = app.Id, live = true });
        older.GetProperty("content").GetArrayLength().Should().Be(1);

        // Put the grant back so other tests see a stats-only connection
        await _oauth.Connect(_fixture.UserB);
    }

    [Fact]
    public async Task Preflight_From_A_Browser_Client_Is_Allowed_And_The_Challenge_Is_Readable()
    {
        var preflight = new HttpRequestMessage(HttpMethod.Options, "/mcp");
        preflight.Headers.Add("Origin", "https://inspector.example");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "authorization,content-type,mcp-protocol-version");
        var allowed = await _http.SendAsync(preflight);

        allowed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        allowed.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal("*");

        var unauthenticated = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
        unauthenticated.Headers.Add("Origin", "https://inspector.example");
        var challenged = await _http.SendAsync(unauthenticated);

        challenged.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        challenged.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal("*");
        string.Join(",", challenged.Headers.GetValues("Access-Control-Expose-Headers")).Should().Contain("WWW-Authenticate");

        // The cookie-authenticated consent endpoint is on no permissive policy
        var consent = new HttpRequestMessage(HttpMethod.Get, "/api/_oauth/consent");
        consent.Headers.Add("Origin", "https://inspector.example");
        (await _http.SendAsync(consent)).Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
