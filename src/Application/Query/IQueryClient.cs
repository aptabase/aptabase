namespace Aptabase.Application.Query;

public interface IQueryClient
{
    Task<IEnumerable<T>> NamedQueryAsync<T>(string name, KeyValuePair<string, string>[] args, CancellationToken cancellationToken);
    Task<T> NamedQuerySingleAsync<T>(string name, KeyValuePair<string, string>[] args, CancellationToken cancellationToken) where T : new();
    Task<IEnumerable<T>> QueryAsync<T>(string query, CancellationToken cancellationToken);
    Task<T> QuerySingleAsync<T>(string query, CancellationToken cancellationToken) where T : new();
}