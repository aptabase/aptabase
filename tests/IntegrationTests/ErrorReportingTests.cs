using Xunit;
using System.Net;
using FluentAssertions;
using Aptabase.IntegrationTests.Clients;
using Aptabase.Features.ErrorReporting.Buffer;

namespace Aptabase.IntegrationTests;

[Collection("Integration Tests")]
public class ErrorReportingTests
{
    private readonly IntegrationTestsFixture _fixture;
    private readonly ErrorBackgroundWritter _errorWritter;

    public ErrorReportingTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
        _errorWritter = _fixture.GetHostedService<ErrorBackgroundWritter>();
    }

    [Fact]
    public async Task Can_Ingest_And_Read_Back_Error()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new ErrorsClient(_fixture.CreateClient(), app.AppKey);
        var code = await client.TrackError(ErrorsClient.NewError(
            DateTime.UtcNow,
            "NullReferenceException",
            "Object reference not set to an instance of an object",
            "at MyApp.MainPage.OnButtonClicked()"));
        code.Should().Be(HttpStatusCode.Accepted);

        await _errorWritter.FlushErrors();

        var response = await _fixture.UserA.GetErrors(app.Id);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ErrorsListResponse>();
        result!.Pagination.Total.Should().Be(1);

        var error = result.Errors.Single();
        error.ErrorType.Should().Be("NullReferenceException");
        error.ErrorMessage.Should().Be("Object reference not set to an instance of an object");
        error.StackTrace.Should().Be("at MyApp.MainPage.OnButtonClicked()");
        error.Severity.Should().Be("error");
        error.Kind.Should().Be("handled");
        error.Platform.Should().Be(".NET MAUI");
        error.OsName.Should().Be("macOS");
    }

    [Fact]
    public async Task Can_Get_Error_By_Id()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new ErrorsClient(_fixture.CreateClient(), app.AppKey);
        var code = await client.TrackError(ErrorsClient.NewError(DateTime.UtcNow, "TimeoutException", "The request timed out"));
        code.Should().Be(HttpStatusCode.Accepted);

        await _errorWritter.FlushErrors();

        var listResponse = await _fixture.UserA.GetErrors(app.Id);
        var list = await listResponse.Content.ReadFromJsonAsync<ErrorsListResponse>();
        var errorId = list!.Errors.Single().ErrorId;

        var response = await _fixture.UserA.GetErrorById(app.Id, errorId);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var error = await response.Content.ReadFromJsonAsync<ErrorEntry>();
        error!.ErrorId.Should().Be(errorId);
        error.ErrorType.Should().Be("TimeoutException");

        var notFound = await _fixture.UserA.GetErrorById(app.Id, Guid.NewGuid().ToString());
        notFound.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Redacts_Pii_From_Error_Messages()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new ErrorsClient(_fixture.CreateClient(), app.AppKey);
        var code = await client.TrackError(ErrorsClient.NewError(
            DateTime.UtcNow,
            "SmtpException",
            "Failed to send receipt to bob@example.com",
            "at Mailer.Send() for user bob@example.com from 192.168.1.42"));
        code.Should().Be(HttpStatusCode.Accepted);

        await _errorWritter.FlushErrors();

        var response = await _fixture.UserA.GetErrors(app.Id);
        var result = await response.Content.ReadFromJsonAsync<ErrorsListResponse>();

        var error = result!.Errors.Single();
        error.ErrorMessage.Should().Be("Failed to send receipt to [EMAIL_REDACTED]");
        error.StackTrace.Should().Be("at Mailer.Send() for user [EMAIL_REDACTED] from [IP_REDACTED]");
    }

    [Fact]
    public async Task Clamps_Future_Timestamps_To_Now()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new ErrorsClient(_fixture.CreateClient(), app.AppKey);
        var code = await client.TrackError(ErrorsClient.NewError(DateTime.UtcNow.AddYears(10), "OverflowException", "Value was too large"));
        code.Should().Be(HttpStatusCode.Accepted);

        await _errorWritter.FlushErrors();

        // The default query window is the last 7 days, so the error is only
        // visible here because ingestion clamped the timestamp to "now"
        var response = await _fixture.UserA.GetErrors(app.Id);
        var result = await response.Content.ReadFromJsonAsync<ErrorsListResponse>();

        result!.Pagination.Total.Should().Be(1);
        result.Errors.Single().Timestamp.Should().BeBefore(DateTime.UtcNow.AddMinutes(1));
    }

    public static IEnumerable<object[]> InvalidPayloads =>
    [
        // Missing errorMessage
        [new { errorType = "NullReferenceException" }],
        // Missing errorType
        [new { errorMessage = "Something broke" }],
        // errorType over the 100 character limit
        [new { errorMessage = "Something broke", errorType = new string('x', 101) }],
        // errorMessage over the 5000 character limit
        [new { errorMessage = new string('x', 5001), errorType = "NullReferenceException" }],
    ];

    [Theory, MemberData(nameof(InvalidPayloads))]
    public async Task Cant_Ingest_Invalid_Payload(object payload)
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new ErrorsClient(_fixture.CreateClient(), app.AppKey);
        var code = await client.TrackError(payload);
        code.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cant_Ingest_Unknown_AppKey()
    {
        var client = new ErrorsClient(_fixture.CreateClient(), "THIS-DOES-NOT-EXIST");
        var code = await client.TrackError(ErrorsClient.NewError(DateTime.UtcNow, "NullReferenceException", "Something broke"));
        code.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cant_Read_Errors_From_Other_Users()
    {
        var app = await _fixture.UserA.CreateApp(Guid.NewGuid().ToString());

        var client = new ErrorsClient(_fixture.CreateClient(), app.AppKey);
        await client.TrackError(ErrorsClient.NewError(DateTime.UtcNow, "NullReferenceException", "Something broke"));
        await _errorWritter.FlushErrors();

        var responseA = await _fixture.UserA.GetErrors(app.Id);
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseB = await _fixture.UserB.GetErrors(app.Id);
        responseB.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

public class ErrorsListResponse
{
    public List<ErrorEntry> Errors { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
}

public class ErrorEntry
{
    public string ErrorId { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string ErrorMessage { get; set; } = "";
    public string ErrorType { get; set; } = "";
    public string StackTrace { get; set; } = "";
    public string Platform { get; set; } = "";
    public string OsName { get; set; } = "";
    public string OsVersion { get; set; } = "";
    public string AppVersion { get; set; } = "";
    public string SdkVersion { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Kind { get; set; } = "";
}
