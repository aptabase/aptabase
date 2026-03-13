namespace Aptabase.Features.Export;

public class ExportCronJob(
    IServiceProvider serviceProvider,
    ILogger<ExportCronJob> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<ExportCronJob> _logger = logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Export background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingExportsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending exports");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Export background service stopped");
    }

    private async Task ProcessPendingExportsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var exportQueries = scope.ServiceProvider.GetRequiredService<IExportQueries>();

        var pendingExports = await exportQueries.GetPendingExportsAsync(cancellationToken);

        foreach (var export in pendingExports)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessExportAsync(export, exportQueries, cancellationToken);
        }
    }

    private async Task ProcessExportAsync(
        Export export,
        IExportQueries exportQueries,
        CancellationToken cancellationToken)
    {
        try
        {
            // Try to claim this export by updating status to InProgress
            var claimed = await exportQueries.UpdateStatusAsync(
                export.Id,
                ExportStatus.Pending,
                ExportStatus.InProgress,
                cancellationToken: cancellationToken);

            if (!claimed)
            {
                return;
            }

            _logger.LogInformation("Processing export {ExportId} for app {AppId}", export.Id, export.AppId);

            // Process the export
            // TODO

            // Mark as completed
            await exportQueries.UpdateStatusAsync(
                export.Id,
                ExportStatus.InProgress,
                ExportStatus.Completed,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process export {ExportId}", export.Id);

            // Mark as failed
            await exportQueries.UpdateStatusAsync(
                export.Id,
                ExportStatus.InProgress,
                ExportStatus.Faulted,
                errorMessage: ex.Message,
                cancellationToken: cancellationToken);
        }
    }
}