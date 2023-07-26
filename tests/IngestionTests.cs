using System.Net;
using FluentAssertions;
using Xunit;

namespace Aptabase.IntegrationTests;

public class IngestionTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IngestionTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Missing_AppKey()
    {
        var response = await _client.PostAsJsonAsync("/api/_auth/register", new {
            name = "Jon Snow",
            email = "jon.snow@got.com"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}