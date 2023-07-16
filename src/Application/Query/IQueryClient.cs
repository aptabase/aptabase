namespace Aptabase.Application.Query;

public interface IQueryClient
{
    Task<IEnumerable<T>> QueryAsync<T>(string query, CancellationToken cancellationToken);
    Task<T> QuerySingleAsync<T>(string query, CancellationToken cancellationToken) where T : new();
}