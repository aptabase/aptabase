using System.Text.Json.Serialization;

namespace Aptabase.Features.Ingestion;

public class InsertResult
{
    [JsonPropertyName("successful_rows")]
    public int SuccessfulRows { get; set; }
    [JsonPropertyName("quarantined_rows")]
    public int QuarantinedRows { get; set; }
}

public interface IIngestionClient
{
    Task<InsertResult> SendSingleAsync(EventRow row, CancellationToken cancellationToken);
    Task<InsertResult> SendMultipleAsync(EventRow[] rows, CancellationToken cancellationToken);
}

