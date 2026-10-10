using System.Collections.Concurrent;

namespace Aptabase.Features.Ingestion;

/// <summary>
/// Counts requests we reject before doing any real work (rate limiter 429s, locked-account 400s)
/// so we can see who is sending them. Nothing is logged per request: counters are aggregated
/// in memory and <see cref="RejectionStatsReporter"/> writes a bounded summary once a minute.
/// </summary>
public interface IRejectionStats
{
    void Record(string reason, string? appKey, string? clientIp, string? userAgent);
    RejectionSnapshot Snapshot();
}

public readonly record struct RejectionKey(string Reason, string AppKey, string ClientIp, string UserAgent);

public sealed record RejectionSnapshot(long Total, long Overflow, IReadOnlyList<KeyValuePair<RejectionKey, long>> Entries);

public sealed class RejectionStats : IRejectionStats
{
    // Hard caps so a flood from many distinct IPs can't grow memory or the summary without bound.
    public const int MaxTrackedSources = 500;
    public const int MaxUserAgentLength = 80;
    private const int MaxAppKeyLength = 20;

    private ConcurrentDictionary<RejectionKey, long> _counts = new();
    private long _total;
    private long _overflow;

    public void Record(string reason, string? appKey, string? clientIp, string? userAgent)
    {
        Interlocked.Increment(ref _total);

        var key = new RejectionKey(
            reason,
            Truncate(appKey?.Trim().ToUpperInvariant(), MaxAppKeyLength),
            clientIp?.Trim() ?? "",
            Truncate(userAgent?.Trim(), MaxUserAgentLength));

        var counts = _counts;
        if (counts.ContainsKey(key) || counts.Count < MaxTrackedSources)
            counts.AddOrUpdate(key, 1, static (_, n) => n + 1);
        else
            Interlocked.Increment(ref _overflow);
    }

    public RejectionSnapshot Snapshot()
    {
        var counts = Interlocked.Exchange(ref _counts, new ConcurrentDictionary<RejectionKey, long>());
        var total = Interlocked.Exchange(ref _total, 0);
        var overflow = Interlocked.Exchange(ref _overflow, 0);

        var entries = counts.OrderByDescending(e => e.Value).ToArray();
        return new RejectionSnapshot(total, overflow, entries);
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value.Length <= max ? value : value[..max];
    }
}

/// <summary>
/// Flushes <see cref="RejectionStats"/> once per interval. Log volume is bounded to at most
/// 1 + <see cref="TopSources"/> lines per interval per instance, regardless of request volume.
/// </summary>
public sealed class RejectionStatsReporter : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    public const int TopSources = 10;

    private readonly IRejectionStats _stats;
    private readonly ILogger _logger;

    public RejectionStatsReporter(IRejectionStats stats, ILogger<RejectionStatsReporter> logger)
    {
        _stats = stats ?? throw new ArgumentNullException(nameof(stats));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
                Flush();
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
        finally
        {
            Flush();
        }
    }

    private void Flush()
    {
        var snapshot = _stats.Snapshot();
        if (snapshot.Total == 0)
            return;

        _logger.LogWarning(
            "Rejected {Total} requests in the last {IntervalSeconds}s from {Sources} distinct sources ({Overflow} beyond the tracking cap).",
            snapshot.Total, (int)Interval.TotalSeconds, snapshot.Entries.Count, snapshot.Overflow);

        foreach (var (key, count) in snapshot.Entries.Take(TopSources))
        {
            _logger.LogWarning(
                "Rejected {Count}x reason={Reason} appKey={AppKey} ip={ClientIp} ua={UserAgent}",
                count, key.Reason, key.AppKey, key.ClientIp, key.UserAgent);
        }
    }
}
