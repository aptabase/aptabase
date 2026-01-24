using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aptabase.Features.ErrorReporting;

public class ErrorBody
{
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
}
