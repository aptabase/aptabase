using System.Text.Json.Serialization;

namespace Aptabase.Features.Billing.LemonSqueezy;

public class PagedList<T> where T : new()
{
    public PagedListMeta Meta { get; set; } = new PagedListMeta();
    public IEnumerable<Resource<T>> Data { get; set; } = Enumerable.Empty<Resource<T>>();
}

public class PagedListMeta
{
    public PageInfo Page { get; set; } = new PageInfo();
}

// LemonSqueezy's page metadata is camelCase, unlike the snake_case attributes.
public class PageInfo
{
    [JsonPropertyName("currentPage")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("lastPage")]
    public int LastPage { get; set; }

    [JsonPropertyName("perPage")]
    public int PerPage { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
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
}