namespace Aptabase.Features.Ingestion;

public interface IIngestionClient
{
    Task<long> SendEventAsync(EventRow row, CancellationToken cancellationToken);
    Task<long> BulkSendEventAsync(IEnumerable<EventRow> rows, CancellationToken cancellationToken);
}

