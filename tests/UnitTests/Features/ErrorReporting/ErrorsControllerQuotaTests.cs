using Xunit;
using Moq;
using AwesomeAssertions;
using Aptabase.Data;
using Aptabase.Features;
using Aptabase.Features.Apps;
using Aptabase.Features.ErrorReporting;
using Aptabase.Features.ErrorReporting.Buffer;
using Aptabase.Features.Ingestion;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aptabase.UnitTests.Features.ErrorReporting;

// Covers the error-quota gate in ErrorsController.PostError.
//
// NOTE: the "quota exhausted on first detection" path (TryConsumeErrorQuota returns false
// -> 403 + cache entry) is intentionally NOT covered here: TryConsumeErrorQuota is a Dapper
// static extension over IDbContext.Connection, so forcing it to return false would require
// mocking the whole ADO.NET command/reader pipeline. It is exercised indirectly via the
// cache-hit test below (which asserts the 403 shape) and belongs in integration tests.
public class ErrorsControllerQuotaTests
{
    private const string AppId = "APP-123";
    private const string AppKey = "A-DEV-000";

    private readonly Mock<IErrorBuffer> _buffer = new();
    private readonly Mock<IIngestionCache> _ingestionCache = new();
    private readonly Mock<IPiiSanitizer> _piiSanitizer = new();
    private readonly Mock<IErrorQueryClient> _errorQueryClient = new();
    private readonly Mock<IDbContext> _db = new();
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());

    public ErrorsControllerQuotaTests()
    {
        _ingestionCache
            .Setup(c => c.FindByAppKey(AppKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CachedApplication(new Application { Id = AppId }));

        _piiSanitizer
            .Setup(s => s.Sanitize(It.IsAny<string?>()))
            .Returns<string?>(text => text ?? "");
    }

    [Fact]
    public async Task Should_Skip_Quota_Check_When_Quota_Is_Disabled()
    {
        var controller = CreateController(errorQuotaEnabled: false);

        var result = await controller.PostError(CreateValidBody(), default);

        result.Should().BeOfType<AcceptedResult>();
        _buffer.Verify(b => b.Add(ref It.Ref<TrackingError>.IsAny), Times.Once());

        // Quota disabled must mean zero Postgres round-trips
        _db.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Should_Return_403_Without_Touching_Database_When_Exhaustion_Is_Cached()
    {
        _memoryCache.Set($"ERROR-QUOTA-EXHAUSTED-{AppId}", true);
        var controller = CreateController(errorQuotaEnabled: true);

        var result = await controller.PostError(CreateValidBody(), default);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(403);

        // Cached exhaustion must short-circuit before the Postgres UPDATE and the buffer
        _db.VerifyNoOtherCalls();
        _buffer.Verify(b => b.Add(ref It.Ref<TrackingError>.IsAny), Times.Never());
    }

    [Fact]
    public async Task Should_Not_Reject_Other_Apps_When_One_App_Is_Exhausted()
    {
        _memoryCache.Set("ERROR-QUOTA-EXHAUSTED-SOME-OTHER-APP", true);
        var controller = CreateController(errorQuotaEnabled: true);

        // This app has no cached exhaustion, so the controller must fall through to the real
        // quota check (i.e. the other app's cache entry does not short-circuit this app).
        // Dapper then blows up on the mocked connection, which is irrelevant here: reaching
        // IDbContext.Connection is exactly the assertion.
        try
        {
            await controller.PostError(CreateValidBody(), default);
        }
        catch
        {
            // expected: Dapper cannot run against a Moq-provided connection
        }

        _db.VerifyGet(d => d.Connection, Times.AtLeastOnce());
    }

    private ErrorsController CreateController(bool errorQuotaEnabled)
    {
        var controller = new ErrorsController(
            _buffer.Object,
            _ingestionCache.Object,
            _piiSanitizer.Object,
            _errorQueryClient.Object,
            _db.Object,
            CreateEnvSettings(errorQuotaEnabled),
            _memoryCache,
            Mock.Of<ILogger<ErrorsController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        controller.HttpContext.Request.Headers["App-Key"] = AppKey;
        return controller;
    }

    // EnvSettings can only be populated from environment variables (EnvSettings.Load), so
    // tests create an instance via its private constructor and flip the single flag needed.
    private static EnvSettings CreateEnvSettings(bool errorQuotaEnabled)
    {
        var env = (EnvSettings)Activator.CreateInstance(typeof(EnvSettings), nonPublic: true)!;
        typeof(EnvSettings)
            .GetProperty(nameof(EnvSettings.ErrorQuotaEnabled))!
            .SetValue(env, errorQuotaEnabled);
        return env;
    }

    private static ErrorBody CreateValidBody() => new()
    {
        ErrorMessage = "Object reference not set to an instance of an object",
        ErrorType = "NullReferenceException",
    };
}
