using System.Text.Json;

namespace Aptabase.Features.Ingestion;

public readonly struct TrackingEvent
{
    public readonly string ClientIpAddress { get; init; }
    public readonly string UserAgent { get; init; }

    public readonly string AppId { get; init; }
    public readonly DateTime Timestamp { get; init; }
    public readonly string EventName { get; init; }
    public readonly string SessionId { get; init; }
    public readonly string OSName { get; init; }
    public readonly string OSVersion { get; init; }
    public readonly string DeviceModel { get; init; }
    public readonly string Locale { get; init; }
    public readonly string AppVersion { get; init; }
    public readonly string AppBuildNumber { get; init; }
    public readonly string EngineName { get; init; }
    public readonly string EngineVersion { get; init; }
    public readonly string SdkVersion { get; init; }
    public readonly string CountryCode { get; init; }
    public readonly string RegionName { get; init; }
    public readonly string StringProps { get; init; }
    public readonly string NumericProps { get; init; }
    public readonly bool IsDebug { get; init; }
}