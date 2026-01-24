using System.Diagnostics;

namespace Aptabase.Features.ErrorReporting.Buffer;

public class ErrorBackgroundWritter : BackgroundService
{
    private readonly IErrorBuffer _buffer;
    private readonly IErrorIngestionClient _client;
    private readonly ILogger _logger;
    private readonly Stopwatch _watch = new();

    public ErrorBackgroundWritter(IErrorBuffer buffer, IErrorIngestionClient client, ILogger<ErrorBackgroundWritter> logger)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ErrorBackgroundWritter is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FlushErrors();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch { }
        }

        // We need to wait a few seconds when cancellation is requested
        // because some errors may be added to the buffer after the cancellation
        // After flushing we can safely exit
        _logger.LogInformation("ErrorBackgroundWritter is stopping.");
        await Task.Delay(TimeSpan.FromSeconds(2));
        await FlushErrors();
        _logger.LogInformation("ErrorBackgroundWritter stopped.");
    }

    public int Count() => _buffer.TakeAll().Length;

    public async Task FlushErrors()
    {
        var errors = _buffer.TakeAll();
        if (errors.Length == 0) return;

        try
        {
            _watch.Restart();

            var rows = errors.Select(e => new ErrorRow(ref e)).ToArray();

            await _client.BulkSendErrorAsync(rows);
            _watch.Stop();
            _logger.LogInformation("Flushed {Count} errors in {TimeMs}ms.", errors.Length, _watch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send errors. {Count} errors were discarded.", errors.Length);
        }
    }
}
