namespace Aptabase.Features.Billing;

public class BillingUsage
{
    public long Count { get; set; }
}

public class BillingUsageByApp
{
    public string AppId { get; set; } = "";
    public long Count { get; set; }
}

public class UserQuota
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string[] AppIds { get; set; } = Array.Empty<string>();
    public long? FreeQuota { get; set; }
    public long? VariantID { get; set; }

    public long GetQuota()
    {
        if (VariantID is not null)
            return SubscriptionPlan.GetByVariantId(VariantID.Value).MonthlyEvents;

        return FreeQuota ?? SubscriptionPlan.FreeTrialMonthlyQuota;
    }
}

public class BillingHistoricUsage
{
    public DateTime Date { get; set; }
    public long Events { get; set; }
}