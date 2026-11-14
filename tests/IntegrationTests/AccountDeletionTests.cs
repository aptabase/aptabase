using System.Net;
using Xunit;
using AwesomeAssertions;
using Aptabase.Features.Apps;
using Aptabase.IntegrationTests.Clients;

namespace Aptabase.IntegrationTests;

[Collection("Integration Tests")]
public class AccountDeletionTests
{
    private readonly IntegrationTestsFixture _fixture;

    public AccountDeletionTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Deleting_An_Account_Removes_Its_Apps_And_Shares()
    {
        var leaver = new AccountClient(_fixture.CreateClient());
        await leaver.CreateAccount("Robb Stark", $"robb.stark.{Guid.NewGuid()}@got.com");

        var leaverApp = await leaver.CreateApp(Guid.NewGuid().ToString());
        await leaver.ShareApp(leaverApp.Id, _fixture.UserA.Email);

        var appA = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());
        await _fixture.UserA.ShareApp(appA.Id, leaver.Email);

        var transfer = await _fixture.UserA.Post($"/api/_apps/{appA.Id}/requests", new { targetUserEmail = leaver.Email, purpose = AppRequestPurpose.AppOwnership });
        transfer.StatusCode.Should().Be(HttpStatusCode.OK);

        // The leaver has also used the query API: a connected client, a service client,
        // metered requests, and a run of denials. All of it is keyed to the user.
        var oauth = new OAuthTestClient(_fixture.CreateClient());
        var userTokens = await oauth.Connect(leaver);
        var (serviceId, serviceSecret) = await OAuthTestClient.CreateServiceClient(leaver, leaverApp.Id);
        var serviceTokens = await oauth.ClientCredentials(serviceId, serviceSecret);
        (await oauth.Get($"/api/v1/apps/{leaverApp.Id}/metrics", userTokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await oauth.Get($"/api/v1/apps/{leaverApp.Id}/timeseries", serviceTokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        await Denials_Slow_The_Subject_Down_And_A_Runaway_User_Grant_Is_Revoked(oauth, leaver, leaverApp.Id, userTokens, serviceId, serviceSecret);

        var deleted = await leaver.DeleteAccount();
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var cn = _fixture.GetService<Aptabase.Data.IDbContext>().Connection)
        {
            var left = await Dapper.SqlMapper.ExecuteScalarAsync<long>(cn,
                @"SELECT (SELECT COUNT(*) FROM oauth_grants WHERE id = @serviceId)
                       + (SELECT COUNT(*) FROM oauth_tokens WHERE grant_id = @serviceId)
                       + (SELECT COUNT(*) FROM api_grant_usage WHERE app_id = @appId)",
                new { serviceId, appId = leaverApp.Id });
            left.Should().Be(0);
        }
        (await oauth.ClientCredentialsRaw(serviceId, serviceSecret)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var response = await _fixture.UserA.Get($"/api/_stats/metrics?buildMode=release&appId={leaverApp.Id}&startDate=2026-01-01&endDate=2026-02-01");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var shares = await _fixture.UserA.GetJson<ShareRow[]>($"/api/_apps/{appA.Id}/shares");
        shares.Should().NotContain(s => s.Email == leaver.Email);

        var pending = await _fixture.UserA.Get($"/api/_apps/{appA.Id}/requests?purpose=AppOwnership");
        var pendingBody = await pending.Content.ReadAsStringAsync();
        pendingBody.Should().BeOneOf("", "null");

        var signIn = await leaver.Post("/api/_auth/signin", new { email = leaver.Email });
        signIn.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Runs on the account that is about to be deleted, because slowing a subject down
    // lasts ten minutes and must not spill over to the accounts other tests share.
    private async Task Denials_Slow_The_Subject_Down_And_A_Runaway_User_Grant_Is_Revoked(
        OAuthTestClient oauth, AccountClient leaver, string ownAppId, TokenSet userTokens, string serviceId, string serviceSecret)
    {
        var foreign = await _fixture.UserB.CreateApp(Guid.NewGuid().ToString());

        // A stale or guessed app id is a 403 and costs nothing from the allowance
        for (var i = 0; i < 19; i++)
            (await oauth.Get($"/api/v1/apps/{foreign.Id}/metrics", userTokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // The twentieth trips the bucket for the whole subject: the next request is slowed
        // down even for an app the user owns, and on the user's other grant too
        (await oauth.Get($"/api/v1/apps/{foreign.Id}/metrics", userTokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var slowed = await oauth.Get($"/api/v1/apps/{ownAppId}/metrics?period=7d", userTokens.AccessToken);
        slowed.StatusCode.Should().Be((HttpStatusCode)429);
        slowed.Headers.RetryAfter.Should().NotBeNull();
        var serviceTokens = await oauth.ClientCredentials(serviceId, serviceSecret);
        (await oauth.Get($"/api/v1/apps/{ownAppId}/metrics?period=7d", serviceTokens.AccessToken)).StatusCode.Should().Be((HttpStatusCode)429);

        // A client that keeps hammering through the slow-down ends up revoked. The service
        // client never is, however many denials it has collected.
        using (var cn = _fixture.GetService<Aptabase.Data.IDbContext>().Connection)
            await Dapper.SqlMapper.ExecuteAsync(cn, "UPDATE oauth_grants SET denied_count = 200, denied_at = now() WHERE id = @serviceId", new { serviceId });

        (await oauth.Get($"/api/v1/apps/{ownAppId}/metrics?period=7d", serviceTokens.AccessToken)).StatusCode.Should().Be((HttpStatusCode)429);
        (await oauth.Get($"/api/v1/apps/{ownAppId}/metrics?period=7d", userTokens.AccessToken)).StatusCode.Should().Be((HttpStatusCode)429);
        (await oauth.Get($"/api/v1/apps/{ownAppId}/metrics?period=7d", userTokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var grants = await leaver.GetJson<System.Text.Json.JsonElement>("/api/_oauth/grants");
        grants.EnumerateArray().Should().Contain(g => g.GetProperty("revokedReason").GetString() == "access_failures");
        (await oauth.ClientCredentialsRaw(serviceId, serviceSecret)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record ShareRow(string Email);
}
