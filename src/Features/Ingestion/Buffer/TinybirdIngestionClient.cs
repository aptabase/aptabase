using System.Text.Json.Serialization;

namespace Aptabase.Features.Ingestion.Buffer;

public class InsertResult
{
    [JsonPropertyName("successful_rows")]
    public int SuccessfulRows { get; set; }
    [JsonPropertyName("quarantined_rows")]
    public int QuarantinedRows { get; set; }
}

public class TinybirdIngestionClient : IIngestionClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public TinybirdIngestionClient(IHttpClientFactory factory, ILogger<TinybirdIngestionClient> logger)
    {
        _httpClient = factory.CreateClient("Tinybird");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static string EventsPath => $"/v0/events?name=events&wait=true";

    public Task<long> SendEventAsync(EventRow row)
    {
        return PostAsync(EventsPath, new[] { row });
    }

    public Task<long> BulkSendEventAsync(IEnumerable<EventRow> rows)
    {
        return PostAsync(EventsPath, rows);
    }

    private async Task<long> PostAsync(string path, IEnumerable<EventRow> rows)
    {
        using var writer = new StringWriter();
        foreach (var row in rows)
        {
            row.WriteJson(writer);
            writer.Write("\n");
        }

        using var content = new StringContent(writer.ToString());
        var response = await _httpClient.PostAsync(path, content);
        
        await response.EnsureSuccessWithLog(_logger);
        var result = await response.Content.ReadFromJsonAsync<InsertResult>() ?? new InsertResult();
        return result.SuccessfulRows;
    }
}