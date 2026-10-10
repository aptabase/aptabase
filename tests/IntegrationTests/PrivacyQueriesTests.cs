using Xunit;
using AwesomeAssertions;
using Aptabase.Features.Privacy;

namespace Aptabase.IntegrationTests;

[Collection("Integration Tests")]
public class PrivacyQueriesTests
{
    private readonly IntegrationTestsFixture _fixture;
    private readonly IPrivacyQueries _queries;

    public PrivacyQueriesTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
        _queries = _fixture.GetService<IPrivacyQueries>();
    }

    [Fact]
    public async Task ReadOrCreateSalt_Returns_The_Stored_Salt_On_Every_Call()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var first = await _queries.ReadOrCreateSalt(app.Id, "2026-10-10");
        var second = await _queries.ReadOrCreateSalt(app.Id, "2026-10-10");
        var otherDay = await _queries.ReadOrCreateSalt(app.Id, "2026-10-11");

        first.Should().HaveCount(16);
        second.Should().Equal(first);
        otherDay.Should().NotEqual(first);
    }

    [Fact]
    public async Task ReadOrCreateSalt_Agrees_On_One_Salt_When_Racing_On_Separate_Connections()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        // Simulates several instances creating the same day's salt at once. Each call runs on its own
        // pooled connection, so the losers of the insert race must still get the winner's salt back.
        var salts = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => _queries.ReadOrCreateSalt(app.Id, "2026-10-10"))));

        salts.Select(Convert.ToHexString).Distinct().Should().ContainSingle();
    }
}
