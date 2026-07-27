using Xunit;
using FluentAssertions;
using Aptabase.Features.ErrorReporting;

namespace Aptabase.UnitTests.Features.ErrorReporting;

public class ErrorBodyTests
{
    private static ErrorBody CreateValidBody() => new()
    {
        ErrorMessage = "Object reference not set to an instance of an object",
        ErrorType = "NullReferenceException",
        StackTrace = "at MyApp.Services.UserService.GetUser(Int32 id)",
        Timestamp = DateTime.UtcNow.AddMinutes(-5),
        Platform = "Android",
        OsName = "Android",
        OsVersion = "14",
        AppVersion = "1.2.3",
        SdkVersion = "aptabase-maui@1.0.0",
        SessionId = "abc-123-def",
        Severity = "fatal",
        Kind = "crash",
    };

    #region IsValid

    [Fact]
    public void IsValid_Should_Return_True_For_Valid_Body()
    {
        CreateValidBody().IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_Should_Return_True_When_Optional_Fields_Are_Null()
    {
        var body = new ErrorBody
        {
            ErrorMessage = "Something went wrong",
            ErrorType = "RuntimeError",
        };

        body.IsValid().Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_Should_Return_False_When_ErrorMessage_Is_Missing(string? errorMessage)
    {
        var body = CreateValidBody();
        body.ErrorMessage = errorMessage;

        body.IsValid().Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_Should_Return_False_When_ErrorType_Is_Missing(string? errorType)
    {
        var body = CreateValidBody();
        body.ErrorType = errorType;

        body.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_ErrorMessage_Exceeds_Max_Length()
    {
        var body = CreateValidBody();
        body.ErrorMessage = new string('a', 5001);

        body.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_True_When_ErrorMessage_Is_At_Max_Length()
    {
        var body = CreateValidBody();
        body.ErrorMessage = new string('a', 5000);

        body.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_ErrorType_Exceeds_Max_Length()
    {
        var body = CreateValidBody();
        body.ErrorType = new string('a', 101);

        body.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_StackTrace_Exceeds_Max_Length()
    {
        var body = CreateValidBody();
        body.StackTrace = new string('a', 10001);

        body.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_Platform_Exceeds_Max_Length()
    {
        var body = CreateValidBody();
        body.Platform = new string('a', 31);

        body.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_OsName_Exceeds_Max_Length()
    {
        var body = CreateValidBody();
        body.OsName = new string('a', 31);

        body.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_OsVersion_Exceeds_Max_Length()
    {
        var body = CreateValidBody();
        body.OsVersion = new string('a', 101);

        body.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_AppVersion_Exceeds_Max_Length()
    {
        var body = CreateValidBody();
        body.AppVersion = new string('a', 51);

        body.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_SdkVersion_Exceeds_Max_Length()
    {
        var body = CreateValidBody();
        body.SdkVersion = new string('a', 41);

        body.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_SessionId_Exceeds_Max_Length()
    {
        var body = CreateValidBody();
        body.SessionId = new string('a', 101);

        body.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_Severity_Exceeds_Max_Length()
    {
        var body = CreateValidBody();
        body.Severity = new string('a', 21);

        body.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_Kind_Exceeds_Max_Length()
    {
        var body = CreateValidBody();
        body.Kind = new string('a', 21);

        body.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsDebug_Should_Default_To_False()
    {
        // Older SDKs never send isDebug, so a missing field must mean "release build"
        new ErrorBody().IsDebug.Should().BeFalse();
    }

    #endregion

    #region NormalizeTimestamp

    [Fact]
    public void NormalizeTimestamp_Should_Default_To_Now_When_Timestamp_Is_Null()
    {
        var body = CreateValidBody();
        body.Timestamp = null;

        body.NormalizeTimestamp();

        body.Timestamp.Should().NotBeNull();
        body.Timestamp!.Value.Kind.Should().Be(DateTimeKind.Utc);
        body.Timestamp.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void NormalizeTimestamp_Should_Clamp_Future_Timestamp_To_Now()
    {
        var body = CreateValidBody();
        body.Timestamp = DateTime.UtcNow.AddHours(2);

        body.NormalizeTimestamp();

        body.Timestamp!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void NormalizeTimestamp_Should_Keep_Past_Utc_Timestamp_Unchanged()
    {
        var past = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var body = CreateValidBody();
        body.Timestamp = past;

        body.NormalizeTimestamp();

        body.Timestamp.Should().Be(past);
        body.Timestamp!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void NormalizeTimestamp_Should_Convert_Local_Timestamp_To_Utc()
    {
        // Offset-format timestamps ("2026-01-01T12:00:00+02:00") deserialize as Local kind
        var local = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var expected = local.ToUniversalTime();
        var body = CreateValidBody();
        body.Timestamp = local;

        body.NormalizeTimestamp();

        body.Timestamp.Should().Be(expected);
        body.Timestamp!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void NormalizeTimestamp_Should_Convert_Unspecified_Timestamp_To_Utc()
    {
        // Timestamps without an offset deserialize as Unspecified kind,
        // which ToUniversalTime() treats as server-local time
        var unspecified = DateTime.SpecifyKind(new DateTime(2026, 1, 1, 12, 0, 0), DateTimeKind.Unspecified);
        var expected = unspecified.ToUniversalTime();
        var body = CreateValidBody();
        body.Timestamp = unspecified;

        body.NormalizeTimestamp();

        body.Timestamp.Should().Be(expected);
        body.Timestamp!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void NormalizeTimestamp_Should_Clamp_Future_Local_Timestamp_To_Now()
    {
        // Conversion must happen BEFORE clamping so the comparison is UTC vs UTC
        var body = CreateValidBody();
        body.Timestamp = DateTime.Now.AddHours(2); // Local kind, in the future

        body.NormalizeTimestamp();

        body.Timestamp!.Value.Kind.Should().Be(DateTimeKind.Utc);
        body.Timestamp.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region NormalizeSeverityAndKind

    [Theory]
    [InlineData("fatal", "fatal")]
    [InlineData("error", "error")]
    [InlineData("FATAL", "fatal")]
    [InlineData("Error", "error")]
    [InlineData("eRrOr", "error")]
    public void NormalizeSeverityAndKind_Should_Canonicalize_Known_Severities(string input, string expected)
    {
        var body = CreateValidBody();
        body.Severity = input;

        body.NormalizeSeverityAndKind();

        body.Severity.Should().Be(expected);
    }

    [Theory]
    [InlineData("crash", "crash")]
    [InlineData("unhandled", "unhandled")]
    [InlineData("taskException", "taskException")]
    [InlineData("handled", "handled")]
    [InlineData("CRASH", "crash")]
    [InlineData("Unhandled", "unhandled")]
    [InlineData("taskexception", "taskException")]
    [InlineData("TASKEXCEPTION", "taskException")]
    [InlineData("Handled", "handled")]
    public void NormalizeSeverityAndKind_Should_Canonicalize_Known_Kinds(string input, string expected)
    {
        var body = CreateValidBody();
        body.Kind = input;

        body.NormalizeSeverityAndKind();

        body.Kind.Should().Be(expected);
    }

    [Theory]
    [InlineData("warning")]
    [InlineData("critical")]
    [InlineData("fatal error")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeSeverityAndKind_Should_Empty_Unknown_Severities(string? input)
    {
        var body = CreateValidBody();
        body.Severity = input;

        body.NormalizeSeverityAndKind();

        body.Severity.Should().Be("");
    }

    [Theory]
    [InlineData("exception")]
    [InlineData("panic")]
    [InlineData("task")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeSeverityAndKind_Should_Empty_Unknown_Kinds(string? input)
    {
        var body = CreateValidBody();
        body.Kind = input;

        body.NormalizeSeverityAndKind();

        body.Kind.Should().Be("");
    }

    #endregion
}
