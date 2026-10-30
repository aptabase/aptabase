namespace Aptabase.Features.Stats;

public static class StoredAppId
{
    public const string DebugSuffix = "_DEBUG";

    public static bool IsDebug(string? buildMode)
        => string.Equals(buildMode, "debug", StringComparison.OrdinalIgnoreCase);

    public static string Resolve(string appId, string? buildMode)
        => IsDebug(buildMode) ? $"{appId}{DebugSuffix}" : appId;
}
