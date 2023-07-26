using System.Net;
using FluentAssertions;
using Xunit;

namespace Aptabase.IntegrationTests;

[Collection("Integration Tests")]
public class IngestionTests
{
    private readonly HttpClient _client;
    public IngestionTests(IntegrationTestsFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task User_Can_Create_App()
    {
        var email = $"jon.snow.{Guid.NewGuid()}@got.com";
        var registration = await _client.PostAsJsonAsync("/api/_auth/register", new { name = "Jon Snow", email });
        registration.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmUrl = await MailCatcher.GetLinkSentTo(email);
        var confirmation = await _client.GetAsync(confirmUrl);
        confirmation.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var apps = await _client.GetAsync("/api/_apps");
        apps.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}