using System.Text.Json.Serialization;

namespace Aptabase.Application.Ingestion;

public class InsertResult
{
    [JsonPropertyName("successful_rows")]
    public int SuccessfulRows { get; set; }
    [JsonPropertyName("quarantined_rows")]
    public int QuarantinedRows { get; set; }
}

public interface IIngestionClient
{
    Task<InsertResult> SendSingleAsync(EventHeader header, EventBody body, CancellationToken cancellationToken);
    Task<InsertResult> SendMultipleAsync(EventHeader header, EventBody[] body, CancellationToken cancellationToken);
}

