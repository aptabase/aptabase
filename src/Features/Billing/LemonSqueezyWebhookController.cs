using Aptabase.Data;
using Aptabase.Features.Billing.LemonSqueezy;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Aptabase.Features.Billing;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class LemonSqueezyWebhookController : Controller
{
    private readonly IDbContext _db;
    private readonly LemonSqueezyClient _lsClient;
    private readonly ILogger _logger;

    private readonly string _region;
    private readonly byte[] _signingSecret;

    public LemonSqueezyWebhookController(EnvSettings env, IDbContext db, LemonSqueezyClient lsClient, ILogger<LemonSqueezyWebhookController> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _lsClient = lsClient ?? throw new ArgumentNullException(nameof(lsClient));
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
        catch (JsonException)
        {
            ev = null;
        }

        if (ev == null)
        {
            _logger.LogWarning("Invalid JSON Body: {Body}", body);

            return BadRequest(new { message = "Invalid JSON Body" });
        }

        // subscription_plan_changed is what LemonSqueezy sends when a customer switches plan in
        // the customer portal. Answering it with 400 left the stored variant, and so the quota,
        // at the old plan.
        var isSubscriptionEvent = ev.Meta.EventName is "subscription_created" or "subscription_updated" or "subscription_plan_changed";

        // The region comes from the checkout's custom data, which later events about the same
        // subscription are not guaranteed to carry. A subscription event without it is matched
        // by subscription id instead, below.
        var hasRegion = ev.Meta.CustomData.TryGetValue("region", out var eventRegion);

        if (!hasRegion && !isSubscriptionEvent)
        {
            _logger.LogError("LemonSqueezy event is missing region");

            return BadRequest(new { message = "Missing 'region' on meta.custom_data" });
        }

        if (hasRegion && eventRegion != _region)
        {
            return Ok(new { message = "Ignoring event from different region" });
        }

        return isSubscriptionEvent
            ? await HandleSubscriptionChanged(ev, hasRegion)
            : await HandleUnknownEvent(ev);
    }

    private async Task<IActionResult> HandleSubscriptionChanged(WebhookEvent ev, bool hasRegion)
    {
        var body = JsonSerializer.Deserialize<SubscriptionAttributes>(ev.Data.Attributes, LemonSqueezyClient.JsonSettings);

        if (body == null)
        {
            return BadRequest(new { message = "Event body is null" });
        }

        var subId = Convert.ToInt64(ev.Data.Id);
        ev.Meta.CustomData.TryGetValue("user_id", out var ownerId);

        // An event about a subscription we already store belongs to its stored owner
        if (string.IsNullOrEmpty(ownerId))
        {
            ownerId = await _db.Connection.QueryFirstOrDefaultAsync<string>(
                @"SELECT owner_id FROM subscriptions WHERE id = @subId",
                new { subId });
        }

        if (string.IsNullOrEmpty(ownerId))
        {
            if (!hasRegion)
            {
                // Without a region every region's deployment receives this event. Only the one
                // that stores the subscription acts on it; the others acknowledge and move on.
                _logger.LogWarning("LemonSqueezy {EventName} for subscription {SubId} has no custom_data and the subscription is not stored in this region; ignoring", ev.Meta.EventName, subId);

                return Ok(new { message = "Ignoring event for a subscription not stored in this region" });
            }

            _logger.LogError("LemonSqueezy event is missing user_id");

            return BadRequest(new { message = "Missing 'user_id' on meta.custom_data" });
        }

        // Only a subscription that is being paid for releases the owner's lock. A cancellation,
        // an expiry, a pause or a failed payment arrives as subscription_updated too, and would
        // otherwise let an over-quota account resume ingestion.
        if (body.Status is "active" or "on_trial")
        {
            await _db.Connection.ExecuteAsync(
                @"UPDATE users SET lock_reason = null WHERE id = @ownerId",
                new { ownerId }
            );
        }

        if (body.Status == "expired")
        {
            await _db.Connection.ExecuteAsync(
                @"DELETE FROM subscriptions WHERE id = @subId",
            new
            {
                subId,
            });
        }
        else
        {
            await _db.Connection.ExecuteAsync(
                @"INSERT INTO subscriptions
                    (id, owner_id, customer_id, product_id, variant_id, status, ends_at)
                VALUES
                    (@subId, @ownerId, @customerId, @productId, @variantId, @status, @endsAt)
                ON CONFLICT (id) 
                DO UPDATE SET product_id = @productId,
                            variant_id = @variantId,
                            status = @status,
                            ends_at = @endsAt,
                            modified_at = now()",
            new
            {
                subId,
                ownerId,
                customerId = body.CustomerID,
                productId = body.ProductID,
                variantId = body.VariantID,
                status = body.Status,
                endsAt = body.EndsAt,
            });
        }

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

    private Task<IActionResult> HandleUnknownEvent(WebhookEvent ev)
    {
        _logger.LogError("Unknown LemonSqueezy event: {EventName}", ev.Meta.EventName);

        return Task.FromResult<IActionResult>(BadRequest(new { message = "Unknown event" }));
    }
}
