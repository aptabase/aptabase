using Aptabase.Data;
using Aptabase.Features.Billing;
using Dapper;
using Microsoft.Extensions.Caching.Memory;

namespace Aptabase.Features.Api;

/// <summary>
/// What a plan gives the query API. Null means unlimited.
/// </summary>
public record ApiTier(string Name, long? MonthlyRequests, int? MaxWindowDays, int? MaxServiceClients)
{
    public static readonly ApiTier Free = new("Free", 100, 30, 1);
    public static readonly ApiTier Starter = new("Starter", 2_000, 90, 3);
    public static readonly ApiTier Growth = new("Growth", 10_000, 365, 10);
    public static readonly ApiTier Scale = new("Scale", 50_000, null, 10);
    public static readonly ApiTier Unlimited = new("Unlimited", null, null, null);

    /// <summary>
    /// Maps a subscription to a tier. Stricter than the billing page, which treats every
    /// status except expired as a live subscription: that is right for showing a plan
    /// name and wrong for granting API volume. Paid tiers need a subscription that is
    /// active, on trial, or cancelled but not yet at its end date. Everything else,
    /// including a variant we do not know, is Free.
    /// </summary>
    public static ApiTier FromSubscription(string? status, long? variantId, DateTime? endsAt, DateTime utcNow)
    {
        if (variantId is null)
            return Free;

        var isLive = status switch
        {
            "active" or "on_trial" => true,
            "cancelled" => endsAt.HasValue && endsAt.Value > utcNow,
            _ => false,
        };

        if (!isLive)
            return Free;

        SubscriptionPlan plan;
        try
        {
            plan = SubscriptionPlan.GetByVariantId(variantId.Value);
        }
        catch (InvalidOperationException)
        {
            return Free;
        }

        return plan.MonthlyEvents switch
        {
            <= 2_000_000 => Starter,
            <= 20_000_000 => Growth,
            _ => Scale,
        };
    }
}

public interface IApiTiers
{
    Task<ApiTier> ResolveAsync(string ownerId, CancellationToken cancellationToken);
}

public class ApiTiers : IApiTiers
{
    private readonly IDbContext _db;
    private readonly EnvSettings _env;
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;

    public ApiTiers(IDbContext db, EnvSettings env, IMemoryCache cache, ILogger<ApiTiers> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private class SubscriptionRow
    {
        public string Status { get; set; } = "";
        public long VariantId { get; set; }
        public DateTime? EndsAt { get; set; }
    }

    public async Task<ApiTier> ResolveAsync(string ownerId, CancellationToken cancellationToken)
    {
        // Self-hosted installs have no plans: no allowance, no window cap, no client limit
        if (!_env.IsBillingEnabled)
            return ApiTier.Unlimited;

        var cacheKey = $"api-tier:{ownerId}";
        if (_cache.TryGetValue<ApiTier>(cacheKey, out var cached) && cached is not null)
            return cached;

        ApiTier tier;
        try
        {
            using var cn = _db.Connection;
            var sub = await cn.QueryFirstOrDefaultAsync<SubscriptionRow>(new CommandDefinition(
                @"SELECT status, variant_id, ends_at FROM subscriptions
                  WHERE owner_id = @ownerId
                  ORDER BY created_at DESC LIMIT 1",
                new { ownerId }, cancellationToken: cancellationToken));

            tier = ApiTier.FromSubscription(sub?.Status, sub?.VariantId, sub?.EndsAt, DateTime.UtcNow);

            if (sub is not null && tier == ApiTier.Free && sub.Status is "active" or "on_trial")
                _logger.LogWarning("Subscription variant {VariantId} of {OwnerId} is unknown, resolving the API tier to Free", sub.VariantId, ownerId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed: a billing lookup that breaks must not hand out paid volume
            _logger.LogError(ex, "Could not resolve the API tier of {OwnerId}, falling back to Free", ownerId);
            return ApiTier.Free;
        }

        _cache.Set(cacheKey, tier, TimeSpan.FromSeconds(60));
        return tier;
    }
}
