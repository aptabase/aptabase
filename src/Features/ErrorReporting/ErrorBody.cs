using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aptabase.Features.ErrorReporting;

public class ErrorBody
{
    // Known values sent by the SDKs. Anything else is normalized to "".
    private static readonly string[] KnownSeverities = new[] { "fatal", "error" };
    private static readonly string[] KnownKinds = new[] { "crash", "unhandled", "taskException", "handled" };

    public string? ErrorMessage { get; set; }
    public string? ErrorType { get; set; }
    public string? StackTrace { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? Platform { get; set; }
    public string? OsName { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public string? SdkVersion { get; set; }
    public string? SessionId { get; set; }
    public string? Severity { get; set; }
    public string? Kind { get; set; }
    // Older SDKs don't send this field, so it defaults to false (release build)
    public bool IsDebug { get; set; }

    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(ErrorMessage))
            return false;

        if (string.IsNullOrWhiteSpace(ErrorType))
            return false;

        // Validate string field lengths
        if (ErrorMessage.Length > 5000)
            return false;

        if (ErrorType.Length > 100)
            return false;

        if (!string.IsNullOrWhiteSpace(StackTrace) && StackTrace.Length > 10000)
            return false;

        if (!string.IsNullOrWhiteSpace(Platform) && Platform.Length > 30)
            return false;

        if (!string.IsNullOrWhiteSpace(OsName) && OsName.Length > 30)
            return false;

        if (!string.IsNullOrWhiteSpace(OsVersion) && OsVersion.Length > 100)
            return false;

        if (!string.IsNullOrWhiteSpace(AppVersion) && AppVersion.Length > 50)
            return false;

        if (!string.IsNullOrWhiteSpace(SdkVersion) && SdkVersion.Length > 40)
            return false;

        if (!string.IsNullOrWhiteSpace(SessionId) && SessionId.Length > 100)
            return false;

        if (!string.IsNullOrWhiteSpace(Severity) && Severity.Length > 20)
            return false;

        if (!string.IsNullOrWhiteSpace(Kind) && Kind.Length > 20)
            return false;

        return true;
    }

    public void NormalizeTimestamp()
    {
        var now = DateTime.UtcNow;

        if (!Timestamp.HasValue)
            Timestamp = now;

        // Convert to UTC before clamping so offset-format timestamps
        // and non-UTC servers compare like-for-like against UtcNow
        Timestamp = Timestamp.Value.ToUniversalTime();

        // Clamp future timestamps to now
        if (Timestamp.Value > now)
            Timestamp = now;
    }

    public void NormalizeSeverityAndKind()
    {
        Severity = Canonicalize(Severity, KnownSeverities);
        Kind = Canonicalize(Kind, KnownKinds);
    }

    // Case-insensitively matches the value against the known list and returns
    // its canonical casing. Unknown, null or empty values become "" — we never
    // reject the whole error report over a bad enum value.
    private static string Canonicalize(string? value, string[] knownValues)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        foreach (var known in knownValues)
        {
            if (string.Equals(known, value, StringComparison.OrdinalIgnoreCase))
                return known;
        }

        return "";
    }
}
