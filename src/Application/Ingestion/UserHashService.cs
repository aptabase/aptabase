using Aptabase.Data;
using System.Security.Cryptography;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace Aptabase.Application.Ingestion;

public interface IUserHashService
{
    Task<string> CalculateHash(DateTime timestamp, string appId, string clientIP, string userAgent);
}

public class DailyUserHashService : IUserHashService
{
    private readonly IMemoryCache _cache;
    private readonly IDbConnectionFactory _dbFactory;

    public DailyUserHashService(IMemoryCache cache, IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<string> CalculateHash(DateTime timestamp, string appId, string clientIP, string userAgent)
    {
        var salt = await GetSaltFor(timestamp.Date.ToString("yyyy-MM-dd"), appId);
        var bytes = Encoding.UTF8.GetBytes($"{clientIP}|${userAgent}");
        var id = SHA256.HashData(bytes.Concat(salt).ToArray());
        return Convert.ToHexString(id);
    }

    private async Task<byte[]> GetSaltFor(string date, string appId)
    {
        var cacheKey = $"DAILY-SALT-{appId}-{date}";
        if (_cache.TryGetValue(cacheKey, out byte[]? cachedSalt) && cachedSalt != null)
            return cachedSalt;


        var storedSalt = await ReadOrCreateSalt(date, appId);
        _cache.Set(cacheKey, storedSalt, TimeSpan.FromDays(2));
        return storedSalt;
    }

    private async Task<byte[]> ReadOrCreateSalt(string date, string appId)
    {
        using var db = _dbFactory.Create();
        var newSalt = RandomNumberGenerator.GetBytes(16);
        await db.ExecuteAsync($"INSERT INTO app_salts (app_id, date, salt) VALUES (@appId, @date, @newSalt) ON CONFLICT DO NOTHING", new { appId, date, newSalt });
        return await db.ExecuteScalarAsync<byte[]>($"SELECT salt FROM app_salts WHERE app_id = @appId AND date = @date", new { appId, date });
    }
}