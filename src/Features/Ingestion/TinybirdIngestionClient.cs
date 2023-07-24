using System.Text.Json;

namespace Aptabase.Features.Ingestion;

public class TinybirdIngestionClient : IIngestionClient
{
    private static readonly JsonSerializerOptions JsonSettings = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private HttpClient _httpClient;
    private ILogger _logger;

    public TinybirdIngestionClient(IHttpClientFactory factory, ILogger<TinybirdIngestionClient> logger)
    {
        _httpClient = factory.CreateClient("Tinybird");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private string EventsPath => $"/v0/events?name=events";

    public async Task<InsertResult> SendSingleAsync(EventRow row, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(EventsPath, row, JsonSettings, cancellationToken);
        
        await response.EnsureSuccessWithLog(_logger);
        return await response.Content.ReadFromJsonAsync<InsertResult>() ?? new InsertResult();
    }

    public async Task<InsertResult> SendMultipleAsync(EventRow[] rows, CancellationToken cancellationToken)
    {
        var rowsAsString = rows.Select(row => JsonSerializer.Serialize(row, JsonSettings));
        var content = new StringContent(string.Join('\n', rowsAsString));
        var response = await _httpClient.PostAsync(EventsPath, content, cancellationToken);
        
        await response.EnsureSuccessWithLog(_logger);
        return await response.Content.ReadFromJsonAsync<InsertResult>() ?? new InsertResult();
    }
}