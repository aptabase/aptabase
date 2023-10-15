using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Extensions;

namespace Aptabase.Features.Stats;

public class QueryResult<T>
{
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
}

public class TinybirdQueryClient : IQueryClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _options;

    public TinybirdQueryClient(IHttpClientFactory factory, ILogger<TinybirdQueryClient> logger)
    {
        _httpClient = factory.CreateClient("Tinybird");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new TinybirdDateTimeJsonConverter());
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string query, CancellationToken cancellationToken)
    {
        var q = WebUtility.UrlEncode($"{query} FORMAT JSON");
        var path = $"/v0/sql?q={q}";
        var response = await _httpClient.GetAsync(path, cancellationToken);

        await response.EnsureSuccessWithLog(_logger);
        var result = await response.Content.ReadFromJsonAsync<QueryResult<T>>(_options) ?? new QueryResult<T>();
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

        var query = new QueryBuilder();
        foreach (var prop in args.GetType().GetProperties())
        {
            var value = prop.GetValue(args, null);
            if (value is not null)
                query.Add(prop.Name, FormatArg(value));
        }

        var response = await _httpClient.GetAsync($"/v0/pipes/{name}.json{query.ToQueryString()}", cancellationToken);
        await response.EnsureSuccessWithLog(_logger);
        var result = await response.Content.ReadFromJsonAsync<QueryResult<T>>(_options) ?? new QueryResult<T>();
        return result.Data;
    }

    public async Task<T> NamedQuerySingleAsync<T>(string name, object args, CancellationToken cancellationToken) where T : new()
    {
        var result = await NamedQueryAsync<T>(name, args, cancellationToken);
        if (result.Any())
            return result.First();

        return new T();
    }

    private static string FormatArg(object value)
    {
        return value switch
        {
            string[] s => string.Join(",", s),
            DateTime d => $"'{d:yyyy-MM-dd HH:mm:ss}'",
            _ => value?.ToString() ?? "",
        };
    }
}