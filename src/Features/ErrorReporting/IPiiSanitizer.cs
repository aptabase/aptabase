namespace Aptabase.Features.ErrorReporting;

/// <summary>
/// Service for sanitizing Personally Identifiable Information (PII) from error messages and stack traces
/// </summary>
public interface IPiiSanitizer
{
    /// <summary>
    /// Sanitizes PII from the given text by replacing sensitive patterns with redacted placeholders
    /// </summary>
    /// <param name="text">The text to sanitize</param>
    /// <returns>The sanitized text with PII redacted</returns>
    string Sanitize(string? text);
}
