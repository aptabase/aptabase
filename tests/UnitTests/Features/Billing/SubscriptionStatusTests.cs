using Xunit;
using Aptabase.Features.Billing.LemonSqueezy;
using AwesomeAssertions;

namespace Aptabase.UnitTests.Features.Billing;

public class SubscriptionStatusTests
{
    [Theory]
    [InlineData("active")]
    [InlineData("on_trial")]
    public void Paid_statuses_grant_access(string status)
    {
        SubscriptionStatus.GrantsAccess(status).Should().BeTrue();
    }

    // Regression: a subscription_updated webhook with status "cancelled" or "expired" used to
    // clear the owner's lock_reason, so cancelling a subscription unlocked an over-quota account.
    [Theory]
    [InlineData("cancelled")]
    [InlineData("expired")]
    [InlineData("paused")]
    [InlineData("past_due")]
    [InlineData("unpaid")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("ACTIVE")]
    public void Non_paid_statuses_never_grant_access(string? status)
    {
        SubscriptionStatus.GrantsAccess(status).Should().BeFalse();
    }

    [Theory]
    [InlineData("expired", true)]
    [InlineData("cancelled", false)]
    [InlineData("active", false)]
    [InlineData(null, false)]
    public void Only_expired_ends_the_subscription(string? status, bool expected)
    {
        SubscriptionStatus.IsEnded(status).Should().Be(expected);
    }
}
