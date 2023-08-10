using Xunit;
using Moq;
using Aptabase.Features.Ingestion;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Aptabase.Features.Apps;

namespace Aptabase.UnitTests.Features.Ingestion;

public class IngestionCacheTests
{
    [Fact]
    public async Task Returns_Empty_For_Unkown_AppKeys()
    {
        var queries = new Mock<IAppQueries>();
        queries.Setup(q => q.GetActiveAppByAppKey(It.IsAny<string>(), default)).ReturnsAsync((Application?)null);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new IngestionCache(cache, queries.Object);

        var appId = await sut.FindByAppKey("A-DEV-000", default);
        appId.Should().BeEmpty();
        cache.Count.Should().Be(1);

        var appId2 = await sut.FindByAppKey("A-DEV-000", default);
        appId2.Should().BeEmpty();
        cache.Count.Should().Be(1);

        queries.Verify(q => q.GetActiveAppByAppKey(It.IsAny<string>(), default), Times.Once());
    }

    [Fact]
    public async Task Should_Cache_Result()
    {
        var queries = new Mock<IAppQueries>();
        queries.Setup(q => q.GetActiveAppByAppKey("A-DEV-000", default)).ReturnsAsync(new Application { Id = "1234" });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new IngestionCache(cache, queries.Object);

        var appId = await sut.FindByAppKey("A-DEV-000", default);
        appId.Should().Be("1234");
        cache.Count.Should().Be(1);

        var appId2 = await sut.FindByAppKey("A-DEV-000", default);
        appId2.Should().Be("1234");
        cache.Count.Should().Be(1);

        queries.Verify(q => q.GetActiveAppByAppKey(It.IsAny<string>(), default), Times.Once());
    }
}
