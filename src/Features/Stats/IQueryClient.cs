namespace Aptabase.Features.Stats;

public record QueryStatistics(double Elapsed, long RowsRead, long BytesRead);

public record QueryResponse<T>(IReadOnlyList<T> Rows, QueryStatistics? Statistics);

public interface IQueryClient
{
    Task<IEnumerable<T>> NamedQueryAsync<T>(string name, object args, CancellationToken cancellationToken);
    Task<T> NamedQuerySingleAsync<T>(string name, object args, CancellationToken cancellationToken) where T : new();
    Task<Stream> StreamResponseAsync(string query, CancellationToken cancellationToken);

    /// <summary>
    /// The entry point for query API traffic. Arguments are strings only, so nothing
    /// user-derived can reach a query as a raw, unescaped value, and Tinybird requests go
    /// out with the dedicated API token. Statistics are filled when the backend reports them.
    /// </summary>
    Task<QueryResponse<T>> ApiQueryAsync<T>(string name, IReadOnlyDictionary<string, string?> args, CancellationToken cancellationToken);
}
