using System.Text.RegularExpressions;

namespace Aptabase.Features.ErrorReporting;

/// <summary>
/// Sanitizes Personally Identifiable Information (PII) from error messages and stack traces
/// </summary>
public partial class PiiSanitizer : IPiiSanitizer
{
    // Email pattern: matches standard email addresses
    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    // IPv4 pattern: matches standard IPv4 addresses with valid octet ranges (0-255).
    // Lookbehind/lookahead prevent matching inside a longer dotted-number sequence.
    [GeneratedRegex(@"(?<!\d\.)\b(?:(?:25[0-5]|2[0-4][0-9]|1?[0-9]?[0-9])\.){3}(?:25[0-5]|2[0-4][0-9]|1?[0-9]?[0-9])\b(?!\.\d)", RegexOptions.Compiled)]
    private static partial Regex IPv4Regex();

    // IPv6 pattern: matches standard IPv6 addresses (including compressed forms like ::1).
    // Lookbehind/lookahead forbid adjacent identifier characters so C++/Rust scope
    // operators (e.g. std::vector) are not mistaken for IPv6 addresses.
    [GeneratedRegex(@"(?<![0-9A-Za-z:])(?:(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}|(?:[0-9a-fA-F]{1,4}:){1,7}:|(?:[0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|::(?:[0-9a-fA-F]{1,4}:){0,6}[0-9a-fA-F]{1,4}|::1)(?![0-9A-Za-z:])", RegexOptions.Compiled)]
    private static partial Regex IPv6Regex();

    // Credit card pattern: matches real 16-digit cards (grouped 4-4-4-4 or contiguous)
    // plus 15-digit American Express (4-6-5). Avoids matching arbitrary digit runs.
    [GeneratedRegex(@"\b(?:(?:\d{4}[-\s]?){3}\d{4}|3[47]\d{2}[-\s]?\d{6}[-\s]?\d{5})\b", RegexOptions.Compiled)]
    private static partial Regex CreditCardRegex();

    // Phone number pattern: matches various phone number formats.
    // Matches: +1-234-567-8900, (123) 456-7890, 123-456-7890, 123.456.7890, etc.
    // Separators between groups are MANDATORY so a bare run of 10 digits is not matched.
    // Negative lookbehind/lookahead avoid matching within longer hex/alphanumeric strings.
    [GeneratedRegex(@"(?<![0-9a-fA-F-])(?:\+?1[-.\s])?\(?([0-9]{3})\)?[-.\s]([0-9]{3})[-.\s]([0-9]{4})(?![0-9a-fA-F])", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();

    // API key/token patterns: matches common patterns for API keys, tokens, and secrets
    // Looks for key=value, token=value, bearer tokens, etc.
    // Group 1: opening quote (optional), Group 2: the actual key value, Group 3: closing quote (optional)
    [GeneratedRegex(@"\b(?:api[_-]?key|token|bearer|secret|password|auth)[=:\s]+(['""]?)([A-Za-z0-9_\-]{20,})(['""]?)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyRegex();

    // JWT pattern: matches JSON Web Tokens (three base64 segments separated by dots)
    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b", RegexOptions.Compiled)]
    private static partial Regex JwtRegex();

    // UUID pattern: matches UUIDs that might be sensitive identifiers
    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", RegexOptions.Compiled)]
    private static partial Regex UuidRegex();

    // SSN pattern: matches US Social Security Numbers (XXX-XX-XXXX format)
    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex SsnRegex();

    public string Sanitize(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        // Apply all sanitization patterns
        var sanitized = text;

        // Redact emails
        sanitized = EmailRegex().Replace(sanitized, "[EMAIL_REDACTED]");

        // Redact IP addresses
        sanitized = IPv4Regex().Replace(sanitized, "[IP_REDACTED]");
        sanitized = IPv6Regex().Replace(sanitized, "[IP_REDACTED]");

        // Redact credit card numbers
        sanitized = CreditCardRegex().Replace(sanitized, "[CARD_REDACTED]");

        // Redact phone numbers
        sanitized = PhoneRegex().Replace(sanitized, "[PHONE_REDACTED]");

        // Redact SSNs (before API keys to avoid false positives)
        sanitized = SsnRegex().Replace(sanitized, "[SSN_REDACTED]");

        // Redact JWTs (before API keys to avoid JWT being caught by bearer token pattern)
        sanitized = JwtRegex().Replace(sanitized, "[JWT_REDACTED]");

        // Redact API keys/tokens/secrets
        sanitized = ApiKeyRegex().Replace(sanitized, m =>
        {
            // Preserve the key name (and surrounding quotes) but redact the value.
            // Group 1 is the opening quote (if any), Group 2 is the actual key value,
            // Group 3 is the closing quote (if any). Extract the prefix before group 2
            // and re-append the captured closing quote so balanced quotes are preserved.
            var prefix = m.Value.Substring(0, m.Groups[2].Index - m.Index);
            return prefix + "[KEY_REDACTED]" + m.Groups[3].Value;
        });

        // Redact UUIDs (be conservative - only if they appear in sensitive contexts)
        // Only redact UUIDs that appear after common sensitive field names
        sanitized = Regex.Replace(
            sanitized,
            @"(?:user[_-]?id|session[_-]?id|client[_-]?id|device[_-]?id)([=:\s]+)['""]?[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}['""]?",
            match => match.Value.Substring(0, match.Groups[1].Index + match.Groups[1].Length - match.Index) + "[UUID_REDACTED]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        return sanitized;
    }
}
