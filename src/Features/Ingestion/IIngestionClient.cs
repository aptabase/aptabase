namespace Aptabase.Features.Ingestion;

public interface IIngestionClient
{
    Task<long> SendSingleAsync(EventRow row, CancellationToken cancellationToken);
    Task<long> SendMultipleAsync(EventRow[] rows, CancellationToken cancellationToken);
}

