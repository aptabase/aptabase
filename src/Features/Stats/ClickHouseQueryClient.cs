using Dapper;
using Scriban;
using ClickHouse.Client.ADO;
using System.Collections.Concurrent;

namespace Aptabase.Features.Stats;

public class ClickHouseQueryClient : IQueryClient
{
    private readonly ClickHouseConnection _conn;
    private readonly EnvSettings _env;

    public ClickHouseQueryClient(ClickHouseConnection conn, EnvSettings env)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _conn = conn ?? throw new ArgumentNullException(nameof(conn));
    }

    public async Task<IEnumerable<T>> NamedQueryAsync<T>(string name, object args, CancellationToken cancellationToken)
    {
        var dict = args.GetType().GetProperties().ToDictionary(p => p.Name, p => FormatArg(p.GetValue(args, null)));
        var template = await ReadNamedQuery(name);
        var query = await template.RenderAsync(dict);
        return await _conn.QueryAsync<T>(query, cancellationToken);
    }

    public async Task<T> NamedQuerySingleAsync<T>(string name, object args, CancellationToken cancellationToken) where T : new()
    {
        var rows = await NamedQueryAsync<T>(name, args, cancellationToken);
        return rows.FirstOrDefault() ?? new T();
    }

    private readonly ConcurrentDictionary<string, Template> _namedQueries = new();
    private async Task<Template> ReadNamedQuery(string name)
    {
        if (_namedQueries.ContainsKey(name))
            return _namedQueries[name];

        var pathToQuery = Path.Combine(_env.EtcDirectoryPath, "clickhouse", "queries", $"{name}.liquid");
        var content = await File.ReadAllTextAsync(pathToQuery);
        var template = Template.ParseLiquid(content);

        _namedQueries[name] = template;
        return template;
    }

    private string? FormatArg(object? value)
    {
        return value switch
        {
            string[] s => string.Join("','", s),
            DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss"),
            null => null,
            _ => $"{value}",
        };
    }
}