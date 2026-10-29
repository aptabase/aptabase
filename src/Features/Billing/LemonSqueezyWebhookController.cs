using Aptabase.Features.Billing.LemonSqueezy;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Aptabase.Features.Billing;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class LemonSqueezyWebhookController : Controller
{
    private readonly ISubscriptionSync _subscriptions;
    private readonly ILogger _logger;

    private readonly string _region;
    private readonly byte[] _signingSecret;

    public LemonSqueezyWebhookController(EnvSettings env, ISubscriptionSync subscriptions, ILogger<LemonSqueezyWebhookController> logger)
    {
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _region = env.Region;
        _signingSecret = Encoding.UTF8.GetBytes(env.LemonSqueezySigningSecret);
    }

    [HttpPost("/webhook/lemonsqueezy")]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        var (isValid, body) = await ValidateSignatureAsync(HttpContext);

        if (!isValid)
        {
            _logger.LogWarning("Invalid LemonSqueezy Signature");

            return Unauthorized(new { message = "Invalid LemonSqueezy Signature" });
        }

        WebhookEvent? ev;
        try
        {
            ev = JsonSerializer.Deserialize<WebhookEvent>(body, LemonSqueezyClient.JsonSettings);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON Body: {Body}", body);
            return BadRequest(new { message = "Invalid JSON Body" });
        }

        if (ev == null)
        {
            _logger.LogWarning("Invalid JSON Body: {Body}", body);

            return BadRequest(new { message = "Invalid JSON Body" });
        }

        return SubscriptionLifecycleEvents.Contains(ev.Meta.EventName)
            ? await HandleSubscriptionChanged(ev)
            : AcknowledgeOtherEvent(ev);
    }

    // Every one of these carries the subscription object in `data`, so they all go through the same
    // handler; what happens to the owner's lock depends on the status, not on the event name.
    // subscription_plan_changed is the one LemonSqueezy sends when a customer switches plan in the
    // customer portal; until it was listed here a plan change was answered with 400 and the stored
    // variant (and therefore the quota) stayed at the old plan.
    private static readonly HashSet<string> SubscriptionLifecycleEvents = new(StringComparer.Ordinal)
    {
        "subscription_created",
        "subscription_updated",
        "subscription_plan_changed",
        "subscription_cancelled",
        "subscription_resumed",
        "subscription_expired",
        "subscription_paused",
        "subscription_unpaused",
    };

    private async Task<IActionResult> HandleSubscriptionChanged(WebhookEvent ev)
    {
        var body = JsonSerializer.Deserialize<SubscriptionAttributes>(ev.Data.Attributes, LemonSqueezyClient.JsonSettings);

        if (body == null)
        {
            return BadRequest(new { message = "Event body is null" });
        }

        if (!long.TryParse(ev.Data.Id, out var subId))
        {
            _logger.LogError("LemonSqueezy {EventName} has a non-numeric subscription id: {Id}", ev.Meta.EventName, ev.Data.Id);
            return BadRequest(new { message = "Invalid subscription id" });
        }

        // custom_data is what the checkout attached (user_id + region). It is the source of truth for a
        // new subscription, but later events are not guaranteed to carry it, so an event about a
        // subscription we already store is attributed to its stored owner instead.
        ev.Meta.CustomData.TryGetValue("region", out var eventRegion);
        ev.Meta.CustomData.TryGetValue("user_id", out var customOwnerId);

        string? ownerId;
        if (!string.IsNullOrEmpty(eventRegion))
        {
            if (eventRegion != _region)
            {
                return Ok(new { message = "Ignoring event from different region" });
            }

            ownerId = !string.IsNullOrEmpty(customOwnerId) ? customOwnerId : await _subscriptions.FindOwnerId(subId);
            if (string.IsNullOrEmpty(ownerId))
            {
                _logger.LogError("LemonSqueezy {EventName} for subscription {SubId} has no user_id and the subscription is unknown", ev.Meta.EventName, subId);
                return BadRequest(new { message = "Missing 'user_id' on meta.custom_data" });
            }
        }
        else
        {
            // Without a region every region's deployment receives this event. Only the one that
            // already stores the subscription may act on it; the others acknowledge and move on.
            ownerId = await _subscriptions.FindOwnerId(subId);
            if (string.IsNullOrEmpty(ownerId))
            {
                _logger.LogWarning("LemonSqueezy {EventName} for subscription {SubId} has no custom_data and the subscription is not stored in this region; ignoring", ev.Meta.EventName, subId);
                return Ok(new { message = "Ignoring event for a subscription not stored in this region" });
            }
        }

        await _subscriptions.Apply(subId, ownerId, body);

        _logger.LogInformation("Applied LemonSqueezy {EventName} for subscription {SubId} of user {UserId}: variant {VariantId}, status {Status}", ev.Meta.EventName, subId, ownerId, body.VariantID, body.Status);

        return Ok(new {});
    }

    private async Task<(bool, string)> ValidateSignatureAsync(HttpContext http)
    {
        using var reader = new StreamReader(Request.Body);
        string body = await reader.ReadToEndAsync();

        using var hmac = new HMACSHA256(_signingSecret);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var digest = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
        var digestBytes = Encoding.UTF8.GetBytes(digest);

        var signature = http.Request.Headers["X-Signature"].ToString() ?? string.Empty;
        var signatureBytes = Encoding.UTF8.GetBytes(signature);

        if (!CryptographicOperations.FixedTimeEquals(digestBytes, signatureBytes))
        {
            return (false, string.Empty);
        }

        return (true, body);
    }

    // Payment, order, customer and license events carry no subscription state we need, and an unknown
    // event is not a delivery failure on our side: LemonSqueezy retries anything but a 200 three times
    // and then drops it, so a non-200 here only adds noise. Acknowledge and log instead.
    private IActionResult AcknowledgeOtherEvent(WebhookEvent ev)
    {
        if (ev.Meta.EventName.StartsWith("subscription_payment_", StringComparison.Ordinal)
            || ev.Meta.EventName.StartsWith("order_", StringComparison.Ordinal)
            || ev.Meta.EventName.StartsWith("license_key_", StringComparison.Ordinal)
            || ev.Meta.EventName is "customer_updated" or "affiliate_activated")
        {
            _logger.LogInformation("Acknowledged LemonSqueezy {EventName} without changes", ev.Meta.EventName);
        }
        else
        {
            _logger.LogWarning("Unknown LemonSqueezy event acknowledged without changes: {EventName}", ev.Meta.EventName);
        }

        return Ok(new { message = "Acknowledged" });
    }
}
