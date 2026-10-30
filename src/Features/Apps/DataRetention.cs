namespace Aptabase.Features.Apps;

public static class DataRetention
{
    public const int DefaultMonths = 60;

    public static DateTime ExpiresAt(DateTime timestamp, int months, TimeSpan? shorterCap = null)
    {
        // Default-initialized tracking structs and internal event generators use the default policy.
        if (months == 0)
            months = DefaultMonths;

        if (months is not (6 or 12 or 24 or 60))
            throw new ArgumentOutOfRangeException(nameof(months));

        var expiry = timestamp.AddMonths(months);
        if (shorterCap.HasValue)
            expiry = expiry < timestamp.Add(shorterCap.Value) ? expiry : timestamp.Add(shorterCap.Value);

        return expiry;
    }
}
