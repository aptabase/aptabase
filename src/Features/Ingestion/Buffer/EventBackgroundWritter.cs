

using System.Diagnostics;
using Aptabase.Features.Privacy;

namespace Aptabase.Features.Ingestion.Buffer;

public class EventBackgroundWritter : BackgroundService
{
    private readonly IEventBuffer _buffer;
    private readonly IIngestionClient _client;
    private readonly ILogger _logger;
    private readonly IUserHasher _hasher;
    private readonly Stopwatch _watch = new();

    public EventBackgroundWritter(IEventBuffer buffer, IUserHasher hasher, IIngestionClient client,  ILogger<EventBackgroundWritter> logger)
    {
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventBackgroundWritter is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FlushEvents();
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch { }
        }

        // We need to wait a few seconds when cancellation is requested
        // because some events may be added to the buffer after the cancellation
        // After flushing we can safely exit
        _logger.LogInformation("EventBackgroundWritter is stopping.");
        await Task.Delay(TimeSpan.FromSeconds(2));
        await FlushEvents();
        _logger.LogInformation("EventBackgroundWritter stopped.");
    }

    public int Count() => _buffer.TakeAll().Length;
    
    public async Task FlushEvents()
    {
        var events = _buffer.TakeAll();
        if (events.Length == 0) return;

        try
        {
            _logger.LogInformation("Flushing {Count} events.", events.Length);
            _watch.Restart();

            var rows = await Task.WhenAll(events.Select(ToEventRow));

            await _client.BulkSendEventAsync(rows);
            _watch.Stop();
            _logger.LogInformation("Flushed {Count} events in {TimeMs}ms.", events.Length, _watch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send events. Will retry later.");
            _buffer.AddRange(ref events);
        }
    }

    private async Task<EventRow> ToEventRow(TrackingEvent e) 
    {
        var userId = await _hasher.CalculateHash(e.Timestamp, e.AppId, e.SessionId, e.ClientIpAddress, e.UserAgent);
        return new EventRow(ref e, userId);
    }
}