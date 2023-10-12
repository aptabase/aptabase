using System.Text.Json;

namespace Aptabase.Features.Ingestion.Buffer;

public readonly struct EventRow
{
    // Events from Debug builds are kept for 6 months
    public static readonly TimeSpan DebugTTL = TimeSpan.FromDays(182);
    // Events from Release builds are kept for 5 years
    public static readonly TimeSpan ReleaseTTL = TimeSpan.FromDays(5 * 365);

    public readonly string AppId;
    public readonly DateTime Timestamp;
    public readonly string EventName;
    public readonly string UserId;
    public readonly string SessionId;
    public readonly string OSName;
    public readonly string OSVersion;
    public readonly string Locale;
    public readonly string AppVersion;
    public readonly string AppBuildNumber;
    public readonly string EngineName;
    public readonly string EngineVersion;
    public readonly string SdkVersion;
    public readonly string CountryCode;
    public readonly string RegionName;
    public readonly string City;
    public readonly string StringProps;
    public readonly string NumericProps;
    public readonly DateTime TTL;

    public EventRow(ref TrackingEvent e, string userId)
    {
        var ttl = e.IsDebug ? DebugTTL : ReleaseTTL;

        AppId = e.IsDebug ? $"{e.AppId}_DEBUG" : e.AppId;
        Timestamp = e.Timestamp;
        EventName = e.EventName;
        UserId = userId;
        SessionId = e.SessionId;
        OSName = e.OSName;
        OSVersion = e.OSVersion;
        Locale = e.Locale;
        AppVersion = e.AppVersion;
        AppBuildNumber = e.AppBuildNumber;
        EngineName = e.EngineName;
        EngineVersion = e.EngineVersion;
        SdkVersion = e.SdkVersion;
        CountryCode = e.CountryCode;
        RegionName = e.RegionName;
        City = "";
        StringProps = e.StringProps;
        NumericProps = e.NumericProps;
        TTL = e.Timestamp.Add(ttl);
    }

    public void WriteJson(StringWriter writer)
    {
        writer.Write("{");
        WriteProperty(writer, "appId", AppId);
        WriteProperty(writer, "timestamp", Timestamp.ToString("o"));
        WriteProperty(writer, "eventName", EventName);
        WriteProperty(writer, "userId", UserId);
        WriteProperty(writer, "sessionId", SessionId);
        WriteProperty(writer, "osName", OSName);
        WriteProperty(writer, "osVersion", OSVersion);
        WriteProperty(writer, "locale", Locale);
        WriteProperty(writer, "appVersion", AppVersion);
        WriteProperty(writer, "appBuildNumber", AppBuildNumber);
        WriteProperty(writer, "engineName", EngineName);
        WriteProperty(writer, "engineVersion", EngineVersion);
        WriteProperty(writer, "sdkVersion", SdkVersion);
        WriteProperty(writer, "countryCode", CountryCode);
        WriteProperty(writer, "regionName", RegionName);
        WriteProperty(writer, "city", "");
        WriteProperty(writer, "stringProps", StringProps);
        WriteProperty(writer, "numericProps", NumericProps);
        WriteProperty(writer, "ttl", TTL.ToString("o"), true);
        writer.Write("}");
    }

    private static void WriteProperty(StringWriter writer, string name, string value, bool isLast = false)
    {
        writer.Write($"\"{name}\": \"{JsonEncodedText.Encode(value)}\" {(isLast ? "" : ",")}");
    }
}