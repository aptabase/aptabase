namespace Aptabase.Features.ErrorReporting;

public class ErrorDto
{
    private DateTime _timestamp;

    public string ErrorId { get; set; } = "";
    public string AppId { get; set; } = "";

    // Timestamps in the error_events table are stored as UTC wall time, but the two
    // query backends materialize them with different DateTimeKind: ClickHouse.Client
    // (via Dapper) yields Unspecified, while TinybirdDateTimeJsonConverter yields Utc.
    // Normalizing to Utc here guarantees both backends serialize identically as ISO 8601
    // with a trailing "Z" (e.g. "2026-07-02T14:30:00Z"), which the frontend's
    // `new Date(...)` parses correctly as UTC. Both Dapper and System.Text.Json
    // populate this DTO through the setter, so the normalization applies everywhere.
    public DateTime Timestamp
    {
        get => _timestamp;
        set => _timestamp = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }
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

public class ErrorCountDto
{
    public int Count { get; set; }
}

// Shaped like Stats.TopNItem ({ name, value }) so the frontend can treat it
// like the other top-N filter sources (e.g. /_stats/top-operatingsystems).
public class ErrorTypeDto
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}

public interface IErrorQueryClient
{
    Task<IEnumerable<ErrorDto>> GetErrorsAsync(string appId, DateTime startDate, DateTime endDate, string? errorType, string? osName, string? severity, string? kind, int offset, int limit, CancellationToken cancellationToken);
    Task<ErrorDto?> GetErrorByIdAsync(string appId, string errorId, CancellationToken cancellationToken);
    Task<int> GetErrorCountAsync(string appId, DateTime startDate, DateTime endDate, string? errorType, string? osName, string? severity, string? kind, CancellationToken cancellationToken);
    Task<IEnumerable<ErrorTypeDto>> GetErrorTypesAsync(string appId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
}
