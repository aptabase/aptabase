using System.Text.Json.Serialization;

namespace Aptabase.Features.ErrorReporting.Buffer;

public class InsertErrorResult
{
    [JsonPropertyName("successful_rows")]
    public int SuccessfulRows { get; set; }
    [JsonPropertyName("quarantined_rows")]
    public int QuarantinedRows { get; set; }
}

public class TinybirdErrorIngestionClient : IErrorIngestionClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly TimeSpan[] _retriesDelay =
    [
        TimeSpan.FromMilliseconds(1000),
        TimeSpan.FromMilliseconds(3000),
        TimeSpan.FromMilliseconds(5000),
    ];

    public TinybirdErrorIngestionClient(IHttpClientFactory factory, ILogger<TinybirdErrorIngestionClient> logger)
    {
        _httpClient = factory.CreateClient("Tinybird");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static string ErrorsPath => $"/v0/events?name=error_events&wait=true";

    public Task<long> BulkSendErrorAsync(IEnumerable<ErrorRow> rows, CancellationToken ct = default)
    {
        return PostAsync(ErrorsPath, rows, ct);
    }

    private async Task<long> PostAsync(string path, IEnumerable<ErrorRow> rows, CancellationToken ct = default)
    {
        using var content = SerializeBody(rows);

        for (var i = 0; i < _retriesDelay.Length; i++)
        {
            try
            {
                var response = await _httpClient.PostAsync(path, content, ct);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<InsertErrorResult>() ?? new InsertErrorResult();
                if (result.QuarantinedRows > 0)
                    _logger.LogWarning("Tinybird quarantined {QuarantinedRows} error rows ({SuccessfulRows} rows ingested successfully). Check the error_events quarantine datasource for schema mismatches.", result.QuarantinedRows, result.SuccessfulRows);

                return result.SuccessfulRows;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send errors to Tinybird. Will retry again after {Delay}ms.", _retriesDelay[i].TotalMilliseconds);
                await Task.Delay(_retriesDelay[i]);
            }
        }

        throw new Exception($"Failed to send errors to Tinybird after {_retriesDelay.Length} retries.");
    }

    private static StringContent SerializeBody(IEnumerable<ErrorRow> rows)
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
