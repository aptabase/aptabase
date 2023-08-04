using System.Net;
using FluentAssertions;
using Aptabase.Features.Apps;
using Aptabase.Features.Stats;

namespace Aptabase.IntegrationTests.Clients;

public class AccountClient
{
    private readonly HttpClient _client;

    public AccountClient(HttpClient client)
    {
        _client = client;
    }

    public async Task CreateAccount(string name, string email)
    {
        var response = await _client.PostAsJsonAsync("/api/_auth/register", new { name, email });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmUrl = await MailCatcher.GetLinkSentTo(email);
        response = await _client.GetAsync(confirmUrl);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    public async Task<Application> CreateApp(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/_apps", new { name });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apps = await _client.GetFromJsonAsync<Application[]>("/api/_apps");
        var app = apps?.FirstOrDefault(x => x.Name == name);
        return app ?? throw new Exception("No app found");
    }

    public async Task<HttpResponseMessage> GetKeyMetrics(string appId, string period)
    {
        return await _client.GetAsync($"/api/_stats/metrics?buildMode=release&period={period}&appId={appId}");
    }

    public async Task<long> CountEvents(string appId, string period)
    {
        var metrics = await _client.GetFromJsonAsync<KeyMetrics>($"/api/_stats/metrics?buildMode=release&period={period}&appId={appId}");
        return metrics?.Current.Events ?? 0;
    }
}