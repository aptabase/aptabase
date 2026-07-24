using System.Net;

namespace Aptabase.IntegrationTests.Clients;

public class ErrorsClient
{
    private static readonly Random _random = new();
    private readonly HttpClient _client;
    private readonly string _ipAddress;
    private readonly string _appKey;

    // Use this constructor to avoid rate limiting
    public ErrorsClient(HttpClient client, string appKey)
        : this(client, appKey, RandomIpAddress())
    {
    }

    public ErrorsClient(HttpClient client, string appKey, string ipAddress)
    {
        _client = client;
        _ipAddress = ipAddress;
        _appKey = appKey;
    }

    public async Task<HttpStatusCode> TrackError(object error)
    {
        var body = JsonContent.Create(error);
        body.Headers.Add("App-Key", _appKey);
        body.Headers.Add("CloudFront-Viewer-Address", _ipAddress);
        var response = await _client.PostAsync("/api/v0/error", body);
        return response.StatusCode;
    }

    public static object NewError(DateTime timestamp, string errorType, string errorMessage, string? stackTrace = null)
    {
        return new
        {
            timestamp = timestamp.ToString("o"),
            errorType,
            errorMessage,
            stackTrace,
            platform = ".NET MAUI",
            osName = "macOS",
            osVersion = "13.5",
            appVersion = "1.0.0",
            sdkVersion = "aptabase-maui@0.2.0",
            sessionId = IngestionClient.NewSessionId(),
            severity = "error",
            kind = "handled",
            isDebug = false
        };
    }

    private static string RandomIpAddress()
    {
        return $"{_random.Next(0, 255)}.{_random.Next(0, 255)}.{_random.Next(0, 255)}.{_random.Next(0, 255)}";
    }
}
