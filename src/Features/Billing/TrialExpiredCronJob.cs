using Sgbj.Cron;

namespace Aptabase.Features.Billing;

public class TrialExpiredCronJob : BackgroundService
{
    private readonly IBillingQueries _billingQueries;
    private readonly ILogger _logger;

    public TrialExpiredCronJob(IBillingQueries billingQueries, ILogger<TrialExpiredCronJob> logger)
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
                _logger.LogInformation("TrialExpiredCronJob is starting.");

                using var timer = new CronTimer("0 0 * * *", TimeZoneInfo.Utc);

                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    _logger.LogInformation("Searching for users with expired trial.");
                    var count = await _billingQueries.LockUsersWithExpiredTrials();
                    _logger.LogInformation("Locked {count} users with expired trial.", count);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("TrialExpiredCronJob stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TrialExpiredCronJob crashed.");
            }
        }
    }
}