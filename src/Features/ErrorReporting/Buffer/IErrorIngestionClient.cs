namespace Aptabase.Features.ErrorReporting.Buffer;

public interface IErrorIngestionClient
{
    Task<long> BulkSendErrorAsync(IEnumerable<ErrorRow> rows, CancellationToken ct = default);
}
