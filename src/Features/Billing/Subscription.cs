
public class SubscriptionPlan
{
    public string Name { get; }
    public int MonthlyPrice { get; }
    public int MonthlyEvents { get; }
    public long VariantId { get; }

    private SubscriptionPlan(string name, int monthlyEvents, int monthlyPrice, long variantId)
    {
        Name = name;
        MonthlyEvents = monthlyEvents;
        MonthlyPrice = monthlyPrice;
        VariantId = variantId;
    }

    public static readonly SubscriptionPlan AptabaseFree = new SubscriptionPlan("Free", 20_000, 0, 0);
    public static readonly SubscriptionPlan Aptabase200k = new SubscriptionPlan("200k", 200_000, 10, 85183);
    public static readonly SubscriptionPlan Aptabase1M = new SubscriptionPlan("1M", 1_000_000, 20, 85184);
    public static readonly SubscriptionPlan Aptabase2M = new SubscriptionPlan("2M", 2_000_000, 40, 85185);
    public static readonly SubscriptionPlan Aptabase5M = new SubscriptionPlan("5M", 5_000_000, 75, 85187);
    public static readonly SubscriptionPlan Aptabase10M = new SubscriptionPlan("10M", 10_000_000, 140, 85188);
    public static readonly SubscriptionPlan Aptabase20M = new SubscriptionPlan("20M", 20_000_000, 240, 85190);
    public static readonly SubscriptionPlan Aptabase30M = new SubscriptionPlan("30M", 30_000_000, 300, 85192);
    public static readonly SubscriptionPlan Aptabase50M = new SubscriptionPlan("50M", 50_000_000, 450, 85194);

    public static SubscriptionPlan GetByVariantId(long variantId)
    {
        return All.FirstOrDefault(plan => plan.VariantId == variantId) ?? SubscriptionPlan.AptabaseFree;
    }

    public static readonly SubscriptionPlan[] All = new[]
    {
        Aptabase200k,
        Aptabase1M,
        Aptabase2M,
        Aptabase5M,
        Aptabase10M,
        Aptabase20M,
        Aptabase30M,
        Aptabase50M
    };
}

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