using System.Text;
using Microsoft.Extensions.Caching.Memory;
using FastHashes;

namespace Aptabase.Features.Privacy;

public interface IUserHasher
{
    Task<string> CalculateHash(DateTime timestamp, string appId, string sessionId, string clientIP, string userAgent);
}

public class DailyUserHasher : IUserHasher
{
    // After a failed salt lookup, callers queued behind it fail immediately instead of each waiting out
    // its own connection timeout, which would stall the whole flush during a database outage.
    // Kept shorter than the ingestion flush interval so the next flush retries.
    private static readonly TimeSpan SaltRetryDelay = TimeSpan.FromSeconds(5);

    private readonly IMemoryCache _cache;
    private readonly IPrivacyQueries _privacyQueries;
    private readonly TimeProvider _time;

    // Salt cache misses come in bursts (every app at once when the UTC date rolls over, or on a cold start)
    // while every event of a flush is hashed concurrently. Loading salts one at a time means each one is
    // read once and at most one Postgres connection is used, instead of one per event.
    private readonly SemaphoreSlim _saltLock = new(1, 1);
    private DateTimeOffset _lastSaltFailure = DateTimeOffset.MinValue;

    public DailyUserHasher(IMemoryCache cache, IPrivacyQueries privacyQueries, TimeProvider? time = null)
    {
        _privacyQueries = privacyQueries ?? throw new ArgumentNullException(nameof(privacyQueries));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _time = time ?? TimeProvider.System;
    }

    public async Task<string> CalculateHash(DateTime timestamp, string appId, string sessionId, string clientIP, string userAgent)
    {
        var cacheKey = $"USERID-{appId}-{sessionId}";

        // If we already have a cached user ID for this session, return it immediately
        // This avoid issues with the user ID changing in the middle of a session because of an IP change
        if (_cache.TryGetValue(cacheKey, out string? userId) && !string.IsNullOrEmpty(userId))
            return userId;

        var salt = await GetSaltFor(timestamp.Date.ToString("yyyy-MM-dd"), appId);
        var bytes = Encoding.UTF8.GetBytes($"{clientIP}|${userAgent}");
        var hash = ComputeHash(salt, bytes);
        userId = Convert.ToHexString(hash);

        _cache.Set(cacheKey, userId, TimeSpan.FromHours(48));
        return userId;
    }

    private static byte[] ComputeHash(Span<byte> salt, byte[] bytes)
    {
        var key1 = BitConverter.ToUInt64(salt[..8]);
        var key2 = BitConverter.ToUInt64(salt[0..]);
        var hasher = new SipHash(SipHashVariant.V24, key1, key2);
        return hasher.ComputeHash(bytes);
    }

    private async Task<byte[]> GetSaltFor(string date, string appId)
    {
        var cacheKey = $"DAILY-SALT-{appId}-{date}";
        if (_cache.TryGetValue(cacheKey, out byte[]? cachedSalt) && cachedSalt != null)
            return cachedSalt;

        await _saltLock.WaitAsync();
        try
        {
            // Another caller may have loaded it while we were waiting for the lock
            if (_cache.TryGetValue(cacheKey, out cachedSalt) && cachedSalt != null)
                return cachedSalt;

            if (_time.GetUtcNow() - _lastSaltFailure < SaltRetryDelay)
                throw new InvalidOperationException("Skipped salt lookup because a recent one failed.");

            byte[] storedSalt;
            try
            {
                storedSalt = await _privacyQueries.ReadOrCreateSalt(appId, date);
            }
            catch
            {
                _lastSaltFailure = _time.GetUtcNow();
                throw;
            }

            _cache.Set(cacheKey, storedSalt, TimeSpan.FromDays(2));
            return storedSalt;
        }
        finally
        {
            _saltLock.Release();
        }
    }
}