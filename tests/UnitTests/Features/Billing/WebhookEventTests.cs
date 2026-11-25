using System.Text.Json;
using Xunit;
using Aptabase.Features.Billing.LemonSqueezy;
using AwesomeAssertions;

namespace Aptabase.UnitTests.Features.Billing;

public class WebhookEventTests
{
    private static WebhookEvent Parse(string json) =>
        JsonSerializer.Deserialize<WebhookEvent>(json, LemonSqueezyClient.JsonSettings)!;

    [Fact]
    public void Reads_custom_data_from_checkout()
    {
        var ev = Parse("""{"meta":{"event_name":"subscription_created","custom_data":{"user_id":"abc","region":"US"}},"data":{"id":"1","type":"subscriptions","attributes":{}}}""");

        ev.Meta.EventName.Should().Be("subscription_created");
        ev.Meta.CustomData.Should().Equal(new Dictionary<string, string> { ["user_id"] = "abc", ["region"] = "US" });
    }

    [Fact]
    public void Reads_numeric_and_boolean_custom_values_as_text()
    {
        var ev = Parse("""{"meta":{"event_name":"subscription_updated","custom_data":{"user_id":42,"flag":true,"nested":{"a":1}}},"data":{"id":"1","type":"subscriptions","attributes":{}}}""");

        ev.Meta.CustomData.Should().Equal(new Dictionary<string, string> { ["user_id"] = "42", ["flag"] = "true" });
    }

    // Regression: LemonSqueezy serializes empty custom data as a PHP-style "[]", which used to make the
    // whole event fail to parse and be answered with 400 instead of being attributed by subscription id.
    [Theory]
    [InlineData("""{"meta":{"event_name":"subscription_plan_changed","custom_data":[]},"data":{"id":"1","type":"subscriptions","attributes":{}}}""")]
    [InlineData("""{"meta":{"event_name":"subscription_plan_changed","custom_data":null},"data":{"id":"1","type":"subscriptions","attributes":{}}}""")]
    [InlineData("""{"meta":{"event_name":"subscription_plan_changed"},"data":{"id":"1","type":"subscriptions","attributes":{}}}""")]
    public void Missing_or_empty_custom_data_is_an_empty_dictionary(string json)
    {
        var ev = Parse(json);

        ev.Meta.EventName.Should().Be("subscription_plan_changed");
        ev.Meta.CustomData.Should().BeEmpty();
    }

    [Fact]
    public void Subscription_attributes_parse_from_the_event_data()
    {
        var ev = Parse("""{"meta":{"event_name":"subscription_plan_changed"},"data":{"id":"77","type":"subscriptions","attributes":{"store_id":1,"customer_id":5,"product_id":9,"variant_id":85185,"status":"active","ends_at":null,"urls":{"customer_portal":"https://x"}}}}""");

        var attrs = JsonSerializer.Deserialize<SubscriptionAttributes>(ev.Data.Attributes, LemonSqueezyClient.JsonSettings)!;
        attrs.VariantID.Should().Be(85185);
        attrs.CustomerID.Should().Be(5);
        attrs.Status.Should().Be("active");
        attrs.EndsAt.Should().BeNull();
    }
}
