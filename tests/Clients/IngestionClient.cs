using System.Net;

namespace Aptabase.IntegrationTests.Clients;

public class IngestionClient
{
    private readonly HttpClient _client;
    private readonly string _ipAddress;
    private readonly string _appKey;

    public IngestionClient(HttpClient client, string appKey, string ipAddress)
    {
        _client = client;
        _ipAddress = ipAddress;
        _appKey = appKey;
    }

    public async Task<HttpStatusCode> TrackEvent(DateTime timestamp, string eventName)
    {
        var row = NewEvent(timestamp, eventName);
        var body = JsonContent.Create(row);
        body.Headers.Add("App-Key", _appKey);
        body.Headers.Add("CloudFront-Viewer-Address", _ipAddress);
        var response = await _client.PostAsync($"/api/v0/event", body);
        return response.StatusCode;
    }

    public async Task<HttpStatusCode> TrackEvents(IEnumerable<(DateTime, string)> events)
    {
        var rows = events.Select(tuple => NewEvent(tuple.Item1, tuple.Item2));
        var body = JsonContent.Create(rows);
        body.Headers.Add("App-Key", _appKey);
        body.Headers.Add("CloudFront-Viewer-Address", _ipAddress);
        var response = await _client.PostAsync($"/api/v0/events", body);
        return response.StatusCode;
    }

    private object NewEvent(DateTime timestamp, string eventName)
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
            }
        };
    }
}