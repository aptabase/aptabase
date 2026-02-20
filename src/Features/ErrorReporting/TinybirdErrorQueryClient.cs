using Aptabase.Features.Stats;

namespace Aptabase.Features.ErrorReporting;

public class TinybirdErrorQueryClient : IErrorQueryClient
{
    private readonly IQueryClient _queryClient;

    public TinybirdErrorQueryClient(IQueryClient queryClient)
    {
        _queryClient = queryClient ?? throw new ArgumentNullException(nameof(queryClient));
    }

    public async Task<IEnumerable<ErrorDto>> GetErrorsAsync(string appId, DateTime startDate, DateTime endDate, string? errorType, string? osName, int offset, int limit, CancellationToken cancellationToken)
    {
        var args = new
        {
            app_id = appId,
            start_date = startDate,
            end_date = endDate,
            error_type = errorType,
            os_name = osName,
            offset = offset,
            limit = limit
        };

        return await _queryClient.NamedQueryAsync<ErrorDto>("get_errors", args, cancellationToken);
    }

    public async Task<ErrorDto?> GetErrorByIdAsync(string appId, string errorId, CancellationToken cancellationToken)
    {
        var args = new
        {
            app_id = appId,
            error_id = errorId
        };

        var result = await _queryClient.NamedQueryAsync<ErrorDto>("get_error_by_id", args, cancellationToken);
        return result.FirstOrDefault();
    }

    public async Task<int> GetErrorCountAsync(string appId, DateTime startDate, DateTime endDate, string? errorType, string? platform, CancellationToken cancellationToken)
    {
        var args = new
        {
            app_id = appId,
            start_date = startDate,
            end_date = endDate,
            error_type = errorType,
            platform = platform
        };

        var result = await _queryClient.NamedQuerySingleAsync<ErrorCountDto>("get_error_count", args, cancellationToken);
        return result.Count;
    }
}
