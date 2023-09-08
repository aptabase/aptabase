using Xunit;
using Moq;
using Aptabase.Features.Ingestion;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Aptabase.Features.Apps;

namespace Aptabase.UnitTests.Features.Ingestion;

public class IngestionCacheTests
{
    private readonly Mock<IAppQueries> _queries;
    private readonly MemoryCache _cache;
    private readonly IngestionCache _sut;

    public IngestionCacheTests()
    {
        _queries = new Mock<IAppQueries>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new IngestionCache(_cache, _queries.Object);
    }

    [Fact]
    public async Task Returns_Empty_For_Unkown_AppKeys()
    {
        _queries.Setup(q => q.GetActiveAppByAppKey(It.IsAny<string>(), default)).ReturnsAsync((Application?)null);

        var app = await _sut.FindByAppKey("A-DEV-000", default);
        app.Id.Should().Be(string.Empty);
        _cache.Count.Should().Be(1);

        var app2 = await _sut.FindByAppKey("A-DEV-000", default);
        app2.Id.Should().Be(string.Empty);
        _cache.Count.Should().Be(1);

        _queries.Verify(q => q.GetActiveAppByAppKey(It.IsAny<string>(), default), Times.Once());
        _queries.Verify(q => q.MaskAsOnboarded(It.IsAny<string>(), default), Times.Never());
    }

    [Fact]
    public async Task Should_Cache_Result()
    {
        _queries.Setup(q => q.GetActiveAppByAppKey("A-DEV-000", default)).ReturnsAsync(new Application { Id = "1234" });

        var app = await _sut.FindByAppKey("A-DEV-000", default);
        app.Id.Should().Be("1234");
        _cache.Count.Should().Be(1);

        var app2 = await _sut.FindByAppKey("A-DEV-000", default);
        app2.Id.Should().Be("1234");
        _cache.Count.Should().Be(1);

        _queries.Verify(q => q.GetActiveAppByAppKey(It.IsAny<string>(), default), Times.Once());
        _queries.Verify(q => q.MaskAsOnboarded(It.IsAny<string>(), default), Times.Once());
    }
}
