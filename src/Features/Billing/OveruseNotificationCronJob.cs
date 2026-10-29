using Aptabase.Features.Billing.LemonSqueezy;
using Aptabase.Features.Notification;
using Features.Cache;
using Sgbj.Cron;

namespace Aptabase.Features.Billing;

public class OveruseNotificationCronJob : BackgroundService
{
    private readonly IBillingQueries _billingQueries;
    private readonly ISubscriptionSync _subscriptions;
    private readonly IEmailClient _emailClient;
    private readonly EnvSettings _env;
    private readonly ICache _cache;
    private readonly ILogger _logger;

    public OveruseNotificationCronJob(IBillingQueries billingQueries, ISubscriptionSync subscriptions, IEmailClient emailClient, EnvSettings env, ICache cache, ILogger<OveruseNotificationCronJob> logger)
    {
        _billingQueries = billingQueries ?? throw new ArgumentNullException(nameof(billingQueries));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested) 
        {
            try
            {
                _logger.LogInformation("OveruseNotificationCronJob is starting.");

                using var timer = new CronTimer("10 * * * *", TimeZoneInfo.Utc);

                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    _logger.LogInformation("Looking for accounts with events overuse.");
                    var usagePerApp = await _billingQueries.GetBillingUsageByApp(DateTime.UtcNow.Year, DateTime.UtcNow.Month);
                    var users = await _billingQueries.GetUserQuotaForApps(usagePerApp.Select(x => x.AppId).ToArray());
                    foreach (var user in users)
                    {
                        try
                        {
                            var quota = user.GetQuota();
                            var usage = usagePerApp.Where(x => user.AppIds.Contains(x.AppId)).Sum(x => x.Count);

                            // Pausing ingestion is the one action here that costs a paying customer data, so before
                            // doing it on a subscription make sure the stored plan is what LemonSqueezy has: a plan
                            // change whose webhook was missed or rejected must not lock the customer at the old quota.
                            if (usage >= quota && user.SubscriptionId is long subscriptionId)
                            {
                                var refreshed = await _subscriptions.RefreshFromLemonSqueezy(subscriptionId, user.Id, cancellationToken);
                                if (refreshed is not null)
                                {
                                    user.VariantID = SubscriptionStatus.IsEnded(refreshed.Status) ? null : refreshed.VariantID;
                                    quota = user.GetQuota();
                                }
                            }

                            var perc = usage * 1.0 / quota;
                            _logger.LogInformation("User {UserId} has used {Usage} ({Perc:P}) out of {Quota} events.", user.Id, usage, perc, quota);

                            var (subject, templateName) = GetSubjectTemplateName(perc);
                            if (string.IsNullOrEmpty(templateName) || string.IsNullOrEmpty(subject))
                                continue; // No need to send notification

                            var cacheKey = $"OveruseNotification.{user.Id}.{templateName}.{DateTime.UtcNow.Year}-{DateTime.UtcNow.Month.ToString().PadLeft(2, '0')}";
                            if (await _cache.Exists(cacheKey))
                                continue; // Already sent notification

                            await _emailClient.SendEmailAsync(user.Email, subject, templateName, new()
                            {
                                { "name", user.Name.Split(" ").ElementAtOrDefault(0) ?? user.Name },
                                { "quota", quota.ToString("N0") },
                                { "url", _env.SelfBaseUrl },
                            }, cancellationToken);

                            if (usage >= quota)
                            {
                                _logger.LogInformation("User {UserId} has reached the limit, pausing ingestion.", user.Id);
                                await _billingQueries.LockUser(user.Id, "O");
                            }

                            await _cache.Set(cacheKey, DateTime.UtcNow.ToString("o"), TimeSpan.FromDays(10));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "OveruseNotificationCronJob failed to process usage for user {UserId}.", user.Id);
                        }
                    }
                    _logger.LogInformation("Finished processing overuse notifications.");

                    await ReleaseLockedSubscribersWithRoom(usagePerApp);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OveruseNotificationCronJob stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OveruseNotificationCronJob crashed.");
            }
        }
    }

    // The pass above only sees unlocked accounts, so a subscriber who was paused at a plan we had
    // wrong would stay paused until the monthly reset. The stored plan is kept honest by webhooks,
    // the six-hourly reconciliation and the Sync button, so this only compares usage with the stored
    // quota and releases the subscribers who have room. No LemonSqueezy calls here.
    private async Task ReleaseLockedSubscribersWithRoom(IEnumerable<BillingUsageByApp> usagePerApp)
    {
        var locked = await _billingQueries.GetLockedOveruseSubscribers();
        foreach (var user in locked)
        {
            try
            {
                var quota = user.GetQuota();
                var usage = usagePerApp.Where(x => user.AppIds.Contains(x.AppId)).Sum(x => x.Count);
                if (usage < quota)
                {
                    _logger.LogInformation("User {UserId} is locked for overuse but has used {Usage} of {Quota} on their current plan; resuming ingestion.", user.Id, usage, quota);
                    await _billingQueries.UnlockUser(user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OveruseNotificationCronJob failed to re-check locked user {UserId}.", user.Id);
            }
        }
    }

    private static (string?, string?) GetSubjectTemplateName(double perc)
    {
        if (perc >= 1)
            return ("ACTION REQUIRED: Event ingestion paused", "UsageLevel100");
        if (perc >= 0.9)
            return ("WARNING: You have used 90% of your monthly limit, incoming events will be dropped soon", "UsageLevel90");
        if (perc >= 0.8)
            return ("You have used 80% of your monthly limit, incoming events will be dropped soon", "UsageLevel80");
        return (null, null);
    }
}
