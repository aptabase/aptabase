using Aptabase.Features.Apps;
using Microsoft.Extensions.Caching.Memory;

namespace Aptabase.Features.Ingestion;

public class CachedApplication
{
    public string Id { get; private set; }
    public bool IsLocked { get; private set; }

    public CachedApplication()
    {
        Id = string.Empty;
    }

    public CachedApplication(Application app)
    {
        Id = app.Id;
        IsLocked = app.IsLocked;
    }

    public static CachedApplication Empty => new CachedApplication();
}

public interface IIngestionCache
{
    Task<CachedApplication> FindByAppKey(string appKey, CancellationToken cancellationToken);
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

    public async Task<CachedApplication> FindByAppKey(string appKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            return CachedApplication.Empty;

        var cacheKey = $"APP-KEY-STATUS-{appKey}";
        if (_cache.TryGetValue(cacheKey, out CachedApplication? cachedApp) && cachedApp is not null)
            return cachedApp;

        var app = await _appQueries.GetActiveAppByAppKey(appKey, cancellationToken);
        if (app is null)
        {
            _cache.Set(cacheKey, CachedApplication.Empty, FailureCacheDuration);
            return CachedApplication.Empty;
        }

        if (!app.HasEvents) 
            await _appQueries.MaskAsOnboarded(app.Id, cancellationToken);
        
        cachedApp = new CachedApplication(app);
        _cache.Set(cacheKey, cachedApp, SuccessCacheDuration);
        return cachedApp;
    }
}