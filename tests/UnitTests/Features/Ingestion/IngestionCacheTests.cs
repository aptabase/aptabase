using Xunit;
using Moq;
using Aptabase.Features.Ingestion;
using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Aptabase.Features.Apps;

namespace Aptabase.UnitTests.Features.Ingestion;

public class IngestionCacheTests
{
    private readonly Mock<IAppQueries> _queries;
    private readonly MemoryCache _cache;
    private readonly IngestionCache _sut;

    public IngestionCacheTests()
    {
        _queries = new Mock<IAppQueries>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new IngestionCache(_cache, _queries.Object);
    }

    [Fact]
    public async Task Returns_Empty_For_Unkown_AppKeys()
    {
        _queries.Setup(q => q.GetActiveAppByAppKey(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Application?)null);

        var app = await _sut.FindByAppKey("A-DEV-000", default);
        app.Id.Should().Be(string.Empty);
        _cache.Count.Should().Be(1);

        var app2 = await _sut.FindByAppKey("A-DEV-000", default);
        app2.Id.Should().Be(string.Empty);
        _cache.Count.Should().Be(1);

        _queries.Verify(q => q.GetActiveAppByAppKey(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
        _queries.Verify(q => q.MaskAsOnboarded(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Should_Cache_Result()
    {
        _queries.Setup(q => q.GetActiveAppByAppKey("A-DEV-000", It.IsAny<CancellationToken>())).ReturnsAsync(new Application { Id = "1234" });

        var app = await _sut.FindByAppKey("A-DEV-000", default);
        app.Id.Should().Be("1234");
        _cache.Count.Should().Be(1);

        var app2 = await _sut.FindByAppKey("A-DEV-000", default);
        app2.Id.Should().Be("1234");
        _cache.Count.Should().Be(1);

        _queries.Verify(q => q.GetActiveAppByAppKey(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
        _queries.Verify(q => q.MaskAsOnboarded(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Retention_Refreshes_After_Thirty_Minutes_Without_Changing_Existing_Snapshots()
    {
        var clock = new TestClock();
        using var cache = new MemoryCache(new MemoryCacheOptions { Clock = clock });
        var sut = new IngestionCache(cache, _queries.Object);
        var storedApp = new Application { Id = "1234", HasEvents = true, RetentionMonths = 24 };
        _queries.Setup(q => q.GetActiveAppByAppKey("A-DEV-000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedApp);
        var original = await sut.FindByAppKey("A-DEV-000", default);

        storedApp.RetentionMonths = 12;
        clock.UtcNow = clock.UtcNow.AddMinutes(29);
        (await sut.FindByAppKey("A-DEV-000", default)).RetentionMonths.Should().Be(24);
        _queries.Verify(q => q.GetActiveAppByAppKey(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());

        clock.UtcNow = clock.UtcNow.AddMinutes(1);
        (await sut.FindByAppKey("A-DEV-000", default)).RetentionMonths.Should().Be(12);
        original.RetentionMonths.Should().Be(24);
        _queries.Verify(q => q.GetActiveAppByAppKey(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Should_Share_One_Lookup_For_Concurrent_Misses()
    {
        var lookup = new TaskCompletionSource<Application?>();
        _queries.Setup(q => q.GetActiveAppByAppKey("A-DEV-000", It.IsAny<CancellationToken>())).Returns(lookup.Task);

        var calls = Enumerable.Range(0, 200).Select(_ => _sut.FindByAppKey("A-DEV-000", default)).ToArray();
        lookup.SetResult(new Application { Id = "1234" });
        var apps = await Task.WhenAll(calls);

        apps.Should().AllSatisfy(app => app.Id.Should().Be("1234"));
        _queries.Verify(q => q.GetActiveAppByAppKey(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
        _queries.Verify(q => q.MaskAsOnboarded(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Should_Not_Cancel_Shared_Lookup_When_One_Caller_Cancels()
    {
        var lookup = new TaskCompletionSource<Application?>();
        var lookupToken = CancellationToken.None;
        _queries
            .Setup(q => q.GetActiveAppByAppKey("A-DEV-000", It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, token) => lookupToken = token)
            .Returns(lookup.Task);

        using var cts = new CancellationTokenSource();
        var canceled = _sut.FindByAppKey("A-DEV-000", cts.Token);
        var other = _sut.FindByAppKey("A-DEV-000", default);

        cts.Cancel();
        await FluentActions.Awaiting(() => canceled).Should().ThrowAsync<OperationCanceledException>();
        lookupToken.IsCancellationRequested.Should().BeFalse();

        lookup.SetResult(new Application { Id = "1234" });
        (await other).Id.Should().Be("1234");
        _queries.Verify(q => q.GetActiveAppByAppKey(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Should_Cancel_Shared_Lookup_Once_Every_Caller_Has_Left()
    {
        var tokens = new List<CancellationToken>();
        _queries
            .Setup(q => q.GetActiveAppByAppKey("A-DEV-000", It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken token) =>
            {
                tokens.Add(token);

                // The first lookup waits (e.g. queued for a pool connection) until it is cancelled
                if (tokens.Count == 1)
                    await Task.Delay(Timeout.Infinite, token);

                return new Application { Id = "1234" };
            });

        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();
        var first = _sut.FindByAppKey("A-DEV-000", cts1.Token);
        var second = _sut.FindByAppKey("A-DEV-000", cts2.Token);

        cts1.Cancel();
        await FluentActions.Awaiting(() => first).Should().ThrowAsync<OperationCanceledException>();
        tokens[0].IsCancellationRequested.Should().BeFalse();

        cts2.Cancel();
        await FluentActions.Awaiting(() => second).Should().ThrowAsync<OperationCanceledException>();
        tokens[0].IsCancellationRequested.Should().BeTrue();

        // A later request starts a fresh lookup instead of joining the cancelled one
        var app = await _sut.FindByAppKey("A-DEV-000", default);
        app.Id.Should().Be("1234");
        tokens.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_Not_Cache_Failed_Lookups()
    {
        _queries
            .SetupSequence(q => q.GetActiveAppByAppKey("A-DEV-000", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Timeout during connection attempt"))
            .ReturnsAsync(new Application { Id = "1234" });

        await FluentActions.Awaiting(() => _sut.FindByAppKey("A-DEV-000", default))
            .Should().ThrowAsync<TimeoutException>();

        var app = await _sut.FindByAppKey("A-DEV-000", default);
        app.Id.Should().Be("1234");
        _queries.Verify(q => q.GetActiveAppByAppKey(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Should_Not_Query_When_Cache_Is_Filled_After_A_Stale_Miss()
    {
        // Simulates a caller that saw a miss, then resumed after the previous lookup filled the cache
        var cache = new StaleFirstReadCache();
        cache.Inner.Set("APP-KEY-STATUS-A-DEV-000", new CachedApplication(new Application { Id = "1234" }));
        var sut = new IngestionCache(cache, _queries.Object);

        var app = await sut.FindByAppKey("A-DEV-000", default);

        app.Id.Should().Be("1234");
        _queries.Verify(q => q.GetActiveAppByAppKey(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    private sealed class StaleFirstReadCache : IMemoryCache
    {
        public MemoryCache Inner { get; } = new(new MemoryCacheOptions());
        private bool _staleRead = true;

        public bool TryGetValue(object key, out object? value)
        {
            if (_staleRead)
            {
                _staleRead = false;
                value = null;
                return false;
            }

            return Inner.TryGetValue(key, out value);
        }

        public ICacheEntry CreateEntry(object key) => Inner.CreateEntry(key);
        public void Remove(object key) => Inner.Remove(key);
        public void Dispose() => Inner.Dispose();
    }

    private sealed class TestClock : Microsoft.Extensions.Internal.ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
    }
}
