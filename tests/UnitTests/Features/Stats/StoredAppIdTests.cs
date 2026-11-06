using Xunit;
using AwesomeAssertions;
using Aptabase.Features.Stats;

namespace Aptabase.UnitTests.Features.Stats;

public class StoredAppIdTests
{
    [Theory]
    [InlineData("debug", "app1_DEBUG")]
    [InlineData("DEBUG", "app1_DEBUG")]
    [InlineData("Debug", "app1_DEBUG")]
    [InlineData("release", "app1")]
    [InlineData("", "app1")]
    [InlineData(null, "app1")]
    [InlineData("_DEBUG", "app1")]
    [InlineData("debug; drop", "app1")]
    public void Should_Only_Append_Constant_Suffix_For_Debug(string? buildMode, string expected)
    {
        StoredAppId.Resolve("app1", buildMode).Should().Be(expected);
    }

    [Fact]
    public void Should_Never_Embed_Build_Mode_Text()
    {
        StoredAppId.Resolve("app1", "release_DEBUG").Should().Be("app1");
        StoredAppId.IsDebug("release_DEBUG").Should().BeFalse();
    }
}
