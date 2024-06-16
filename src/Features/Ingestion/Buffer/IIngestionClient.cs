namespace Aptabase.Features.Ingestion.Buffer;

public interface IIngestionClient
{
    Task<long> SendEventAsync(EventRow row);
    Task<long> BulkSendEventAsync(IEnumerable<EventRow> rows, CancellationToken ct = default);
}

