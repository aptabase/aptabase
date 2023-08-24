using Aptabase.Features.Apps;
using ClickHouse.Client.Utility;
using Microsoft.Extensions.Caching.Memory;

namespace Aptabase.Features.Ingestion;

public record CachedApplication
{
    public string Id { get; set; } = "";
    public bool HasEvents { get; set; } = false;

    public static CachedApplication Empty => new();

    public CachedApplication()
    {

    }

    public CachedApplication(Application app)
    {
        Id = app.Id;
        HasEvents = app.HasEvents;
    }
}

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
        if (_cache.TryGetValue(cacheKey, out CachedApplication? cachedApp) && cachedApp is not null)
        {
            if (string.IsNullOrEmpty(cachedApp.Id))
                return string.Empty;

            // if the app never had events before, we need to set the flag in the database and update the cache
            var changed = await CheckOnbordingStatus(cachedApp, cancellationToken);
            if (changed)
                _cache.Set(cacheKey, cachedApp, SuccessCacheDuration);

            return cachedApp.Id;
        }

        var app = await _appQueries.GetActiveAppByAppKey(appKey, cancellationToken);
        if (app is null)
        {
            _cache.Set(cacheKey, CachedApplication.Empty, FailureCacheDuration);
            return string.Empty;
        }

        cachedApp = new CachedApplication(app);
        await CheckOnbordingStatus(cachedApp, cancellationToken);
        _cache.Set(cacheKey, cachedApp, SuccessCacheDuration);
        return app.Id;
    }

    private async Task<bool> CheckOnbordingStatus(CachedApplication app, CancellationToken cancellationToken)
    {
        if (app.HasEvents)
            return false;

        await _appQueries.MaskAsOnboarded(app.Id, cancellationToken);
        app.HasEvents = true;
        return true;
    }
}