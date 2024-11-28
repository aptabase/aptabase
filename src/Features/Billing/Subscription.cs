
namespace Aptabase.Features.Billing;

public class Subscription
{
    public long Id { get; set; }
    public string OwnerId { get; set; } = "";
    public long CustomerId { get; set; }
    public long ProductId { get; set; }
    public long VariantId { get; set; }
    public string Status { get; set; } = "";
    public DateTime? EndsAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}

public class FreeSubscription
{
    public long? FreeQuota { get; set; }
    public DateTime? FreeTrialEndsAt { get; set; }
}

public class SubscriptionPlan
{
    public const int FreeTrialMonthlyQuota = 1_000_000;

    public string Name { get; }
    public int MonthlyPrice { get; }
    public int MonthlyEvents { get; }
    public long VariantId { get; }
    public DateTime? FreeTrialEndsAt { get; }

    public SubscriptionPlan(string name, int monthlyEvents, int monthlyPrice, long variantId, DateTime? freeTrialEndsAt)
    {
        Name = name;
        MonthlyEvents = monthlyEvents;
        MonthlyPrice = monthlyPrice;
        VariantId = variantId;
        FreeTrialEndsAt = freeTrialEndsAt;
    }

    public static SubscriptionPlan GetFreeVariant(FreeSubscription sub)
    {
        if (sub.FreeQuota.HasValue)
            return new SubscriptionPlan("Free Plan", (int)sub.FreeQuota.Value, 0, 0, null);

        if (sub.FreeTrialEndsAt.HasValue)
            return new SubscriptionPlan("Free Trial", FreeTrialMonthlyQuota, 0, 0, sub.FreeTrialEndsAt);

        throw new InvalidOperationException("No free subscription found");
    }

    public static SubscriptionPlan GetByVariantId(long variantId)
    {
        return ProductionPlans.FirstOrDefault(plan => plan.VariantId == variantId) 
            ?? DevelopmentPlans.FirstOrDefault(plan => plan.VariantId == variantId)
            ?? throw new InvalidOperationException($"Subscription Variant not found for ID {variantId}");
    }

    private static readonly SubscriptionPlan[] DevelopmentPlans =
    [
        new SubscriptionPlan("200k Plan", 200_000, 10, 85183, null),
        new SubscriptionPlan("1M Plan", 1_000_000, 20, 85184, null),
        new SubscriptionPlan("2M Plan", 2_000_000, 40, 85185, null),
        new SubscriptionPlan("5M Plan", 5_000_000, 75, 85187, null),
        new SubscriptionPlan("10M Plan", 10_000_000, 140, 85188, null),
        new SubscriptionPlan("20M Plan", 20_000_000, 240, 85190, null),
        new SubscriptionPlan("30M Plan", 30_000_000, 300, 85192, null),
        new SubscriptionPlan("50M Plan", 50_000_000, 450, 85194, null),
    ];

    private static readonly SubscriptionPlan[] ProductionPlans =
    [
        new SubscriptionPlan("200k Plan", 200_000, 10, 103474, null),
        new SubscriptionPlan("1M Plan", 1_000_000, 20, 103475, null),
        new SubscriptionPlan("2M Plan", 2_000_000, 40, 103476, null),
        new SubscriptionPlan("5M Plan", 5_000_000, 75, 103477, null),
        new SubscriptionPlan("10M Plan", 10_000_000, 140, 103478, null),
        new SubscriptionPlan("20M Plan", 20_000_000, 240, 103479, null),
        new SubscriptionPlan("30M Plan", 30_000_000, 300, 103480, null),
        new SubscriptionPlan("50M Plan", 50_000_000, 450, 103481, null),
        new SubscriptionPlan("100M Plan", 100_000_000, 750, 614418, null),
        new SubscriptionPlan("200M Plan", 200_000_000, 1200, 614417, null),
    ];
}