using Xunit;
using Moq;
using AwesomeAssertions;
using Aptabase.Features.Apps;
using Aptabase.Features.ErrorReporting;
using Aptabase.Features.Ingestion;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace Aptabase.UnitTests.Features.Ingestion;

public class ResolveAppKeyAttributeTests
{
    private readonly Mock<IIngestionCache> _cache = new();
    private readonly Mock<IRejectionStats> _rejections = new();
    private readonly ResolveAppKeyAttribute _sut = new();
    private bool _nextCalled;

    public ResolveAppKeyAttributeTests()
    {
        _cache
            .Setup(c => c.FindByAppKey(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CachedApplication.Empty);
    }

    [Fact]
    public async Task Should_Return_404_Without_Running_Action_For_Unknown_AppKey()
    {
        var context = CreateContext("A-DEV-000");

        await _sut.OnResourceExecutionAsync(context, Next(context));

        var result = context.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        result.Value.Should().Be("Appplication not found with given app key: A-DEV-000");
        _rejections.Verify(r => r.Record("unknown_key", "A-DEV-000", It.IsAny<string?>(), "test-agent"), Times.Once());
        _nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Return_404_Without_Running_Action_For_Missing_AppKey()
    {
        var context = CreateContext(appKey: null);

        await _sut.OnResourceExecutionAsync(context, Next(context));

        context.Result.Should().BeOfType<NotFoundObjectResult>();
        _cache.VerifyNoOtherCalls();
        _rejections.Verify(r => r.Record("missing_key", "", It.IsAny<string?>(), "test-agent"), Times.Once());
        _nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Return_400_And_Record_Rejection_Without_Running_Action_For_Locked_App()
    {
        SetupApp("A-DEV-000", new Application { Id = "APP-1", LockReason = 'T' });
        var context = CreateContext("A-DEV-000");

        await _sut.OnResourceExecutionAsync(context, Next(context));

        var result = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        result.Value.Should().Be("Owner account is locked.");
        _rejections.Verify(r => r.Record("locked", "A-DEV-000", It.IsAny<string?>(), "test-agent"), Times.Once());
        _nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Run_Action_With_Resolved_App_For_Active_App()
    {
        SetupApp("A-DEV-000", new Application { Id = "APP-1" });
        var context = CreateContext("a-dev-000");

        await _sut.OnResourceExecutionAsync(context, Next(context));

        context.Result.Should().BeNull();
        _nextCalled.Should().BeTrue();
        ResolveAppKeyAttribute.GetApp(context.HttpContext).Id.Should().Be("APP-1");
        _rejections.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Error_Endpoint_Should_Return_400_For_Missing_AppKey()
    {
        var context = CreateContext(appKey: null);

        await new ResolveErrorAppKeyAttribute().OnResourceExecutionAsync(context, Next(context));

        var result = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        result.Value.Should().BeEquivalentTo(new { error = "Missing App-Key header" });
        _nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Error_Endpoint_Should_Return_401_For_Unknown_AppKey()
    {
        var context = CreateContext("A-DEV-000");

        await new ResolveErrorAppKeyAttribute().OnResourceExecutionAsync(context, Next(context));

        var result = context.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        result.Value.Should().BeEquivalentTo(new { error = "Invalid App-Key" });
        _rejections.Verify(r => r.Record("unknown_key", "A-DEV-000", It.IsAny<string?>(), "test-agent"), Times.Once());
        _nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Error_Endpoint_Should_Return_400_For_Locked_App()
    {
        SetupApp("A-DEV-000", new Application { Id = "APP-1", LockReason = 'T' });
        var context = CreateContext("A-DEV-000");

        await new ResolveErrorAppKeyAttribute().OnResourceExecutionAsync(context, Next(context));

        var result = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        result.Value.Should().BeEquivalentTo(new { error = "Owner account is locked" });
        _rejections.Verify(r => r.Record("locked", "A-DEV-000", It.IsAny<string?>(), "test-agent"), Times.Once());
        _nextCalled.Should().BeFalse();
    }

    private void SetupApp(string appKey, Application app)
    {
        _cache
            .Setup(c => c.FindByAppKey(appKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CachedApplication(app));
    }

    private ResourceExecutingContext CreateContext(string? appKey)
    {
        var services = new ServiceCollection()
            .AddSingleton(_cache.Object)
            .AddSingleton(_rejections.Object)
            .AddLogging()
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Request.Headers.UserAgent = "test-agent";
        if (appKey is not null)
            httpContext.Request.Headers["App-Key"] = appKey;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ResourceExecutingContext(actionContext, new List<IFilterMetadata>(), new List<IValueProviderFactory>());
    }

    private ResourceExecutionDelegate Next(ResourceExecutingContext context)
    {
        return () =>
        {
            _nextCalled = true;
            return Task.FromResult(new ResourceExecutedContext(context, new List<IFilterMetadata>()));
        };
    }
}
