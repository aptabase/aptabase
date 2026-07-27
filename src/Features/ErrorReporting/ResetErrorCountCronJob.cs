using Aptabase.Data;
using Sgbj.Cron;

namespace Aptabase.Features.ErrorReporting;

public class ResetErrorCountCronJob : BackgroundService
{
    private readonly IDbContext _db;
    private readonly ILogger _logger;

    public ResetErrorCountCronJob(IDbContext db, ILogger<ResetErrorCountCronJob> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("ResetErrorCountCronJob is starting.");

                // Run at 00:05 UTC on the 1st of every month
                using var timer = new CronTimer("5 0 1 * *", TimeZoneInfo.Utc);

                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    _logger.LogInformation("Resetting monthly error counts.");
                    var count = await _db.ResetAllErrorCounts(cancellationToken);
                    _logger.LogInformation("Reset error_count for {count} apps.", count);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ResetErrorCountCronJob stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResetErrorCountCronJob crashed.");
            }
        }
    }
}
