using System.Net;
using System.Text.Json;
using Aptabase.Data;
using Aptabase.Features.Apps;
using Aptabase.IntegrationTests.Clients;
using AwesomeAssertions;
using Dapper;
using Xunit;

namespace Aptabase.IntegrationTests;

[Collection("Integration Tests")]
public class QueryApiLifecycleTests
{
    private readonly IntegrationTestsFixture _fixture;
    private readonly OAuthTestClient _oauth;

    public QueryApiLifecycleTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
        _oauth = new OAuthTestClient(fixture.CreateClient());
    }

    [Fact]
    public async Task Ownership_Transfer_Leaves_Consumed_Requests_With_The_Old_Owner_And_Revokes_Service_Clients()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        var (clientId, secret) = await OAuthTestClient.CreateServiceClient(_fixture.UserA, app.Id);
        var service = await _oauth.ClientCredentials(clientId, secret);

        // B reads the app through a share before the transfer, and owns it afterwards
        await _fixture.UserA.ShareApp(app.Id, _fixture.UserB.Email);
        var reader = await _oauth.Connect(_fixture.UserB);

        (await _oauth.Get($"/api/v1/apps/{app.Id}/metrics?period=7d", service.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _oauth.Get($"/api/v1/apps/{app.Id}/timeseries?period=7d", reader.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        var before = await UsageOf(app.Id);
        before.Should().HaveCount(2);
        before.Should().OnlyContain(r => r.OwnerEmail == _fixture.UserA.Email);

        (await _fixture.UserA.Post($"/api/_apps/{app.Id}/requests", new { targetUserEmail = _fixture.UserB.Email, purpose = AppRequestPurpose.AppOwnership })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _fixture.UserB.Post($"/api/_apps/{app.Id}/requests/accept?purpose=AppOwnership")).StatusCode.Should().Be(HttpStatusCode.OK);

        // The client belonged to the old owner. It is gone, visibly.
        (await _oauth.ClientCredentialsRaw(clientId, secret)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using (var cn = _fixture.GetService<IDbContext>().Connection)
            (await cn.ExecuteScalarAsync<string>("SELECT revoked_reason FROM oauth_grants WHERE id = @clientId", new { clientId })).Should().Be("app_transferred");

        // B's own grant keeps working, and what it spends from now on is B's
        (await _oauth.Get($"/api/v1/apps/{app.Id}/breakdown?dimension=country&period=7d", reader.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await UsageOf(app.Id);
        after.Where(r => r.OwnerEmail == _fixture.UserA.Email).Sum(r => r.Requests).Should().Be(2, "the old owner's month is unchanged");
        after.Where(r => r.OwnerEmail == _fixture.UserB.Email).Sum(r => r.Requests).Should().Be(1, "the new owner starts counting at the transfer");

        // Each owner's view of the app shows only what came out of their own allowance
        var newOwnerView = await _fixture.UserB.GetJson<JsonElement>($"/api/_apps/{app.Id}/api-usage");
        newOwnerView.GetProperty("clients").EnumerateArray().Sum(c => c.GetProperty("requests").GetInt64()).Should().Be(1);
    }

    [Fact]
    public async Task One_Grant_Across_Two_Owners_Is_Metered_Per_Owner()
    {
        var own = await _fixture.UserB.CreateApp(Guid.NewGuid().ToString());
        var shared = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        await _fixture.UserA.ShareApp(shared.Id, _fixture.UserB.Email);
        var tokens = await _oauth.Connect(_fixture.UserB);

        (await _oauth.Get($"/api/v1/apps/{own.Id}/metrics?period=7d", tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _oauth.Get($"/api/v1/apps/{shared.Id}/metrics?period=7d", tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await UsageOf(own.Id)).Should().ContainSingle().Which.OwnerEmail.Should().Be(_fixture.UserB.Email);
        (await UsageOf(shared.Id)).Should().ContainSingle().Which.OwnerEmail.Should().Be(_fixture.UserA.Email);

        // The owner of the shared app sees B's usage of it, and nothing about B's own app
        var ownerWide = await _fixture.UserA.Http.GetStringAsync("/api/_billing/api-usage");
        ownerWide.Should().Contain(shared.Id);
        ownerWide.Should().NotContain(own.Id);
    }

    [Fact]
    public async Task Sign_In_During_An_Authorization_Returns_To_The_Consent_Page()
    {
        var browser = _fixture.CreateClient();
        var (_, challenge) = OAuthTestClient.NewPkce();
        (await browser.GetAsync(OAuthTestClient.AuthorizeUrl(challenge))).StatusCode.Should().Be(HttpStatusCode.Redirect);

        (await browser.PostAsJsonAsync("/api/_auth/signin", new { email = _fixture.UserA.Email })).StatusCode.Should().Be(HttpStatusCode.OK);
        var link = await MailCatcher.GetLinkSentTo(_fixture.UserA.Email);

        var signedIn = await browser.GetAsync(link);
        signedIn.StatusCode.Should().Be(HttpStatusCode.Redirect);
        signedIn.Headers.Location!.ToString().Should().Be($"{OAuthTestClient.Origin}/oauth/consent");
        (await browser.GetAsync("/api/_oauth/consent")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Sign_In_Without_A_Pending_Authorization_Goes_Home()
    {
        var browser = _fixture.CreateClient();
        (await browser.PostAsJsonAsync("/api/_auth/signin", new { email = _fixture.UserB.Email })).StatusCode.Should().Be(HttpStatusCode.OK);
        var link = await MailCatcher.GetLinkSentTo(_fixture.UserB.Email);

        var signedIn = await browser.GetAsync(link);

        signedIn.Headers.Location!.ToString().Should().Be($"{OAuthTestClient.Origin}/");
    }

    [Fact]
    public async Task A_Forged_Consent_Cookie_Is_Ignored()
    {
        var browser = _fixture.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/_auth/signin") { Content = JsonContent.Create(new { email = _fixture.UserB.Email }) };
        (await browser.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.OK);
        var link = await MailCatcher.GetLinkSentTo(_fixture.UserB.Email);

        var signIn = new HttpRequestMessage(HttpMethod.Get, link);
        signIn.Headers.Add("Cookie", "oauth-consent=eyJhbGciOiJIUzI1NiIsInR5cCI6ImNvbnNlbnQrand0In0.eyJjbGllbnRfaWQiOiJ4In0.AAAA");
        var signedIn = await browser.SendAsync(signIn);

        signedIn.Headers.Location!.ToString().Should().Be($"{OAuthTestClient.Origin}/");
    }

    [Fact]
    public async Task New_Grant_Sends_One_Email_And_Reconsent_Sends_None()
    {
        // B has connected the test client in other tests or does so now: either way a
        // second consent for the same client replaces the grant and must stay silent
        await _oauth.Connect(_fixture.UserB);
        var before = await CountMessagesTo(_fixture.UserB.Email, "was connected");
        before.Should().BeGreaterThanOrEqualTo(1);

        await _oauth.Connect(_fixture.UserB);

        (await CountMessagesTo(_fixture.UserB.Email, "was connected")).Should().Be(before);
    }

    private static async Task<int> CountMessagesTo(string email, string subjectPart)
    {
        using var client = new HttpClient();
        var messages = await client.GetFromJsonAsync<JsonElement>("http://localhost:1080/messages");
        return messages.EnumerateArray().Count(m =>
            m.GetProperty("recipients").EnumerateArray().Any(r => r.GetString() == $"<{email}>")
            && (m.GetProperty("subject").GetString() ?? "").Contains(subjectPart));
    }

    private record UsageRow(string OwnerEmail, long Requests);

    private async Task<UsageRow[]> UsageOf(string appId)
    {
        using var cn = _fixture.GetService<IDbContext>().Connection;
        var rows = await cn.QueryAsync<(string, long)>(
            @"SELECT u.email, g.requests FROM api_grant_usage g
              INNER JOIN users u ON u.id = g.owner_id
              WHERE g.app_id = @appId", new { appId });
        return rows.Select(r => new UsageRow(r.Item1, r.Item2)).ToArray();
    }
}
