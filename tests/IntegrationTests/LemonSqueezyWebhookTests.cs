using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Aptabase.Data;
using Aptabase.Features.Authentication;
using AwesomeAssertions;
using Dapper;
using Xunit;

namespace Aptabase.IntegrationTests;

// Events are signed with the (empty) development signing secret.
[Collection("Integration Tests")]
public class LemonSqueezyWebhookTests
{
    private readonly IntegrationTestsFixture _fixture;

    // A development variant from SubscriptionPlan.DevelopmentPlans
    private const int Plan200k = 85183;

    public LemonSqueezyWebhookTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("cancelled", 'O')]
    [InlineData("expired", 'O')]
    [InlineData("paused", 'O')]
    [InlineData("past_due", 'O')]
    [InlineData("unpaid", 'O')]
    [InlineData("active", null)]
    [InlineData("on_trial", null)]
    public async Task Only_a_live_subscription_releases_the_owners_lock(string status, char? expectedLock)
    {
        var me = await _fixture.UserB.Get("/api/_auth/me");
        var user = await me.Content.ReadFromJsonAsync<UserAccount>();
        var subId = Random.Shared.NextInt64(1_000_000, 9_000_000);

        try
        {
            await SetLock(user!.Id, 'O');

            var response = await PostSubscriptionUpdated(subId, status, user.Id);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            (await GetLock(user.Id)).Should().Be(expectedLock);
        }
        finally
        {
            // Leave the shared user as the other tests expect it
            using var cn = _fixture.GetService<IDbContext>().Connection;
            await cn.ExecuteAsync("DELETE FROM subscriptions WHERE id = @subId", new { subId });
            await SetLock(user!.Id, null);
        }
    }

    private async Task SetLock(string userId, char? lockReason)
    {
        using var cn = _fixture.GetService<IDbContext>().Connection;
        await cn.ExecuteAsync("UPDATE users SET lock_reason = @lockReason WHERE id = @userId", new { userId, lockReason });
    }

    private async Task<char?> GetLock(string userId)
    {
        using var cn = _fixture.GetService<IDbContext>().Connection;
        return await cn.QueryFirstOrDefaultAsync<char?>("SELECT lock_reason FROM users WHERE id = @userId", new { userId });
    }

    private async Task<HttpResponseMessage> PostSubscriptionUpdated(long subId, string status, string userId)
    {
        var body = "{\"meta\":{\"event_name\":\"subscription_updated\",\"test_mode\":true,\"custom_data\":{\"user_id\":\"" + userId + "\",\"region\":\"DEV\"}},"
            + "\"data\":{\"id\":\"" + subId + "\",\"type\":\"subscriptions\",\"attributes\":{\"store_id\":30508,\"customer_id\":1234,\"product_id\":42,\"variant_id\":" + Plan200k
            + ",\"status\":\"" + status + "\",\"ends_at\":null,\"urls\":{\"customer_portal\":\"https://example.test/portal\"}}}}";

        var content = new StringContent(body, Encoding.UTF8, "application/json");
        content.Headers.Add("X-Signature", Sign(body));
        content.Headers.Add("X-Event-Name", "subscription_updated");
        return await _fixture.CreateClient().PostAsync("/webhook/lemonsqueezy", content);
    }

    private static string Sign(string body)
    {
        using var hmac = new HMACSHA256(Array.Empty<byte>());
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    }
}
