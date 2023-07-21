using System.Text.Json;
using System.Net;
using System.Text.Json.Serialization;

namespace Aptabase.Features.Query;

public class QueryResult<T>
{
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
}


public class TinybirdQueryClient : IQueryClient
{
    private static readonly JsonSerializerOptions JsonSettings = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private HttpClient _httpClient;
    private ILogger _logger;

    public TinybirdQueryClient(IHttpClientFactory factory, ILogger<TinybirdQueryClient> logger)
    {
        _httpClient = factory.CreateClient("Tinybird");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string query, CancellationToken cancellationToken)
    {
        var q = WebUtility.UrlEncode($"{query} FORMAT JSON");
        var path = $"/v0/sql?q={q}";
        var response = await _httpClient.GetAsync(path, cancellationToken);

        response.EnsureSuccessWithLog(_logger);
        var result = await response.Content.ReadFromJsonAsync<QueryResult<T>>() ?? new QueryResult<T>();
        return result.Data;
    }

    public async Task<T> QuerySingleAsync<T>(string query, CancellationToken cancellationToken) where T : new()
    {
        var result = await QueryAsync<T>(query, cancellationToken);
        if (result.Any())
            return result.First();

        return new T();
    }

    public async Task<IEnumerable<T>> NamedQueryAsync<T>(string name, object args, CancellationToken cancellationToken)
    {
        var nameValueCol = args.GetType().GetProperties().Select(x => new KeyValuePair<string, string>(x.Name, FormatArg(x.GetValue(args, null))));
        var formData = new FormUrlEncodedContent(nameValueCol);
        var response = await _httpClient.PostAsync($"/v0/pipes/{name}.json", formData, cancellationToken);

        response.EnsureSuccessWithLog(_logger);
        var result = await response.Content.ReadFromJsonAsync<QueryResult<T>>() ?? new QueryResult<T>();
        return result.Data;
    }

    public async Task<T> NamedQuerySingleAsync<T>(string name, object args, CancellationToken cancellationToken) where T : new()
    {
        var result = await NamedQueryAsync<T>(name, args, cancellationToken);
        if (result.Any())
            return result.First();

        return new T();
    }

    private string FormatArg(object? value)
    {
        switch (value)
        {
            case string[] s:
                return string.Join(",", s);
            case DateTime d:
                return $"'{d:yyyy-MM-dd HH:mm:ss}'";
            default:
                return value?.ToString() ?? "";
        }
    }
}