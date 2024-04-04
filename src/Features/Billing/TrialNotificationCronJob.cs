using Aptabase.Features.Notification;
using Sgbj.Cron;

namespace Aptabase.Features.Billing;

public class TrialNotificationCronJob : BackgroundService
{
    private readonly IBillingQueries _billingQueries;
    private readonly IEmailClient _emailClient;
    private readonly EnvSettings _env;
    private readonly ILogger _logger;

    public TrialNotificationCronJob(IBillingQueries billingQueries, IEmailClient emailClient, EnvSettings env, ILogger<TrialNotificationCronJob> logger)
    {
        _billingQueries = billingQueries ?? throw new ArgumentNullException(nameof(billingQueries));
        _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("TrialNotificationCronJob is starting.");

            using var timer = new CronTimer("0 0 * * *", TimeZoneInfo.Utc);

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                _logger.LogInformation("Searching for users to notify about trial expiration.");
                var users = await _billingQueries.GetTrialsDueSoon();
                foreach (var user in users)
                {
                    _logger.LogInformation("Sending trial notification to {name} ({user})", user.Name, user.Email);
                    await _emailClient.SendEmailAsync(user.Email, "Your Trial ends in 5 days", "TrialEndsSoon", new()
                    {
                        { "name", user.Name },
                        { "url", _env.SelfBaseUrl },
                    }, cancellationToken);
                }

                _logger.LogInformation("Sent trial notifications to {count} users", users.Length);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TrialNotificationCronJob stopped.");
        }
    }
}