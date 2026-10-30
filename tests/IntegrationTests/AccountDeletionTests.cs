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

        var deleted = await leaver.DeleteAccount();
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);

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

    private record ShareRow(string Email);
}
