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
    public async Task Missing_AppKey()
    {
        var response = await _client.PostAsJsonAsync("/api/_auth/register", new {
            name = "Jon Snow",
            email = "jon.snow@got.com"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}