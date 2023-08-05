namespace Aptabase.Features.Stats;

public interface IQueryClient
{
    Task<IEnumerable<T>> NamedQueryAsync<T>(string name, object args, CancellationToken cancellationToken);
    Task<T> NamedQuerySingleAsync<T>(string name, object args, CancellationToken cancellationToken) where T : new();
    Task<IEnumerable<T>> QueryAsync<T>(string query, CancellationToken cancellationToken);
    Task<T> QuerySingleAsync<T>(string query, CancellationToken cancellationToken) where T : new();
}