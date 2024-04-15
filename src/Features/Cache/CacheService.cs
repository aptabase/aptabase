using Aptabase.Data;
using Dapper;

namespace Features.Cache;

public interface ICache
{
    Task<string?> Get(string key);
    Task Set(string key, string value, TimeSpan ttl);
    Task<bool> Exists(string key);
}

public class DatabaseCache : ICache
{
    private readonly IDbContext _db;

    public DatabaseCache(IDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<string?> Get(string key)
    {
        return await _db.Connection.ExecuteScalarAsync<string>(
            "SELECT value FROM cache WHERE key = @key AND expires_at > now()",
            new { key });
    }

    public async Task Set(string key, string value, TimeSpan ttl)
    {
        var expiresAt = DateTime.UtcNow.Add(ttl);
        await _db.Connection.ExecuteAsync(
            "INSERT INTO cache (key, value, expires_at) VALUES (@key, @value, @expiresAt) " +
            "ON CONFLICT (key) DO UPDATE SET value = @value, expires_at = @expiresAt",
            new { key, value, expiresAt });
    }

    public async Task<bool> Exists(string key)
    {
        var value = await Get(key);
        return !string.IsNullOrEmpty(value);
    }
}