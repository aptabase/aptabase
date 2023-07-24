using System.Data.Common;
using Dapper;

namespace Aptabase.Data;

public interface IDbContext
{
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
    Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default);
    Task<object> ExecuteScalarAsync(string sql, object? param = null, CancellationToken cancellationToken = default);
    Task<T> ExecuteScalarAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
    Task<T> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
    Task<T> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
}

public class DbContext : IDbContext
{
    private readonly DbDataSource _ds;

    public DbContext(DbDataSource ds)
    {
        _ds = ds;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var conn = _ds.CreateConnection();
        var cmd = new CommandDefinition(sql, param, cancellationToken: cancellationToken);
        return await conn.QueryAsync<T>(cmd);
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var conn = _ds.CreateConnection();
        var cmd = new CommandDefinition(sql, param, cancellationToken: cancellationToken);
        return await conn.ExecuteAsync(cmd);
    }

    public async Task<object> ExecuteScalarAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var conn = _ds.CreateConnection();
        var cmd = new CommandDefinition(sql, param, cancellationToken: cancellationToken);
        return await conn.ExecuteScalarAsync(cmd);
    }

    public async Task<T> ExecuteScalarAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var conn = _ds.CreateConnection();
        var cmd = new CommandDefinition(sql, param, cancellationToken: cancellationToken);
        return await conn.ExecuteScalarAsync<T>(cmd);
    }

    public async Task<T> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var conn = _ds.CreateConnection();
        var cmd = new CommandDefinition(sql, param, cancellationToken: cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<T>(cmd);
    }

    public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var conn = _ds.CreateConnection();
        var cmd = new CommandDefinition(sql, param, cancellationToken: cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<T>(cmd);
    }
}