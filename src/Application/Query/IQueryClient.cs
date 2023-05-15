using System.Text.Json.Serialization;

namespace Aptabase.Application.Query;

public class QueryStatistics
{
    [JsonPropertyName("bytes_read")]
    public int BytesRead { get; set; }
    [JsonPropertyName("rows_read")]
    public int RowsRead { get; set; }
}

public class QueryResult<T>
{
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    [JsonPropertyName("statistics")]
    public QueryStatistics Statistics { get; set; } = new QueryStatistics();
}

public interface IQueryClient
{
    Task<(IEnumerable<T>, QueryStatistics)> QueryAsync<T>(string query, CancellationToken cancellationToken);
    Task<(T, QueryStatistics)> QuerySingleAsync<T>(string query, CancellationToken cancellationToken) where T : new();
}