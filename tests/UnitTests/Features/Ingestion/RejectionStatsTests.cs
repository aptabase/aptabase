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
}
