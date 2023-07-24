using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Aptabase.Features.Billing.LemonSqueezy;
using System.Security.Cryptography;
using System.Text;
using Aptabase.Data;

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
        var (isValid, body) = await ValidateSignature(HttpContext);
        if (!isValid)
        {
            _logger.LogWarning("Invalid LemonSqueezy Signature");
            return Unauthorized(new { message = "Invalid LemonSqueezy Signature" });
        }

        var ev = JsonSerializer.Deserialize<WebhookEvent>(body, LemonSqueezyClient.JsonSettings);
        if (ev == null)
        {
            _logger.LogWarning("Invalid JSON Body: {Body}", body);
            return BadRequest(new { message = "Invalid JSON Body" });
        }

        if (ev.Meta.CustomData.TryGetValue("region", out var eventRegion) == false)
        {
            _logger.LogError("LemonSqueezy event is missing region");
            return BadRequest(new { message = "Missing 'region' on meta.custom_data" });
        }

        if (eventRegion != _region)
            return Ok(new { message = "Ignoring event from different region" });
        
        var task = ev.Meta.EventName switch {
            "subscription_created" => HandleSubscriptionCreatedOrUpdated(ev, cancellationToken),
            "subscription_updated" => HandleSubscriptionCreatedOrUpdated(ev, cancellationToken),
            _ => HandleUnknownEvent(ev, cancellationToken),
        };

        return await task;
    }

    private async Task<IActionResult> HandleSubscriptionCreatedOrUpdated([FromBody] WebhookEvent ev, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Deserialize<SubscriptionDataEvent>(ev.Data.Attributes, LemonSqueezyClient.JsonSettings);
        if (body == null)
            return BadRequest(new { message = "Event body is null" });

        var subId = Convert.ToInt64(ev.Data.Id);
        var ownerId = ev.Meta.CustomData["user_id"];
        if (string.IsNullOrEmpty(ownerId))
        {
            _logger.LogError("LemonSqueezy event is missing user_id");
            return BadRequest(new { message = "Missing 'user_id' on meta.custom_data" });
        }

        await _db.ExecuteAsync(@"INSERT INTO subscriptions
                                    (id, owner_id, customer_id, product_id, variant_id, status, ends_at)
                                 VALUES
                                    (@subId, @ownerId, @customerId, @productId, @variantId, @status, @endsAt)
                                 ON CONFLICT (id) 
                                 DO UPDATE SET product_id = @productId,
                                               variant_id = @variantId,
                                               status = @status,
                                               ends_at = @endsAt,
                                               modified_at = now()", new {
            subId,
            ownerId,
            customerId = body.CustomerID,
            productId = body.ProductID,
            variantId = body.VariantID,
            status = body.Status,
            endsAt = body.EndsAt
        });

        return Ok(new {});
    }

    private async Task<(bool, string)> ValidateSignature(HttpContext http)
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
            return (false, string.Empty);

        return (true, body);
    }

    private Task<IActionResult> HandleUnknownEvent(WebhookEvent ev, CancellationToken cancellationToken)
    {
        _logger.LogError("Unknown LemonSqueezy event: {EventName}", ev.Meta.EventName);
        return Task.FromResult<IActionResult>(BadRequest(new { message = "Unknown event" }));
    }
}
