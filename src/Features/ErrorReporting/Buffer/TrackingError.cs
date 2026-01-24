namespace Aptabase.Features.ErrorReporting.Buffer;

public readonly struct TrackingError
{
    public required string ErrorId { get; init; }
    public required string AppId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string ErrorMessage { get; init; }
    public required string ErrorType { get; init; }
    public string? StackTrace { get; init; }
    public string? Platform { get; init; }
    public string? OsName { get; init; }
    public string? OsVersion { get; init; }
    public string? AppVersion { get; init; }
    public string? SdkVersion { get; init; }
    public string? SessionId { get; init; }
}
