using Sgbj.Cron;

namespace Aptabase.Features.Billing;

/// <summary>
/// Safety net for missed webhooks: every six hours, list the whole LemonSqueezy store (one API call
/// per 100 subscriptions) and correct any stored plan, status or end date that drifted. This replaces
/// per-subscriber reads, so its LemonSqueezy cost grows with the store size divided by 100, not with
/// the number of locked or over-quota users.
/// </summary>
public class SubscriptionReconciliationCronJob : BackgroundService
{
    private readonly ISubscriptionSync _subscriptions;
    private readonly ILogger _logger;

    public SubscriptionReconciliationCronJob(ISubscriptionSync subscriptions, ILogger<SubscriptionReconciliationCronJob> logger)
    {
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("SubscriptionReconciliationCronJob is starting.");

                // Minute 40 keeps it clear of the overuse job (minute 10), so a corrected plan is
                // picked up by the next overuse run rather than racing the current one.
                using var timer = new CronTimer("40 */6 * * *", TimeZoneInfo.Utc);

                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    _logger.LogInformation("Reconciling stored subscriptions with LemonSqueezy.");
                    var summary = await _subscriptions.ReconcileAll(cancellationToken);
                    _logger.LogInformation("Reconciled subscriptions: {Stored} stored, {Fetched} fetched, {Changed} corrected, {Missing} not found in LemonSqueezy.",
                        summary.Stored, summary.Fetched, summary.Changed, summary.Missing);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SubscriptionReconciliationCronJob stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubscriptionReconciliationCronJob crashed.");
            }
        }
    }
}
