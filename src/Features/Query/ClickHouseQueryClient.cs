using Dapper;
using ClickHouse.Client.ADO;

namespace Aptabase.Features.Query;

public class ClickHouseQueryClient : IQueryClient
{
    private readonly ClickHouseConnection _conn;
    private readonly EnvSettings _env;

    public ClickHouseQueryClient(ClickHouseConnection conn, EnvSettings env)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _conn = conn ?? throw new ArgumentNullException(nameof(conn));
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
        // TODO: replace this with parameterized views when this is fixed: https://github.com/ClickHouse/ClickHouse/issues/53004

        var query = await ReadNamedQuery(name);
        foreach (var prop in args.GetType().GetProperties())
        {
            var value = prop.GetValue(args, null);
            if (value is null)
                query = query.Replace($"{{{prop.Name}}}", "NULL");
            else
                query = query.Replace($"{{{prop.Name}}}", FormatArg(value));
        }
        return await _conn.QueryAsync<T>(query, cancellationToken);
    }

    public async Task<T> NamedQuerySingleAsync<T>(string name, object args, CancellationToken cancellationToken) where T : new()
    {
        var rows = await NamedQueryAsync<T>(name, args, cancellationToken);
        return rows.FirstOrDefault() ?? new T();
    }

    private async Task<string> ReadNamedQuery(string name)
    {
        var pathToQuery = Path.Combine(_env.EtcDirectoryPath, "clickhouse", "queries", $"{name}.sql");
        return await File.ReadAllTextAsync(pathToQuery);
    }

    private string FormatArg(object value)
    {
        return value switch
        {
            string[] s => $"'${string.Join("','", s)}'",
            DateTime d => $"'{d:yyyy-MM-dd HH:mm:ss}'",
            _ => $"'{value}'",
        };
    }
}