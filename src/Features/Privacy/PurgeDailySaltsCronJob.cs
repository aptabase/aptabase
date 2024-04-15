using Sgbj.Cron;

namespace Aptabase.Features.Privacy;

public class PurgeDailySaltsCronJob : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IPrivacyQueries _privacyQueries;

    public PurgeDailySaltsCronJob(IPrivacyQueries privacyQueries, ILogger<PurgeDailySaltsCronJob> logger)
    {
        _privacyQueries = privacyQueries ?? throw new ArgumentNullException(nameof(privacyQueries));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested) 
        {
            try
            {
                _logger.LogInformation("PurgeDailySaltsCronJob is starting.");

                using var timer = new CronTimer("0 0 * * *", TimeZoneInfo.Utc);

                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    _logger.LogInformation("Purging daily salts");
                    var rows = await _privacyQueries.PurgeOldSalts(cancellationToken);
                    _logger.LogInformation("Deleted {rows} rows", rows);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("PurgeDailySaltsCronJob stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PurgeDailySaltsCronJob crashed.");
            }
        }
    }
}