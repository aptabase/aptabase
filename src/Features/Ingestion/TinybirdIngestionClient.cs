using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aptabase.Features.Ingestion;

public class InsertResult
{
    [JsonPropertyName("successful_rows")]
    public int SuccessfulRows { get; set; }
    [JsonPropertyName("quarantined_rows")]
    public int QuarantinedRows { get; set; }
}

public class TinybirdIngestionClient : IIngestionClient
{
    private static readonly JsonSerializerOptions JsonSettings = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public TinybirdIngestionClient(IHttpClientFactory factory, ILogger<TinybirdIngestionClient> logger)
    {
        _httpClient = factory.CreateClient("Tinybird");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private string EventsPath => $"/v0/events?name=events";

    public Task<long> SendEventAsync(EventRow row, CancellationToken cancellationToken)
    {
        return PostAsync(EventsPath, new[] { row }, cancellationToken);
    }

    public Task<long> BulkSendEventAsync(IEnumerable<EventRow> rows, CancellationToken cancellationToken)
    {
        return PostAsync(EventsPath, rows, cancellationToken);
    }

    private async Task<long> PostAsync<T>(string path, IEnumerable<T> rows, CancellationToken cancellationToken)
    {
        var rowsAsString = rows.Select(row => JsonSerializer.Serialize(row, JsonSettings));
        var content = new StringContent(string.Join('\n', rowsAsString));
        var response = await _httpClient.PostAsync(path, content, cancellationToken);
        
        await response.EnsureSuccessWithLog(_logger);
        var result = await response.Content.ReadFromJsonAsync<InsertResult>() ?? new InsertResult();
        return result.SuccessfulRows;
    }
}