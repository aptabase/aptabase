using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using AwesomeAssertions;
using Microsoft.AspNetCore.WebUtilities;

namespace Aptabase.IntegrationTests.Clients;

public record TokenSet(string AccessToken, string? RefreshToken, string Scope, int ExpiresIn);

// A scripted OAuth client: what an MCP client or a script does against the authorization
// server, step by step, so that each step can also be done wrong on purpose.
public class OAuthTestClient
{
    public const string Origin = "http://localhost:3000";

    private readonly HttpClient _anonymous;

    public OAuthTestClient(HttpClient anonymous)
    {
        _anonymous = anonymous;
    }

    public static (string Verifier, string Challenge) NewPkce()
    {
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return (verifier, challenge);
    }

    public static string AuthorizeUrl(string challenge, string scope = "stats:read", string? clientId = null, string? redirectUri = null, string? resource = null, string state = "xyz")
    {
        var query = new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = clientId ?? FakeClientMetadataFetcher.ClientId,
            ["redirect_uri"] = redirectUri ?? "http://127.0.0.1:53124/callback",
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["scope"] = scope,
            ["resource"] = resource ?? Origin + "/",
            ["state"] = state,
        };
        return QueryHelpers.AddQueryString("/oauth/authorize", query);
    }

    // Runs authorize and consent in the user's browser session and returns the code
    public static async Task<string> Authorize(AccountClient user, string challenge, string scope = "stats:read", string[]? appIds = null, string? resource = null, string? redirectUri = null)
    {
        var redirect = await ApproveConsent(user, challenge, scope, appIds, resource, redirectUri);
        var query = HttpUtility.ParseQueryString(new Uri(redirect).Query);
        query["iss"].Should().Be(Origin);
        query["state"].Should().Be("xyz");
        return query["code"] ?? throw new Exception($"No code in {redirect}");
    }

    public static async Task<string> ApproveConsent(AccountClient user, string challenge, string scope = "stats:read", string[]? appIds = null, string? resource = null, string? redirectUri = null)
    {
        var response = await user.Http.GetAsync(AuthorizeUrl(challenge, scope, resource: resource, redirectUri: redirectUri));
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be($"{Origin}/oauth/consent");

        var consent = await user.Http.GetAsync("/api/_oauth/consent");
        consent.StatusCode.Should().Be(HttpStatusCode.OK);

        var decision = await user.Http.PostAsJsonAsync("/api/_oauth/consent", new { allow = true, scopes = scope.Split(' '), appIds });
        decision.StatusCode.Should().Be(HttpStatusCode.OK, await decision.Content.ReadAsStringAsync());
        var body = await decision.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("redirectUrl").GetString()!;
    }

    public Task<HttpResponseMessage> PostToken(Dictionary<string, string> form)
        => _anonymous.PostAsync("/oauth/token", new FormUrlEncodedContent(form));

    public Task<HttpResponseMessage> ExchangeCodeRaw(string code, string verifier, string? clientId = null, string? redirectUri = null, string? resource = null)
        => PostToken(new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["code_verifier"] = verifier,
            ["client_id"] = clientId ?? FakeClientMetadataFetcher.ClientId,
            ["redirect_uri"] = redirectUri ?? "http://127.0.0.1:53124/callback",
            ["resource"] = resource ?? Origin + "/",
        });

    public async Task<TokenSet> ExchangeCode(string code, string verifier, string? resource = null)
    {
        var response = await ExchangeCodeRaw(code, verifier, resource: resource);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await ReadTokens(response);
    }

    public Task<HttpResponseMessage> RefreshRaw(string refreshToken, string? clientId = null)
        => PostToken(new()
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId ?? FakeClientMetadataFetcher.ClientId,
        });

    public Task<HttpResponseMessage> ClientCredentialsRaw(string clientId, string clientSecret, string? resource = null)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
        };
        if (resource is not null) form["resource"] = resource;
        return PostToken(form);
    }

    public async Task<TokenSet> ClientCredentials(string clientId, string clientSecret, string? resource = null)
    {
        var response = await ClientCredentialsRaw(clientId, clientSecret, resource);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await ReadTokens(response);
    }

    public static async Task<TokenSet> ReadTokens(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new TokenSet(
            body.GetProperty("access_token").GetString()!,
            body.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : null,
            body.GetProperty("scope").GetString()!,
            body.GetProperty("expires_in").GetInt32());
    }

    // The whole interactive flow for a user: authorize, consent, exchange
    public async Task<TokenSet> Connect(AccountClient user, string scope = "stats:read", string[]? appIds = null, string? resource = null)
    {
        var (verifier, challenge) = NewPkce();
        var code = await Authorize(user, challenge, scope, appIds, resource);
        return await ExchangeCode(code, verifier, resource);
    }

    public static async Task<(string ClientId, string ClientSecret)> CreateServiceClient(AccountClient owner, string appId, string scopes = "stats:read")
    {
        var response = await owner.Http.PostAsJsonAsync($"/api/_apps/{appId}/clients", new { name = "CI job", scopes = scopes.Split(' ') });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("clientId").GetString()!, body.GetProperty("clientSecret").GetString()!);
    }

    public async Task<HttpResponseMessage> Get(string url, string? accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _anonymous.SendAsync(request);
    }

    public async Task<JsonElement> GetJson(string url, string accessToken)
    {
        var response = await Get(url, accessToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
