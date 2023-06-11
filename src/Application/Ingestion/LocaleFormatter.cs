namespace Aptabase.Application.Ingestion;

public static class LocaleFormatter
{

    // List of special locales and the expected format
    private static Dictionary<string, string> SpecialCases = new()
    {
        { "en-001", "en" },
        { "en-150", "en" },
        { "en-us-posix", "en" },
    };

    // Validates and formats a locale string
    // Return a lowercase locale string
    // Return empty string for empty or the special 'C' locale
    // Returns null for invalid/unsupported locales
    public static string? FormatLocale(string? locale)
    {
        if (string.IsNullOrEmpty(locale) || locale == "C")
            return "";

        // Some systems may use '_' instead of '-'
        // Lowercase it to be consistent
        locale = locale.Replace("_", "-").ToLower();

        if (SpecialCases.ContainsKey(locale))
            return SpecialCases[locale];

        return locale.Length switch
        {
            2 => locale,
            (>= 5) and (<= 7) => locale[2] == '-' ? locale : null,
            10 => locale[2] == '-' && locale[7] == '-' ? locale : null,
            _ => null
        };
    }
}