using Sgbj.Cron;

namespace Aptabase.Features.Billing;

public class ResetOveruseCronJob : BackgroundService
{
    private readonly IBillingQueries _billingQueries;
    private readonly ILogger _logger;

    public ResetOveruseCronJob(IBillingQueries billingQueries, ILogger<OveruseNotificationCronJob> logger)
    {
        _billingQueries = billingQueries ?? throw new ArgumentNullException(nameof(billingQueries));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested) 
        {
            try
            {
                _logger.LogInformation("ResetOveruseCronJob is starting.");

                using var timer = new CronTimer("5 0 1 * *", TimeZoneInfo.Utc);

                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    _logger.LogInformation("Unlocking accounts with overuse.");
                    var count = await _billingQueries.UnlockOveruseAccounts();
                    _logger.LogInformation("Unlocked {count} accounts with overuse status.", count);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ResetOveruseCronJob stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResetOveruseCronJob crashed.");
            }
        }
    }
}
