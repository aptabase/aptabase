
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

public class SubscriptionPlan
{
    public string Name { get; }
    public int MonthlyPrice { get; }
    public int MonthlyEvents { get; }
    public long VariantId { get; }

    public SubscriptionPlan(string name, int monthlyEvents, int monthlyPrice, long variantId)
    {
        Name = name;
        MonthlyEvents = monthlyEvents;
        MonthlyPrice = monthlyPrice;
        VariantId = variantId;
    }

    public static readonly SubscriptionPlan AptabaseFree = new SubscriptionPlan("Free", 20_000, 0, 0);

    public static SubscriptionPlan GetByVariantId(long variantId)
    {
        return ProductionPlans.FirstOrDefault(plan => plan.VariantId == variantId) 
            ?? DevelopmentPlans.FirstOrDefault(plan => plan.VariantId == variantId)
            ?? SubscriptionPlan.AptabaseFree;
    }

    private static readonly SubscriptionPlan[] DevelopmentPlans = new[]
    {
        new SubscriptionPlan("200k", 200_000, 10, 85183),
        new SubscriptionPlan("1M", 1_000_000, 20, 85184),
        new SubscriptionPlan("2M", 2_000_000, 40, 85185),
        new SubscriptionPlan("5M", 5_000_000, 75, 85187),
        new SubscriptionPlan("10M", 10_000_000, 140, 85188),
        new SubscriptionPlan("20M", 20_000_000, 240, 85190),
        new SubscriptionPlan("30M", 30_000_000, 300, 85192),
        new SubscriptionPlan("50M", 50_000_000, 450, 85194),
    };

    private static readonly SubscriptionPlan[] ProductionPlans = new[]
    {
        new SubscriptionPlan("200k", 200_000, 10, 103474),
        new SubscriptionPlan("1M", 1_000_000, 20, 103475),
        new SubscriptionPlan("2M", 2_000_000, 40, 103476),
        new SubscriptionPlan("5M", 5_000_000, 75, 103477),
        new SubscriptionPlan("10M", 10_000_000, 140, 103478),
        new SubscriptionPlan("20M", 20_000_000, 240, 103479),
        new SubscriptionPlan("30M", 30_000_000, 300, 103480),
        new SubscriptionPlan("50M", 50_000_000, 450, 103481),
    };
}