using System.Text.Json;
using Xunit;
using Aptabase.Features.Billing;
using Aptabase.Features.Billing.LemonSqueezy;
using AwesomeAssertions;

namespace Aptabase.UnitTests.Features.Billing;

public class SubscriptionReconciliationTests
{
    private static StoredSubscription Stored(long variant = 85183, string status = "active", DateTime? endsAt = null) =>
        new() { Id = 1, OwnerId = "u", VariantId = variant, Status = status, EndsAt = endsAt };

    private static SubscriptionAttributes Fetched(int variant = 85183, string status = "active", DateTime? endsAt = null) =>
        new() { VariantID = variant, Status = status, EndsAt = endsAt };

    [Fact]
    public void Identical_subscriptions_do_not_differ()
    {
        Stored().Differs(Fetched()).Should().BeFalse();
    }

    [Fact]
    public void A_plan_change_differs()
    {
        Stored(variant: 85183).Differs(Fetched(variant: 85188)).Should().BeTrue();
    }

    [Fact]
    public void A_status_change_differs()
    {
        Stored(status: "active").Differs(Fetched(status: "cancelled")).Should().BeTrue();
    }

    [Fact]
    public void An_end_date_change_differs_and_equal_instants_in_other_kinds_do_not()
    {
        var utc = new DateTime(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);
        Stored(endsAt: utc).Differs(Fetched(endsAt: utc.ToLocalTime())).Should().BeFalse();
        Stored(endsAt: utc).Differs(Fetched(endsAt: utc.AddDays(1))).Should().BeTrue();
        Stored(endsAt: null).Differs(Fetched(endsAt: utc)).Should().BeTrue();
    }

    [Fact]
    public void Paged_list_reads_camel_case_page_metadata_and_snake_case_attributes()
    {
        var json = """
        {
          "meta": {"page": {"currentPage": 2, "from": 101, "lastPage": 3, "perPage": 100, "to": 200, "total": 250}},
          "jsonapi": {"version": "1.0"},
          "links": {"first": "x", "last": "y"},
          "data": [
            {"type": "subscriptions", "id": "900001", "attributes": {"store_id": 30508, "customer_id": 7, "product_id": 1, "variant_id": 85185, "status": "active", "ends_at": null, "urls": {"customer_portal": "u"}}}
          ]
        }
        """;

        var page = JsonSerializer.Deserialize<PagedList<SubscriptionAttributes>>(json, LemonSqueezyClient.JsonSettings)!;

        page.Meta.Page.CurrentPage.Should().Be(2);
        page.Meta.Page.LastPage.Should().Be(3);
        page.Meta.Page.Total.Should().Be(250);
        var item = page.Data.Single();
        item.Id.Should().Be("900001");
        item.Attributes.VariantID.Should().Be(85185);
        item.Attributes.Status.Should().Be("active");
    }
}
