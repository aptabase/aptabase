using Xunit;
using AwesomeAssertions;
using Aptabase.Features.Export;
using Aptabase.Features.Stats;

namespace Aptabase.UnitTests.Features.Stats;

public class HasReadAccessToAppFilterTests
{
    private const string AppA = "app-a";

    [Fact]
    public void Should_Resolve_From_QueryParams()
    {
        var args = Args(("body", new QueryParams { AppId = AppA }));

        HasReadAccessToApp.TryGetAppId(args, out var appId).Should().BeTrue();
        appId.Should().Be(AppA);
    }

    [Fact]
    public void Should_Resolve_From_DownloadRequest()
    {
        var args = Args(("body", new DownloadRequest { AppId = AppA }));

        HasReadAccessToApp.TryGetAppId(args, out var appId).Should().BeTrue();
        appId.Should().Be(AppA);
    }

    [Fact]
    public void Should_Resolve_From_String_Parameter()
    {
        var args = Args(("buildMode", "release"), ("appId", AppA));

        HasReadAccessToApp.TryGetAppId(args, out var appId).Should().BeTrue();
        appId.Should().Be(AppA);
    }

    [Fact]
    public void Should_Ignore_Other_String_Parameters()
    {
        var args = Args(("buildMode", "release"), ("sessionId", AppA));

        HasReadAccessToApp.TryGetAppId(args, out _).Should().BeFalse();
    }

    [Fact]
    public void Should_Reject_Empty_AppId()
    {
        var args = Args(("body", new QueryParams { AppId = "" }));

        HasReadAccessToApp.TryGetAppId(args, out _).Should().BeFalse();
    }

    [Fact]
    public void Should_Reject_Missing_Arguments()
    {
        var args = Args();

        HasReadAccessToApp.TryGetAppId(args, out _).Should().BeFalse();
    }

    private static Dictionary<string, object?> Args(params (string Name, object? Value)[] items)
        => items.ToDictionary(x => x.Name, x => x.Value);
}
