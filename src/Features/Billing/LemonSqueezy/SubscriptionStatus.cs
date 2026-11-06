namespace Aptabase.Features.Billing.LemonSqueezy;

/// <summary>
/// Maps LemonSqueezy subscription statuses to what they mean for the owner's account.
/// Statuses: on_trial, active, paused, past_due, unpaid, cancelled, expired.
/// </summary>
public static class SubscriptionStatus
{
    /// <summary>
    /// Whether a subscription change with this status may clear an overuse/trial lock on the
    /// owner. Every status does, as before, except the two that mean the customer is leaving:
    /// a cancellation or expiry must never release a lock.
    /// </summary>
    public static bool ClearsLock(string? status) =>
        status is not ("cancelled" or "expired");

    /// <summary>
    /// The subscription is over and its row should be removed.
    /// </summary>
    public static bool IsEnded(string? status) =>
        status is "expired";
}
