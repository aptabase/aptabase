using Aptabase.Features;
using Aptabase.Features.Api;
using Aptabase.Features.Stats;
using AwesomeAssertions;
using Xunit;

namespace Aptabase.UnitTests.Features.Api;

public class ApiRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 10, 7, 33, DateTimeKind.Utc);

    // Development variant ids from Subscription.cs: 85183 = 200k, 85187 = 5M, 85192 = 30M
    [Theory]
    [InlineData("active", 85183L, "Starter")]
    [InlineData("on_trial", 85187L, "Growth")]
    [InlineData("active", 85192L, "Scale")]
    [InlineData("active", 103481L, "Scale")]
    [InlineData("paused", 85192L, "Free")]
    [InlineData("past_due", 85192L, "Free")]
    [InlineData("unpaid", 85192L, "Free")]
    [InlineData("expired", 85192L, "Free")]
    [InlineData("active", 999L, "Free")]          // a variant we do not know
    [InlineData("something_new", 85192L, "Free")]
    public void Tier_follows_the_strict_status_rule(string status, long variantId, string expected)
    {
        ApiTier.FromSubscription(status, variantId, null, Now).Name.Should().Be(expected);
    }

    [Fact]
    public void Cancelled_subscription_keeps_its_tier_until_it_ends()
    {
        ApiTier.FromSubscription("cancelled", 85187, Now.AddDays(3), Now).Should().Be(ApiTier.Growth);
        ApiTier.FromSubscription("cancelled", 85187, Now.AddSeconds(-1), Now).Should().Be(ApiTier.Free);
        ApiTier.FromSubscription("cancelled", 85187, null, Now).Should().Be(ApiTier.Free);
    }

    [Fact]
    public void No_subscription_is_free()
    {
        var tier = ApiTier.FromSubscription(null, null, null, Now);

        tier.Should().Be(ApiTier.Free);
        tier.MonthlyRequests.Should().Be(100);
        tier.MaxWindowDays.Should().Be(30);
        tier.MaxServiceClients.Should().Be(1);
    }

    private static (ApiWindow? Window, ApiError? Error) Resolve(StatsRequest request, int? maxDays = null)
        => ApiValidation.ResolveWindow(request, Now, maxDays);

    [Fact]
    public void Default_period_is_the_last_24_hours_anchored_to_five_minutes()
    {
        var (window, error) = Resolve(new StatsRequest());

        error.Should().BeNull();
        window!.To.Should().Be(new DateTime(2026, 11, 14, 10, 5, 0, DateTimeKind.Utc));
        window.From.Should().Be(new DateTime(2026, 11, 13, 10, 5, 0, DateTimeKind.Utc));
        window.Granularity.Should().Be(GranularityEnum.Hour);
    }

    [Theory]
    [InlineData("7d", GranularityEnum.Day)]
    [InlineData("30d", GranularityEnum.Day)]
    [InlineData("month", GranularityEnum.Day)]
    [InlineData("last-month", GranularityEnum.Day)]
    [InlineData("180d", GranularityEnum.Month)]
    [InlineData("all", GranularityEnum.Month)]
    [InlineData("yesterday", GranularityEnum.Hour)]
    public void Presets_carry_the_dashboard_granularity(string period, GranularityEnum expected)
    {
        Resolve(new StatsRequest { Period = period }).Window!.Granularity.Should().Be(expected);
    }

    [Fact]
    public void Last_month_is_the_previous_calendar_month()
    {
        var window = Resolve(new StatsRequest { Period = "last-month" }).Window!;

        window.From.Should().Be(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        window.To.Should().Be(new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc));
    }

    [Fact]
    public void Explicit_range_derives_granularity_from_its_length()
    {
        Resolve(new StatsRequest { From = "2026-11-01T00:00:00Z", To = "2026-11-02T00:00:00Z" }).Window!.Granularity.Should().Be(GranularityEnum.Hour);
        Resolve(new StatsRequest { From = "2026-11-01", To = "2026-11-10" }).Window!.Granularity.Should().Be(GranularityEnum.Day);
        Resolve(new StatsRequest { From = "2026-01-01", To = "2026-11-10" }).Window!.Granularity.Should().Be(GranularityEnum.Month);
    }

    [Fact]
    public void Explicit_range_is_read_as_utc_and_offsets_are_converted()
    {
        var window = Resolve(new StatsRequest { From = "2026-11-01T02:00:00+02:00", To = "2026-11-01T12:00:00" }).Window!;

        window.FromArg.Should().Be("2026-11-01 00:00:00");
        window.ToArg.Should().Be("2026-11-01 12:00:00");
    }

    [Theory]
    [InlineData("7d", "hour", true)]
    [InlineData("14d", "hour", false)]
    [InlineData("24h", "day", true)]
    [InlineData("30d", "month", true)]
    [InlineData("30d", "minute", false)]
    [InlineData("30d", "7", false)]
    public void Hourly_buckets_are_only_for_short_windows(string period, string granularity, bool allowed)
    {
        var (window, error) = Resolve(new StatsRequest { Period = period, Granularity = granularity });

        (window is not null).Should().Be(allowed);
        (error is null).Should().Be(allowed);
    }

    [Theory]
    [InlineData("30d", 30, true)]
    [InlineData("month", 30, true)]        // a 31-day month still fits a 30-day cap
    [InlineData("last-month", 30, true)]
    [InlineData("90d", 30, false)]
    [InlineData("90d", 90, true)]
    [InlineData("365d", 365, true)]
    [InlineData("all", 365, false)]
    public void Window_is_capped_by_the_owners_plan(string period, int maxDays, bool allowed)
    {
        (Resolve(new StatsRequest { Period = period }, maxDays).Error is null).Should().Be(allowed);
    }

    [Fact]
    public void Unlimited_plan_may_query_everything()
    {
        var window = Resolve(new StatsRequest { Period = "all" }, null).Window!;

        window.From.Should().BeNull();
        window.FromArg.Should().BeNull();
    }

    [Theory]
    [InlineData("nope", null, null)]
    [InlineData(null, "2026-11-01", null)]
    [InlineData(null, "2026-11-02", "2026-11-01")]
    [InlineData("7d", "2026-11-01", "2026-11-02")]
    [InlineData(null, "yesterday-ish", "2026-11-02")]
    public void Malformed_windows_are_refused(string? period, string? from, string? to)
    {
        var (window, error) = Resolve(new StatsRequest { Period = period, From = from, To = to });

        window.Should().BeNull();
        error!.Status.Should().Be(400);
    }

    [Fact]
    public void Filters_refuse_commas_and_long_values()
    {
        ApiValidation.ValidateFilters(new StatsRequest { CountryCode = "US", EventName = "it's fine" }).Should().BeNull();
        ApiValidation.ValidateFilters(new StatsRequest { CountryCode = "US,DE" }).Should().NotBeNull();
        ApiValidation.ValidateFilters(new StatsRequest { EventName = new string('x', 201) }).Should().NotBeNull();
    }

    [Fact]
    public void Dimensions_map_to_a_fixed_set_of_columns()
    {
        ApiValidation.Dimensions.Values.Should().BeEquivalentTo(
            ["country_code", "region_name", "os_name", "os_version", "device_model", "app_version", "app_build_number", "event_name"]);
        ApiValidation.Dimensions.ContainsKey("user_id").Should().BeFalse();
        ApiValidation.Dimensions.ContainsKey("Country").Should().BeFalse();
    }

    [Fact]
    public void Billing_pipes_are_not_reachable_from_the_query_api()
    {
        ApiQueryService.AllowedPipes.Should().NotContain(p => p.Contains("billing"));
        ApiQueryService.AllowedPipes.Should().NotContain("live_geo__v1");
        ApiQueryService.AllowedPipes.Should().HaveCount(8);
    }

    [Fact]
    public void Rate_limit_allows_sixty_requests_a_minute_per_grant()
    {
        var limits = new ApiLimits();

        Enumerable.Range(0, ApiLimits.RequestsPerMinute).All(_ => limits.TryCountRequest("grant-a", out var _)).Should().BeTrue();
        limits.TryCountRequest("grant-a", out var retryAfter).Should().BeFalse();
        retryAfter.Should().BeInRange(1, 60);
        limits.TryCountRequest("grant-b", out _).Should().BeTrue("limits are per grant");
    }

    [Fact]
    public void Two_queries_in_flight_per_grant()
    {
        var limits = new ApiLimits();

        limits.TryEnterQuery("g").Should().BeTrue();
        limits.TryEnterQuery("g").Should().BeTrue();
        limits.TryEnterQuery("g").Should().BeFalse();
        limits.TryEnterQuery("other").Should().BeTrue();

        limits.ExitQuery("g");
        limits.TryEnterQuery("g").Should().BeTrue();
    }

    [Fact]
    public void Next_reset_is_the_first_of_next_month_utc()
    {
        ApiMetering.NextReset(new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc)).Should().Be(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        ApiMetering.NextReset(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)).Should().Be(new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Signing_key_is_derived_per_purpose_when_not_configured()
    {
        var authSecret = "qjrp73j3lm3ypZgR07sC9XfviHFP9CensnwEJouYEZw="u8.ToArray();

        var signing = EnvSettings.ResolveOAuthSigningKey("", authSecret);
        var consent = EnvSettings.DeriveKey(authSecret, "aptabase-oauth-consent-v1");

        signing.Should().HaveCount(32);
        signing.Should().NotEqual(authSecret);
        signing.Should().NotEqual(consent);
        signing.Should().Equal(EnvSettings.ResolveOAuthSigningKey("  ", authSecret));
    }

    [Fact]
    public void Configured_signing_secret_is_used_and_must_be_long_enough()
    {
        var authSecret = "qjrp73j3lm3ypZgR07sC9XfviHFP9CensnwEJouYEZw="u8.ToArray();
        const string configured = "9b0e6f0c2d0b4b0f8f3f5d7a6c1e2b3a4d5e6f70";

        EnvSettings.ResolveOAuthSigningKey(configured, authSecret).Should().Equal(System.Text.Encoding.ASCII.GetBytes(configured));

        var act = () => EnvSettings.ResolveOAuthSigningKey("too-short", authSecret);
        act.Should().Throw<Exception>().WithMessage("*OAUTH_SIGNING_SECRET*");
    }
}
