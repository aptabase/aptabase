namespace Aptabase.Features.Billing.LemonSqueezy;

/// <summary>
/// Maps LemonSqueezy subscription statuses to what they mean for the owner's account.
/// Statuses: on_trial, active, paused, past_due, unpaid, cancelled, expired.
/// </summary>
public static class SubscriptionStatus
{
    /// <summary>
    /// A status change to one of these means the customer is entitled to the plan and any
    /// overuse/trial lock on the owner can be cleared. Every other status (cancelled, expired,
    /// paused, past_due, unpaid) must never clear a lock: a lock is only released by payment.
    /// </summary>
    public static bool GrantsAccess(string? status) =>
        status is "active" or "on_trial";

    /// <summary>
    /// The subscription is over and its row should be removed.
    /// </summary>
    public static bool IsEnded(string? status) =>
        status is "expired";
}
