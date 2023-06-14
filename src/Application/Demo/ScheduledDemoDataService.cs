using System.Text.Json;
using System.Text.Json.Nodes;
using Aptabase.Application.Ingestion;

namespace Aptabase.Application.Demo;

public class ScheduledDemoDataService : IHostedService, IDisposable
{
    private Task? _executingTask;
    private Timer? _timer;
    private readonly string _appId;
    private readonly ILogger _logger;
    private readonly IIngestionClient _ingestionClient;
    private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

    public ScheduledDemoDataService(IIngestionClient ingestionClient, ILogger<ScheduledDemoDataService> logger)
    {
        _logger = logger;
        _ingestionClient = ingestionClient;
        _appId = Environment.GetEnvironmentVariable("DEMO_APP_ID") ?? "";
    }

    private void ExecuteTask(object? state)
    {
        _timer?.Change(Timeout.Infinite, 0);
        _executingTask = ExecuteTaskAsync(_stoppingCts.Token);
    }

    private async Task ExecuteTaskAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_appId))
            return;

        var sessions = DemoDataGenerator.RandomSessionCount();
        for (int j = 0; j < sessions; j++)
            await IngestSessionDemo(cancellationToken);
    }

    private async Task IngestSessionDemo(CancellationToken cancellationToken)
    {
        var (sessionId, timestamps) = DemoDataGenerator.NewSession();
        _logger.LogInformation("Starting to generate {Count} demo events.", timestamps.Length);

        var (countryCode, regionName, city) = DemoDataGenerator.GetLocation();
        var (osName, osVersion, engineName, engineVersion) = DemoDataGenerator.GetDeviceInfo();
        var locale = DemoDataGenerator.GetLocale();
        var (appVersion, appBuildNumber, sdkVersion) = DemoDataGenerator.GetAppInfo();

        List<EventBody> events = new List<EventBody>();
        foreach (var timestamp in timestamps)
        {
            var (eventName, props) = DemoDataGenerator.GetEvent();
            events.Add(new EventBody
            {
                Timestamp = timestamp,
                EventName = eventName,
                SessionId = sessionId,
                SystemProps = new SystemProperties
                {
                    OSName = osName,
                    OSVersion = osVersion,
                    Locale = locale,
                    EngineName = engineName,
                    EngineVersion = engineVersion,
                    AppVersion = appVersion,
                    AppBuildNumber = appBuildNumber,
                    SdkVersion = sdkVersion,
                },
                Props = JsonDocument.Parse(JsonSerializer.Serialize(props)),
            });
        }

        try
        {
            var header = new EventHeader(_appId, countryCode, regionName, city);
            var result = await _ingestionClient.SendMultipleAsync(header, events.ToArray(), cancellationToken);
            _logger.LogInformation("{Count} demo events ingested.", result.SuccessfulRows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting demo events.");
        }
        finally
        {
            _timer?.Change(_interval, TimeSpan.FromMilliseconds(-1));
        }
    }


    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} is starting", nameof(ScheduledDemoDataService));
        _timer = new Timer(ExecuteTask, null, _interval, TimeSpan.FromMilliseconds(-1));
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} is stopping.", nameof(ScheduledDemoDataService));
        _timer?.Change(Timeout.Infinite, 0);

        if (_executingTask == null)
            return;

        try
        {
            _stoppingCts.Cancel();
        }
        finally
        {
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }

    public void Dispose()
    {
        _stoppingCts.Cancel();
        _timer?.Dispose();
    }
}