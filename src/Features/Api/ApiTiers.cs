using Aptabase.Features.Authentication;
using Aptabase.Features.Billing;
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
    /// Maps the plan billing resolved for an account to a tier. Which plan an account is
    /// on is billing's decision alone; this only sizes the query API for that plan.
    /// </summary>
    public static ApiTier FromPlan(SubscriptionPlan plan)
    {
        // Free Plan and Free Trial carry no variant
        if (plan.VariantId == 0)
            return Free;

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
    private readonly IBillingQueries _billing;
    private readonly EnvSettings _env;
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;

    public ApiTiers(IBillingQueries billing, EnvSettings env, IMemoryCache cache, ILogger<ApiTiers> logger)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var owner = new UserIdentity(ownerId, "", "");
            var sub = await _billing.GetUserSubscription(owner);
            tier = ApiTier.FromPlan(await _billing.ResolvePlan(owner, sub));
        }
        catch (Exception ex)
        {
            // Fail closed: an unknown variant, an account with no free plan on record, or a
            // billing lookup that breaks must not hand out paid volume
            _logger.LogError(ex, "Could not resolve the plan of {OwnerId}, the API tier falls back to Free", ownerId);
            tier = ApiTier.Free;
        }

        _cache.Set(cacheKey, tier, TimeSpan.FromSeconds(60));
        return tier;
    }
}
