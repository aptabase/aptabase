using System.Net;

namespace Aptabase.IntegrationTests.Clients;

public class IngestionClient
{
    private static readonly Random _random = new Random();
    private readonly HttpClient _client;
    private readonly string _ipAddress;
    private readonly string _appKey;

    // Use this constructor to avoid rate limiting
    public IngestionClient(HttpClient client, string appKey)
        : this(client, appKey, RandomIpAddress())
    {
    }

    public IngestionClient(HttpClient client, string appKey, string ipAddress)
    {
        _client = client;
        _ipAddress = ipAddress;
        _appKey = appKey;
    }

    public async Task<HttpStatusCode> TrackEvent(DateTime timestamp, string eventName, object? props)
    {
        var row = NewEvent(timestamp, eventName, props);
        var body = JsonContent.Create(row);
        body.Headers.Add("App-Key", _appKey);
        body.Headers.Add("CloudFront-Viewer-Address", _ipAddress);
        var response = await _client.PostAsync($"/api/v0/event", body);
        return response.StatusCode;
    }

    public async Task<HttpStatusCode> TrackEvents(IEnumerable<(DateTime, string, object?)> events)
    {
        var rows = events.Select(tuple => NewEvent(tuple.Item1, tuple.Item2, tuple.Item3));
        var body = JsonContent.Create(rows);
        body.Headers.Add("App-Key", _appKey);
        body.Headers.Add("CloudFront-Viewer-Address", _ipAddress);
        var response = await _client.PostAsync($"/api/v0/events", body);
        return response.StatusCode;
    }

    private object NewEvent(DateTime timestamp, string eventName, object? props)
    {
        return new
        {
            eventName,
            timestamp = DateTime.UtcNow.ToString("o"),
            sessionId = Guid.NewGuid().ToString(),
            systemProps = new
            {
                isDebug = false,
                osName = "macOS",
                osVersion = "13.5",
                appVersion = "1.0.0",
                sdkVersion = "aptabase-swift@0.0.0"
            },
            props
        };
    }

    private static string RandomIpAddress()
    {
        return $"{_random.Next(0, 255)}.{_random.Next(0, 255)}.{_random.Next(0, 255)}.{_random.Next(0, 255)}";
    }
}