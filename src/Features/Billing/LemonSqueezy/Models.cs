namespace Aptabase.Features.Billing.LemonSqueezy;

public class PagedList<T> where T : new()
{
    public IEnumerable<Resource<T>> Data { get; set; } = Enumerable.Empty<Resource<T>>();
}

public class Resource<T> where T : new()
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public T Attributes { get; set; } = new T();
}

public class GetResponse<T> where T : new()
{
    public T Data { get; set; } = new T();
}

public class CheckoutAttributes
{
    public string Url { get; set; } = "";
}

public class SubscriptionAttributes
{
    public int ProductID { get; set; }
    public int VariantID { get; set; }
    public int CustomerID { get; set; }
    public string Status { get; set; } = "";
    public DateTime? EndsAt { get; set; }
    public SubscriptionUrls Urls { get; set; } = new SubscriptionUrls();
}

public class SubscriptionUrls
{
    public string CustomerPortal { get; set; } = "";

    // Opens the portal's plan picker directly. The API reference documents this link under two
    // names and says it is PayPal-only (card subscriptions return it too), so callers must fall back.
    public string? CustomerPortalUpdateSubscription { get; set; }
    public string? UpdateCustomerPortal { get; set; }
}