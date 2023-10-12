namespace System;

public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Length <= maxLength)
            return value;

        return $"{value[..(maxLength - suffix.Length)]}{suffix}"; 
    }
}