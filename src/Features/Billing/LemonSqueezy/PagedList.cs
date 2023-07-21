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