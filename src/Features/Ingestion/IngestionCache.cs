using System.Collections.Concurrent;
using Aptabase.Features.Apps;
using Microsoft.Extensions.Caching.Memory;

namespace Aptabase.Features.Ingestion;

public class CachedApplication
{
    public string Id { get; private set; }
    public bool IsLocked { get; private set; }
    public int RetentionMonths { get; private set; } = DataRetention.DefaultMonths;

    public CachedApplication()
    {
        Id = string.Empty;
    }

    public CachedApplication(Application app)
    {
        Id = app.Id;
        IsLocked = app.LockReason.HasValue;
        RetentionMonths = app.RetentionMonths;
    }

    public CachedApplication(CachedApplication app, int retentionMonths)
    {
        Id = app.Id;
        IsLocked = app.IsLocked;
        RetentionMonths = retentionMonths;
    }

    public static CachedApplication Empty => new CachedApplication();
}

public interface IIngestionCache
{
    Task<CachedApplication> FindByAppKey(string appKey, CancellationToken cancellationToken);
    void MarkAsDeleted(string appKey);
}

public class IngestionCache : IIngestionCache
{
    private readonly IMemoryCache _cache;
    private readonly IAppQueries _appQueries;

    private readonly TimeSpan SuccessCacheDuration = TimeSpan.FromMinutes(30);
    private readonly TimeSpan FailureCacheDuration = TimeSpan.FromMinutes(5);

    // When a busy app's entry expires (or on a fresh instance), every in-flight request for that key misses
    // at once. Concurrent misses for the same key share one lookup instead of each opening a Postgres connection.
    private readonly ConcurrentDictionary<string, SharedLookup> _lookups = new();

    public IngestionCache(IMemoryCache cache, IAppQueries appQueries)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _appQueries = appQueries ?? throw new ArgumentNullException(nameof(appQueries));
    }

    public void MarkAsDeleted(string appKey)
    {
        _cache.Set($"APP-KEY-STATUS-{appKey}", CachedApplication.Empty, FailureCacheDuration);
    }

    public async Task<CachedApplication> FindByAppKey(string appKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            return CachedApplication.Empty;

        var cacheKey = $"APP-KEY-STATUS-{appKey}";
        if (_cache.TryGetValue(cacheKey, out CachedApplication? cachedApp) && cachedApp is not null)
            return cachedApp;

        while (true)
        {
            var lookup = _lookups.GetOrAdd(appKey, _ => new SharedLookup());
            if (!lookup.TryJoin(token => LoadAndCache(appKey, cacheKey, lookup, token), out var task))
            {
                // Every caller of this lookup left before it finished and it was cancelled, so start a new one
                _lookups.TryRemove(KeyValuePair.Create(appKey, lookup));
                continue;
            }

            try
            {
                return await task.WaitAsync(cancellationToken);
            }
            finally
            {
                lookup.Leave();
            }
        }
    }

    private async Task<CachedApplication> LoadAndCache(string appKey, string cacheKey, SharedLookup lookup, CancellationToken cancellationToken)
    {
        try
        {
            // A caller can see a miss, then resume after the previous lookup has filled the cache and removed itself
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
        finally
        {
            // Removed only after the cache is set (or the lookup failed, so failures aren't remembered)
            _lookups.TryRemove(KeyValuePair.Create(appKey, lookup));
        }
    }

    // A lookup shared by every concurrent caller for one key. One caller leaving (e.g. its client disconnected)
    // never fails the others, but once all of them have left the lookup is cancelled, so abandoned lookups stop
    // holding or queueing for pool connections instead of running until the database timeouts.
    private sealed class SharedLookup
    {
        private readonly object _lock = new();
        private readonly CancellationTokenSource _cts = new();
        private Task<CachedApplication>? _task;
        private int _waiters;
        private bool _abandoned;

        public bool TryJoin(Func<CancellationToken, Task<CachedApplication>> load, out Task<CachedApplication> task)
        {
            lock (_lock)
            {
                if (_abandoned)
                {
                    task = Task.FromCanceled<CachedApplication>(_cts.Token);
                    return false;
                }

                _waiters++;
                task = _task ??= load(_cts.Token);
                return true;
            }
        }

        public void Leave()
        {
            lock (_lock)
            {
                _waiters--;
                if (_waiters > 0 || _task!.IsCompleted)
                    return;

                _abandoned = true;
            }

            // Outside the lock: cancellation callbacks run synchronously on this thread
            _cts.Cancel();
        }
    }
}
