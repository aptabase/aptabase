using Xunit;
using Aptabase.Features.Ingestion;
using AwesomeAssertions;

namespace Aptabase.UnitTests.Features.Ingestion;

public class RejectionStatsTests
{
    [Fact]
    public void Aggregates_by_reason_app_key_ip_and_user_agent()
    {
        var stats = new RejectionStats();

        stats.Record("rate_limit", "a-us-123", "1.1.1.1", "sdk/1.0");
        stats.Record("rate_limit", "A-US-123", "1.1.1.1", "sdk/1.0");
        stats.Record("rate_limit", "A-US-123", "2.2.2.2", "sdk/1.0");
        stats.Record("locked", "A-US-999", "1.1.1.1", null);

        var snapshot = stats.Snapshot();

        snapshot.Total.Should().Be(4);
        snapshot.Overflow.Should().Be(0);
        snapshot.Entries.Should().HaveCount(3);
        snapshot.Entries[0].Key.Should().Be(new RejectionKey("rate_limit", "A-US-123", "1.1.1.1", "sdk/1.0"));
        snapshot.Entries[0].Value.Should().Be(2);
    }

    [Fact]
    public void Snapshot_resets_counters()
    {
        var stats = new RejectionStats();
        stats.Record("rate_limit", "A-US-123", "1.1.1.1", "sdk/1.0");

        stats.Snapshot();
        var second = stats.Snapshot();

        second.Total.Should().Be(0);
        second.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Caps_distinct_sources_and_counts_the_rest_as_overflow()
    {
        var stats = new RejectionStats();

        for (var i = 0; i < RejectionStats.MaxTrackedSources + 50; i++)
            stats.Record("rate_limit", "A-US-123", $"10.0.{i / 256}.{i % 256}", "sdk/1.0");

        // A source that is already tracked keeps counting even when the cap is reached.
        stats.Record("rate_limit", "A-US-123", "10.0.0.0", "sdk/1.0");

        var snapshot = stats.Snapshot();

        snapshot.Total.Should().Be(RejectionStats.MaxTrackedSources + 51);
        snapshot.Entries.Should().HaveCount(RejectionStats.MaxTrackedSources);
        snapshot.Overflow.Should().Be(50);
        snapshot.Entries[0].Value.Should().Be(2);
    }

    [Fact]
    public void Truncates_long_user_agents_and_app_keys()
    {
        var stats = new RejectionStats();
        stats.Record("locked", new string('k', 100), "1.1.1.1", new string('u', 500));

        var key = stats.Snapshot().Entries[0].Key;

        key.AppKey.Should().HaveLength(20);
        key.UserAgent.Should().HaveLength(RejectionStats.MaxUserAgentLength);
    }

    [Fact]
    public void Truncates_long_client_ips()
    {
        // The client IP comes from forwarding headers, so a caller can make it as long as the header limit allows
        var stats = new RejectionStats();
        stats.Record("unknown_key", "A-US-123", new string('1', 5000), "sdk/1.0");

        var key = stats.Snapshot().Entries[0].Key;

        key.ClientIp.Should().HaveLength(RejectionStats.MaxClientIpLength);
    }

    [Fact]
    public void Cap_holds_when_new_sources_race_for_the_last_slot()
    {
        const int threads = 32;

        for (var round = 0; round < 50; round++)
        {
            var stats = new RejectionStats();
            for (var i = 0; i < RejectionStats.MaxTrackedSources - 1; i++)
                stats.Record("unknown_key", $"KEY-{i}", "1.1.1.1", "sdk/1.0");

            RunTogether(threads, t => stats.Record("unknown_key", $"RACE-{t}", "1.1.1.1", "sdk/1.0"));

            var snapshot = stats.Snapshot();
            snapshot.Entries.Should().HaveCount(RejectionStats.MaxTrackedSources);
            snapshot.Overflow.Should().Be(threads - 1);
            snapshot.Total.Should().Be(RejectionStats.MaxTrackedSources - 1 + threads);
        }
    }

    [Fact]
    public void Same_new_source_recorded_concurrently_uses_one_slot()
    {
        const int threads = 32;

        for (var round = 0; round < 50; round++)
        {
            var stats = new RejectionStats();
            for (var i = 0; i < RejectionStats.MaxTrackedSources - 2; i++)
                stats.Record("unknown_key", $"KEY-{i}", "1.1.1.1", "sdk/1.0");

            RunTogether(threads, _ => stats.Record("unknown_key", "HOT", "1.1.1.1", "sdk/1.0"));

            // Callers that lost the race to add HOT must give their slot back, so one more source still fits
            stats.Record("unknown_key", "LAST", "1.1.1.1", "sdk/1.0");

            var snapshot = stats.Snapshot();
            snapshot.Entries.Should().HaveCount(RejectionStats.MaxTrackedSources);
            snapshot.Overflow.Should().Be(0);
            snapshot.Entries[0].Key.AppKey.Should().Be("HOT");
            snapshot.Entries[0].Value.Should().Be(threads);
        }
    }

    // Starts every action on its own thread and releases them at the same moment to maximize contention
    private static void RunTogether(int count, Action<int> action)
    {
        using var barrier = new Barrier(count);
        var threads = Enumerable.Range(0, count)
            .Select(t => new Thread(() =>
            {
                barrier.SignalAndWait();
                action(t);
            }))
            .ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());
    }
}
