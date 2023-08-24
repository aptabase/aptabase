using Aptabase.Features.Apps;
using ClickHouse.Client.Utility;
using Microsoft.Extensions.Caching.Memory;

namespace Aptabase.Features.Ingestion;

public interface IIngestionCache
{
    Task<string> FindByAppKey(string appKey, CancellationToken cancellationToken);
}

public class IngestionCache : IIngestionCache
{
    private readonly IMemoryCache _cache;
    private readonly IAppQueries _appQueries;

    private readonly TimeSpan SuccessCacheDuration = TimeSpan.FromMinutes(30);
    private readonly TimeSpan FailureCacheDuration = TimeSpan.FromMinutes(5);

    public IngestionCache(IMemoryCache cache, IAppQueries appQueries)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _appQueries = appQueries ?? throw new ArgumentNullException(nameof(appQueries));
    }

    public async Task<string> FindByAppKey(string appKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            return string.Empty;

        var cacheKey = $"APP-KEY-STATUS-{appKey}";
        if (_cache.TryGetValue(cacheKey, out string? cachedAppId))
            return string.IsNullOrEmpty(cachedAppId) ? string.Empty : cachedAppId;

        var app = await _appQueries.GetActiveAppByAppKey(appKey, cancellationToken);
        if (app is null)
        {
            _cache.Set(cacheKey, string.Empty, FailureCacheDuration);
            return string.Empty;
        }

        if (!app.HasEvents) 
            await _appQueries.MaskAsOnboarded(app.Id, cancellationToken);
        
        _cache.Set(cacheKey, app.Id, SuccessCacheDuration);
        return app.Id;
    }
}