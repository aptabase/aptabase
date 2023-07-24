using Aptabase.Data;
using Sgbj.Cron;

namespace Aptabase.Features.Privacy;

public class PurgeDailySaltsCronJob : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IDbContext _db;

    public PurgeDailySaltsCronJob(IDbContext db, ILogger<PurgeDailySaltsCronJob> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var timer = new CronTimer("0 0 * * *", TimeZoneInfo.Utc);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            _logger.LogInformation("Purging daily salts");
            var rows = await _db.PurgeOldSalts(cancellationToken);
            _logger.LogInformation("Deleted {rows} rows", rows);
        }
    }
}