using Xunit;
using Aptabase.Features.Billing.LemonSqueezy;
using AwesomeAssertions;

namespace Aptabase.UnitTests.Features.Billing;

public class SubscriptionStatusTests
{
    // Regression: a subscription_updated webhook with status "cancelled" or "expired" used to
    // clear the owner's lock_reason, so cancelling a subscription unlocked an over-quota account.
    [Theory]
    [InlineData("cancelled")]
    [InlineData("expired")]
    public void Cancelled_and_expired_never_clear_a_lock(string status)
    {
        SubscriptionStatus.ClearsLock(status).Should().BeFalse();
    }

    [Theory]
    [InlineData("active")]
    [InlineData("on_trial")]
    [InlineData("paused")]
    [InlineData("past_due")]
    [InlineData("unpaid")]
    [InlineData("")]
    [InlineData(null)]
    public void Every_other_status_clears_a_lock_as_before(string? status)
    {
        SubscriptionStatus.ClearsLock(status).Should().BeTrue();
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
