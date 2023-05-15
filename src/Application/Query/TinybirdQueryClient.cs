using System.Text.Json;
using System.Net;

namespace Aptabase.Application.Query;

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

    public async Task<(IEnumerable<T>, QueryStatistics)> QueryAsync<T>(string query, CancellationToken cancellationToken)
    {
        var q = WebUtility.UrlEncode($"{query} FORMAT JSON");
        var path = $"/v0/sql?q={q}";
        var response = await _httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<QueryResult<T>>() ?? new QueryResult<T>();
        return (result.Data, result.Statistics);
    }

    public async Task<(T, QueryStatistics)> QuerySingleAsync<T>(string query, CancellationToken cancellationToken) where T : new()
    {
        var (result, stats) = await QueryAsync<T>(query, cancellationToken);
        if (result.Any())
            return (result.First(), stats);

        return (new T(), stats);
    }
}