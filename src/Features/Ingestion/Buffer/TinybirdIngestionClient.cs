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
    private readonly TimeSpan[] _retriesDelay = new []{
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromMilliseconds(1000),
    };

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
        using var content = SerializeBody(rows);

        for (var i = 0; i < _retriesDelay.Length; i++)
        {
            try
            {
                var response = await _httpClient.PostAsync(path, content);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<InsertResult>() ?? new InsertResult();
                return result.SuccessfulRows;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send events to Tinybird. Will retry again after {Delay}ms.", _retriesDelay[i].TotalMilliseconds);
                await Task.Delay(_retriesDelay[i]);
            }
        }

        throw new Exception($"Failed to send events to Tinybird after {_retriesDelay.Length} retries.");
    }

    private static HttpContent SerializeBody(IEnumerable<EventRow> rows)
    {
        using var writer = new StringWriter();
        foreach (var row in rows)
        {
            row.WriteJson(writer);
            writer.Write("\n");
        }

        return new StringContent(writer.ToString());
    }
}