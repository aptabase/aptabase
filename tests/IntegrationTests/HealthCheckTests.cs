using System.Net;
using FluentAssertions;
using Xunit;

namespace Aptabase.IntegrationTests;

[Collection("Integration Tests")]
public class HealthCheckTests
{
    private readonly HttpClient _client;
    public HealthCheckTests(IntegrationTestsFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task HealthCheck()
    {
        var response = await _client.GetAsync("/healthz");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
