using Dapper;
using ClickHouse.Client.ADO;

namespace Aptabase.Features.Query;

public class ClickHouseQueryClient : IQueryClient
{
    private ClickHouseConnection _conn;
    private ILogger _logger;

    public ClickHouseQueryClient(ClickHouseConnection conn, ILogger<ClickHouseQueryClient> logger)
    {
        _conn = conn ?? throw new ArgumentNullException(nameof(conn));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string query, CancellationToken cancellationToken)
    {
        return await _conn.QueryAsync<T>(query, cancellationToken);
    }

    public async Task<T> QuerySingleAsync<T>(string query, CancellationToken cancellationToken) where T : new()
    {
        return await _conn.QueryFirstOrDefaultAsync<T>(query, cancellationToken);
    }

    public async Task<IEnumerable<T>> NamedQueryAsync<T>(string name, object args, CancellationToken cancellationToken)
    {
        var argsString = string.Join(", ", args.GetType().GetProperties().Select(x => $"p_{x.Name}={FormatArg(x.GetValue(args, null))}"));
        return await _conn.QueryAsync<T>($"SELECT * FROM {name}({argsString})", cancellationToken);
    }

    public async Task<T> NamedQuerySingleAsync<T>(string name, object args, CancellationToken cancellationToken) where T : new()
    {
        var rows = await NamedQueryAsync<T>(name, args, cancellationToken);
        return rows.FirstOrDefault() ?? new T();
    }

    private string FormatArg(object? value)
    {
        switch (value)
        {
            case string[] s:
                return $"['${string.Join("','", s)}']";
            case DateTime d:
                return $"'{d:yyyy-MM-dd HH:mm:ss}'";
            default:
                return $"'{value}'";
        }
    }
}