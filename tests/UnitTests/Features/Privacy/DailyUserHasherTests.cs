using Xunit;
using Moq;
using AwesomeAssertions;
using Aptabase.Features.Privacy;
using Microsoft.Extensions.Caching.Memory;

namespace Aptabase.UnitTests.Features.Privacy;

// Salt cache misses come in bursts while EventBackgroundWritter hashes a whole flush concurrently,
// so these tests fire many concurrent CalculateHash calls against a cold cache.
public class DailyUserHasherTests
{
    private const string Date = "2026-10-10";
    private static readonly DateTime Timestamp = new(2026, 10, 10, 0, 0, 1, DateTimeKind.Utc);

    private readonly Mock<IPrivacyQueries> _queries = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly ManualTimeProvider _time = new();
    private readonly DailyUserHasher _sut;

    public DailyUserHasherTests()
    {
        _sut = new DailyUserHasher(_cache, _queries.Object, _time);
    }

    [Fact]
    public async Task Should_Read_Salt_Once_For_Concurrent_Misses_Of_Same_App()
    {
        var salt = new TaskCompletionSource<byte[]>();
        _queries.Setup(q => q.ReadOrCreateSalt("APP-1", Date)).Returns(salt.Task);

        // Distinct sessions so the per-session user ID cache doesn't short-circuit the salt lookup
        var hashes = Enumerable.Range(0, 500)
            .Select(i => _sut.CalculateHash(Timestamp, "APP-1", $"session-{i}", "1.2.3.4", "agent"))
            .ToArray();

        salt.SetResult(RandomSalt());
        var results = await Task.WhenAll(hashes);

        results.Distinct().Should().ContainSingle();
        _queries.Verify(q => q.ReadOrCreateSalt("APP-1", Date), Times.Once());
    }

    [Fact]
    public async Task Should_Read_Salts_One_At_A_Time_Across_Apps()
    {
        var inFlight = 0;
        var maxInFlight = 0;
        _queries
            .Setup(q => q.ReadOrCreateSalt(It.IsAny<string>(), Date))
            .Returns(async () =>
            {
                var current = Interlocked.Increment(ref inFlight);
                InterlockedMax(ref maxInFlight, current);
                await Task.Delay(5);
                Interlocked.Decrement(ref inFlight);
                return RandomSalt();
            });

        var hashes = Enumerable.Range(0, 20)
            .SelectMany(app => Enumerable.Range(0, 25)
                .Select(i => _sut.CalculateHash(Timestamp, $"APP-{app}", $"session-{app}-{i}", "1.2.3.4", "agent")));

        await Task.WhenAll(hashes);

        maxInFlight.Should().Be(1);
        for (var app = 0; app < 20; app++)
            _queries.Verify(q => q.ReadOrCreateSalt($"APP-{app}", Date), Times.Once());
    }

    [Fact]
    public async Task Should_Fail_Queued_Callers_Fast_When_Salt_Lookup_Fails()
    {
        var salt = new TaskCompletionSource<byte[]>();
        _queries.Setup(q => q.ReadOrCreateSalt(It.IsAny<string>(), Date)).Returns(salt.Task);

        var hashes = Enumerable.Range(0, 100)
            .Select(i => _sut.CalculateHash(Timestamp, $"APP-{i % 5}", $"session-{i}", "1.2.3.4", "agent"))
            .ToArray();

        salt.SetException(new TimeoutException("Timeout during connection attempt"));

        foreach (var hash in hashes)
            await FluentActions.Awaiting(() => hash).Should().ThrowAsync<Exception>();

        // Only the first caller reaches Postgres; the rest must not each wait out a connection timeout
        _queries.Verify(q => q.ReadOrCreateSalt(It.IsAny<string>(), Date), Times.Once());
    }

    [Fact]
    public async Task Should_Not_Cache_Failed_Salt_Lookups()
    {
        _queries
            .SetupSequence(q => q.ReadOrCreateSalt("APP-1", Date))
            .ThrowsAsync(new TimeoutException("Timeout during connection attempt"))
            .ReturnsAsync(RandomSalt());

        await FluentActions.Awaiting(() => _sut.CalculateHash(Timestamp, "APP-1", "session-1", "1.2.3.4", "agent"))
            .Should().ThrowAsync<TimeoutException>();

        _time.Advance(TimeSpan.FromSeconds(5));

        var userId = await _sut.CalculateHash(Timestamp, "APP-1", "session-1", "1.2.3.4", "agent");
        userId.Should().NotBeNullOrEmpty();
        _queries.Verify(q => q.ReadOrCreateSalt("APP-1", Date), Times.Exactly(2));
    }

    [Fact]
    public async Task Should_Fail_Fast_Within_Retry_Delay_After_A_Failure()
    {
        _queries
            .SetupSequence(q => q.ReadOrCreateSalt("APP-1", Date))
            .ThrowsAsync(new TimeoutException("Timeout during connection attempt"))
            .ReturnsAsync(RandomSalt());

        await FluentActions.Awaiting(() => _sut.CalculateHash(Timestamp, "APP-1", "session-1", "1.2.3.4", "agent"))
            .Should().ThrowAsync<TimeoutException>();

        _time.Advance(TimeSpan.FromSeconds(4));

        await FluentActions.Awaiting(() => _sut.CalculateHash(Timestamp, "APP-1", "session-1", "1.2.3.4", "agent"))
            .Should().ThrowAsync<InvalidOperationException>();
        _queries.Verify(q => q.ReadOrCreateSalt("APP-1", Date), Times.Once());
    }

    private static byte[] RandomSalt() => Guid.NewGuid().ToByteArray();

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        while ((current = Volatile.Read(ref target)) < value)
            if (Interlocked.CompareExchange(ref target, value, current) == current)
                return;
    }

    private class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now += delta;
    }
}
