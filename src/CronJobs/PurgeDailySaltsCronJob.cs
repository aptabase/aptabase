using Aptabase.Data;
using Dapper;
using Sgbj.Cron;

namespace Aptabase.CronJobs;

public class PurgeDailySaltsCronJob : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IDbConnectionFactory _dbFactory;

    public PurgeDailySaltsCronJob(IDbConnectionFactory dbFactory, ILogger<PurgeDailySaltsCronJob> logger)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private readonly string PURGE_QUERY = @"
        DELETE FROM app_salts
        WHERE TO_DATE(date, 'YYYY/MM/DD') <= CURRENT_DATE - INTERVAL '2' DAY";

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var timer = new CronTimer("0 0 * * *", TimeZoneInfo.Utc);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            _logger.LogInformation("Purging daily salts");
            using var db = _dbFactory.Create();
            var rows = await db.ExecuteAsync(PURGE_QUERY);
            _logger.LogInformation("Deleted {rows} rows", rows);
        }
    }
}