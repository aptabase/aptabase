using Aptabase.Features.Stats;

namespace Aptabase.Features.ErrorReporting;

public class TinybirdErrorQueryClient : IErrorQueryClient
{
    private readonly IQueryClient _queryClient;

    public TinybirdErrorQueryClient(IQueryClient queryClient)
    {
        _queryClient = queryClient ?? throw new ArgumentNullException(nameof(queryClient));
    }

    public async Task<IEnumerable<ErrorDto>> GetErrorsAsync(string appId, DateTime startDate, DateTime endDate, string? errorType, string? osName, string? severity, string? kind, int offset, int limit, CancellationToken cancellationToken)
    {
        var args = new
        {
            app_id = appId,
            start_date = startDate,
            end_date = endDate,
            error_type = errorType,
            os_name = osName,
            severity = severity,
            kind = kind,
            offset = offset,
            limit = limit
        };

        return await _queryClient.NamedQueryAsync<ErrorDto>("get_errors__v1", args, cancellationToken);
    }

    public async Task<ErrorDto?> GetErrorByIdAsync(string appId, string errorId, CancellationToken cancellationToken)
    {
        var args = new
        {
            app_id = appId,
            error_id = errorId
        };

        var result = await _queryClient.NamedQueryAsync<ErrorDto>("get_error_by_id__v1", args, cancellationToken);
        return result.FirstOrDefault();
    }

    public async Task<int> GetErrorCountAsync(string appId, DateTime startDate, DateTime endDate, string? errorType, string? osName, string? severity, string? kind, CancellationToken cancellationToken)
    {
        var args = new
        {
            app_id = appId,
            start_date = startDate,
            end_date = endDate,
            error_type = errorType,
            os_name = osName,
            severity = severity,
            kind = kind
        };

        var result = await _queryClient.NamedQuerySingleAsync<ErrorCountDto>("get_error_count__v1", args, cancellationToken);
        return result.Count;
    }

    public async Task<IEnumerable<ErrorTypeDto>> GetErrorTypesAsync(string appId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var args = new
        {
            app_id = appId,
            start_date = startDate,
            end_date = endDate
        };

        return await _queryClient.NamedQueryAsync<ErrorTypeDto>("get_error_types__v1", args, cancellationToken);
    }
}
