using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aptabase.Features.Authentication;
using AwesomeAssertions;
using Xunit;

namespace Aptabase.IntegrationTests;

// Exercises the LemonSqueezy webhook end to end against the real database: events are signed with the
// (empty) development signing secret and the effect is observed through /api/_billing.
[Collection("Integration Tests")]
public class LemonSqueezyWebhookTests
{
    private readonly IntegrationTestsFixture _fixture;

    // Development variants from SubscriptionPlan.DevelopmentPlans
    private const int Plan200k = 85183;
    private const int Plan2M = 85185;
    private const int Plan10M = 85188;

    public LemonSqueezyWebhookTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Plan_change_without_custom_data_updates_the_stored_plan()
    {
        var me = await _fixture.UserA.Get("/api/_auth/me");
        var user = await me.Content.ReadFromJsonAsync<UserAccount>();
        user.Should().NotBeNull();

        var subId = Random.Shared.NextInt64(1_000_000, 9_000_000);
        var ownerCustomData = new { user_id = user!.Id, region = "DEV" };

        try
        {
            // The checkout attaches user_id and region, which is how the subscription gets attributed.
            var created = await PostEvent("subscription_created", subId, Plan200k, "active", ownerCustomData);
            created.StatusCode.Should().Be(HttpStatusCode.OK);
            (await CurrentPlanName()).Should().Be("200k Plan");

            // Regression: a portal plan switch arrives as subscription_plan_changed, which used to be rejected
            // as an unknown event, so the stored variant (and quota) stayed at the old plan.
            var switched = await PostEvent("subscription_plan_changed", subId, Plan2M, "active", customData: null);
            switched.StatusCode.Should().Be(HttpStatusCode.OK);
            (await CurrentPlanName()).Should().Be("2M Plan");

            // subscription_updated with custom_data keeps working as before.
            var updated = await PostEvent("subscription_updated", subId, Plan10M, "active", ownerCustomData);
            updated.StatusCode.Should().Be(HttpStatusCode.OK);
            (await CurrentPlanName()).Should().Be("10M Plan");

            // Another region's subscription is acknowledged and ignored.
            var otherRegion = await PostEvent("subscription_updated", subId, Plan200k, "active", new { user_id = user.Id, region = "EU" });
            otherRegion.StatusCode.Should().Be(HttpStatusCode.OK);
            (await CurrentPlanName()).Should().Be("10M Plan");

            // An event without custom_data about a subscription this region does not store is ignored, not failed.
            var unknown = await PostEvent("subscription_plan_changed", subId + 1, Plan2M, "active", customData: null);
            unknown.StatusCode.Should().Be(HttpStatusCode.OK);

            // Payment events carry an invoice, not a subscription, and must be acknowledged without changes.
            var payment = await PostRaw("subscription_payment_success", """{"id":"555","type":"subscription-invoices","attributes":{"subscription_id":1,"status":"paid"}}""", ownerCustomData);
            payment.StatusCode.Should().Be(HttpStatusCode.OK);
            (await CurrentPlanName()).Should().Be("10M Plan");

            // A subscription LemonSqueezy no longer bills (expired) is removed and the user is back on the free tier.
            var expired = await PostEvent("subscription_expired", subId, Plan10M, "expired", ownerCustomData);
            expired.StatusCode.Should().Be(HttpStatusCode.OK);
            (await CurrentPlanName()).Should().StartWith("Free");
        }
        finally
        {
            // Leave the shared user as the other tests expect it, whatever happened above.
            await PostEvent("subscription_expired", subId, Plan10M, "expired", ownerCustomData);
        }
    }

    [Fact]
    public async Task Manual_sync_reports_when_there_is_nothing_to_sync()
    {
        var response = await _fixture.UserB.Post("/api/_billing/sync");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("outcome").GetString().Should().Be("no_subscription");
        doc.RootElement.GetProperty("unlocked").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Unsigned_events_are_rejected()
    {
        var client = _fixture.CreateClient();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        content.Headers.Add("X-Signature", "deadbeef");
        var response = await client.PostAsync("/webhook/lemonsqueezy", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> CurrentPlanName()
    {
        var response = await _fixture.UserA.Get("/api/_billing");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("plan").GetProperty("name").GetString() ?? "";
    }

    private Task<HttpResponseMessage> PostEvent(string eventName, long subId, int variantId, string status, object? customData)
    {
        var data = "{\"id\":\"" + subId + "\",\"type\":\"subscriptions\",\"attributes\":{\"store_id\":30508,\"customer_id\":1234,\"product_id\":42,\"variant_id\":" + variantId
            + ",\"status\":\"" + status + "\",\"ends_at\":null,\"urls\":{\"customer_portal\":\"https://example.test/portal\"}}}";
        return PostRaw(eventName, data, customData);
    }

    private async Task<HttpResponseMessage> PostRaw(string eventName, string dataJson, object? customData)
    {
        var meta = "{\"event_name\":\"" + eventName + "\",\"test_mode\":true"
            + (customData is null ? "" : ",\"custom_data\":" + JsonSerializer.Serialize(customData))
            + "}";
        var body = "{\"meta\":" + meta + ",\"data\":" + dataJson + "}";

        var client = _fixture.CreateClient();
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        content.Headers.Add("X-Signature", Sign(body));
        content.Headers.Add("X-Event-Name", eventName);
        return await client.PostAsync("/webhook/lemonsqueezy", content);
    }

    // Integration tests run without LEMONSQUEEZY_SIGNING_SECRET, so the key is empty.
    private static string Sign(string body)
    {
        using var hmac = new HMACSHA256(Array.Empty<byte>());
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    }
}
