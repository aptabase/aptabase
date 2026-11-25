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
    private const int Plan2M = 85185;

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

            var response = await PostEvent("subscription_updated", subId, Plan200k, status, CustomData(user.Id));
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

    // A plan switch in the customer portal arrives as subscription_plan_changed, and not
    // necessarily with the custom data the checkout attached.
    [Theory]
    [InlineData("")]
    [InlineData(",\"custom_data\":[]")]
    [InlineData(",\"custom_data\":null")]
    public async Task Plan_change_without_custom_data_updates_the_stored_plan(string customData)
    {
        var me = await _fixture.UserB.Get("/api/_auth/me");
        var user = await me.Content.ReadFromJsonAsync<UserAccount>();
        var subId = Random.Shared.NextInt64(1_000_000, 9_000_000);

        try
        {
            (await PostEvent("subscription_created", subId, Plan200k, "active", CustomData(user!.Id))).StatusCode.Should().Be(HttpStatusCode.OK);
            (await StoredVariant(subId)).Should().Be(Plan200k);

            (await PostEvent("subscription_plan_changed", subId, Plan2M, "active", customData)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await StoredVariant(subId)).Should().Be(Plan2M);

            // A subscription this region does not store is acknowledged and ignored
            (await PostEvent("subscription_plan_changed", subId + 1, Plan2M, "active", customData)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await StoredVariant(subId + 1)).Should().BeNull();

            // Another region's event is still ignored
            (await PostEvent("subscription_plan_changed", subId, Plan200k, "active", CustomData(user.Id, "EU"))).StatusCode.Should().Be(HttpStatusCode.OK);
            (await StoredVariant(subId)).Should().Be(Plan2M);
        }
        finally
        {
            using var cn = _fixture.GetService<IDbContext>().Connection;
            await cn.ExecuteAsync("DELETE FROM subscriptions WHERE id IN (@subId, @other)", new { subId, other = subId + 1 });
        }
    }

    [Fact]
    public async Task Other_events_are_still_rejected()
    {
        (await PostEvent("order_created", 1, Plan200k, "active", "")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PostEvent("order_created", 1, Plan200k, "active", CustomData("someone"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<long?> StoredVariant(long subId)
    {
        using var cn = _fixture.GetService<IDbContext>().Connection;
        return await cn.QueryFirstOrDefaultAsync<long?>("SELECT variant_id FROM subscriptions WHERE id = @subId", new { subId });
    }

    private static string CustomData(string userId, string region = "DEV")
        => ",\"custom_data\":{\"user_id\":\"" + userId + "\",\"region\":\"" + region + "\"}";

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

    private async Task<HttpResponseMessage> PostEvent(string eventName, long subId, int variantId, string status, string customData)
    {
        var body = "{\"meta\":{\"event_name\":\"" + eventName + "\",\"test_mode\":true" + customData + "},"
            + "\"data\":{\"id\":\"" + subId + "\",\"type\":\"subscriptions\",\"attributes\":{\"store_id\":30508,\"customer_id\":1234,\"product_id\":42,\"variant_id\":" + variantId
            + ",\"status\":\"" + status + "\",\"ends_at\":null,\"urls\":{\"customer_portal\":\"https://example.test/portal\"}}}}";

        var content = new StringContent(body, Encoding.UTF8, "application/json");
        content.Headers.Add("X-Signature", Sign(body));
        content.Headers.Add("X-Event-Name", eventName);
        return await _fixture.CreateClient().PostAsync("/webhook/lemonsqueezy", content);
    }

    private static string Sign(string body)
    {
        using var hmac = new HMACSHA256(Array.Empty<byte>());
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    }
}
