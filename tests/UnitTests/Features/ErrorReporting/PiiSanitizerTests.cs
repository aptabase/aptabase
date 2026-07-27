using Xunit;
using FluentAssertions;
using Aptabase.Features.ErrorReporting;

namespace Aptabase.UnitTests.Features.ErrorReporting;

public class PiiSanitizerTests
{
    private readonly IPiiSanitizer _sanitizer;

    public PiiSanitizerTests()
    {
        _sanitizer = new PiiSanitizer();
    }

    [Fact]
    public void Sanitize_Should_Return_Empty_String_For_Null_Input()
    {
        var result = _sanitizer.Sanitize(null);
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Sanitize_Should_Return_Empty_String_For_Empty_Input()
    {
        var result = _sanitizer.Sanitize(string.Empty);
        result.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("Contact me at john.doe@example.com for details", "Contact me at [EMAIL_REDACTED] for details")]
    [InlineData("Email: test.user+tag@sub.domain.co.uk", "Email: [EMAIL_REDACTED]")]
    [InlineData("Multiple emails: alice@test.com and bob@example.org", "Multiple emails: [EMAIL_REDACTED] and [EMAIL_REDACTED]")]
    // ITEM 10: the TLD char class used to contain a literal '|' ([A-Z|a-z]); these still redact.
    [InlineData("user@example.com", "[EMAIL_REDACTED]")]
    [InlineData("john.doe@company.co.uk", "[EMAIL_REDACTED]")]
    public void Sanitize_Should_Redact_Email_Addresses(string input, string expected)
    {
        var result = _sanitizer.Sanitize(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Server IP: 192.168.1.1", "Server IP: [IP_REDACTED]")]
    [InlineData("Connected from 10.0.0.5 to 172.16.0.1", "Connected from [IP_REDACTED] to [IP_REDACTED]")]
    [InlineData("Public IP: 8.8.8.8", "Public IP: [IP_REDACTED]")]
    // ITEM 8: valid octets at sentence boundaries; trailing period preserved.
    [InlineData("192.168.1.1", "[IP_REDACTED]")]
    [InlineData("Server 10.0.0.255 down", "Server [IP_REDACTED] down")]
    [InlineData("Connection to 10.0.0.1.", "Connection to [IP_REDACTED].")]
    public void Sanitize_Should_Redact_IPv4_Addresses(string input, string expected)
    {
        var result = _sanitizer.Sanitize(input);
        result.Should().Be(expected);
    }

    [Theory]
    // ITEM 8: invalid octets (>255) and longer dotted runs must NOT be redacted.
    [InlineData("999.999.999.999")]
    [InlineData("1.2.3.4.5")]
    public void Sanitize_Should_Not_Redact_Invalid_IPv4_Addresses(string input)
    {
        var result = _sanitizer.Sanitize(input);
        result.Should().Be(input);
    }

    [Theory]
    [InlineData("IPv6: 2001:0db8:85a3:0000:0000:8a2e:0370:7334", "IPv6: [IP_REDACTED]")]
    [InlineData("Localhost: ::1", "Localhost: [IP_REDACTED]")]
    // ITEM 5: additional compressed/full IPv6 forms.
    [InlineData("fe80::1", "[IP_REDACTED]")]
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334", "[IP_REDACTED]")]
    [InlineData("::1", "[IP_REDACTED]")]
    public void Sanitize_Should_Redact_IPv6_Addresses(string input, string expected)
    {
        var result = _sanitizer.Sanitize(input);
        result.Should().Be(expected);
    }

    [Theory]
    // ITEM 5: C++/Rust scope operator (::) inside identifiers must NOT be treated as IPv6.
    [InlineData("at std::vector<int>::push_back()")]
    [InlineData("MyNamespace::MyClass::Method")]
    public void Sanitize_Should_Not_Redact_Cpp_Scope_Operator_As_IPv6(string input)
    {
        var result = _sanitizer.Sanitize(input);
        result.Should().Be(input);
        result.Should().NotContain("[IP_REDACTED]");
    }

    [Theory]
    [InlineData("Card: 4532-1234-5678-9010", "Card: [CARD_REDACTED]")]
    [InlineData("Credit card 4532 1234 5678 9010", "Credit card [CARD_REDACTED]")]
    [InlineData("Payment: 4532123456789010", "Payment: [CARD_REDACTED]")]
    // ITEM 6: real 16-digit cards (grouped/contiguous) and 15-digit Amex.
    [InlineData("4111 1111 1111 1111", "[CARD_REDACTED]")]
    [InlineData("4111-1111-1111-1111", "[CARD_REDACTED]")]
    [InlineData("4111111111111111", "[CARD_REDACTED]")]
    [InlineData("3782 822463 10005", "[CARD_REDACTED]")]
    public void Sanitize_Should_Redact_Credit_Card_Numbers(string input, string expected)
    {
        var result = _sanitizer.Sanitize(input);
        result.Should().Be(expected);
    }

    [Theory]
    // ITEM 6: arbitrary digit runs (build numbers, sequences) must NOT be redacted as cards.
    [InlineData("Build 2024 1015 1200 5")]
    [InlineData("seq 1234567890123 done")]
    public void Sanitize_Should_Not_Redact_Non_Card_Digit_Runs(string input)
    {
        var result = _sanitizer.Sanitize(input);
        result.Should().Be(input);
        result.Should().NotContain("[CARD_REDACTED]");
    }

    [Theory]
    [InlineData("Call me at (555) 123-4567", "Call me at [PHONE_REDACTED]")]
    [InlineData("Phone: 555-123-4567", "Phone: [PHONE_REDACTED]")]
    [InlineData("Contact: +1-555-123-4567", "Contact: [PHONE_REDACTED]")]
    // ITEM 7: formatted phone numbers (separator/parens/+country) must be redacted.
    [InlineData("Call +1-234-567-8900", "Call [PHONE_REDACTED]")]
    [InlineData("Phone: (123) 456-7890", "Phone: [PHONE_REDACTED]")]
    [InlineData("123-456-7890", "[PHONE_REDACTED]")]
    [InlineData("123.456.7890", "[PHONE_REDACTED]")]
    public void Sanitize_Should_Redact_Phone_Numbers(string input, string expected)
    {
        var result = _sanitizer.Sanitize(input);
        result.Should().Be(expected);
    }

    [Theory]
    // ITEM 7: a bare run of 10 digits with NO separators must NOT be redacted.
    [InlineData("id 5551234567 here")]
    [InlineData("8005551234")]
    public void Sanitize_Should_Not_Redact_Bare_Digit_Runs_As_Phone(string input)
    {
        var result = _sanitizer.Sanitize(input);
        result.Should().Be(input);
        result.Should().NotContain("[PHONE_REDACTED]");
    }

    [Theory]
    [InlineData("SSN: 123-45-6789", "SSN: [SSN_REDACTED]")]
    [InlineData("Social Security: 987-65-4321", "Social Security: [SSN_REDACTED]")]
    public void Sanitize_Should_Redact_Social_Security_Numbers(string input, string expected)
    {
        var result = _sanitizer.Sanitize(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("api_key=sk_live_1234567890abcdefghij", "api_key=[KEY_REDACTED]")]
    [InlineData("token: abcdefghijklmnopqrstuvwxyz123456", "token: [KEY_REDACTED]")]
    [InlineData("Bearer abcdefghijklmnopqrstuvwxyz123456", "Bearer [KEY_REDACTED]")]
    [InlineData("password=supersecretpassword123456", "password=[KEY_REDACTED]")]
    // ITEM 9: the closing quote must be preserved (balanced quotes), not consumed.
    [InlineData("secret=\"my_secret_value_1234567890123\"", "secret=\"[KEY_REDACTED]\"")]
    [InlineData("api_key=abcdefghij1234567890abcdef", "api_key=[KEY_REDACTED]")]
    public void Sanitize_Should_Redact_API_Keys_And_Tokens(string input, string expected)
    {
        var result = _sanitizer.Sanitize(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void Sanitize_Should_Redact_JWT_Tokens()
    {
        var input = "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var expected = "Authorization: Bearer [JWT_REDACTED]";

        var result = _sanitizer.Sanitize(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("user_id=550e8400-e29b-41d4-a716-446655440000")]
    [InlineData("session_id: 123e4567-e89b-12d3-a456-426614174000")]
    [InlineData("client-id=\"7c9e6679-7425-40de-944b-e07fc1f90ae7\"")]
    [InlineData("device_id='a1b2c3d4-e5f6-7890-abcd-ef1234567890'")]
    public void Sanitize_Should_Redact_Sensitive_UUIDs(string input)
    {
        var result = _sanitizer.Sanitize(input);
        result.Should().Contain("[UUID_REDACTED]");
    }

    [Fact]
    public void Sanitize_Should_Preserve_Non_Sensitive_UUIDs()
    {
        // UUIDs not preceded by sensitive field names should be preserved
        var input = "error_id: 550e8400-e29b-41d4-a716-446655440000";
        var result = _sanitizer.Sanitize(input);
        result.Should().Contain("550e8400-e29b-41d4-a716-446655440000");
    }

    [Fact]
    public void Sanitize_Should_Handle_Multiple_PII_Types_In_Same_Text()
    {
        var input = @"User john.doe@example.com from IP 192.168.1.5 called (555) 123-4567 with API key api_key=sk_live_abcdefghijklmnopqrst and card 4532-1234-5678-9010";

        var result = _sanitizer.Sanitize(input);

        result.Should().NotContain("john.doe@example.com");
        result.Should().NotContain("192.168.1.5");
        result.Should().NotContain("555) 123-4567");
        result.Should().NotContain("sk_live_abcdefghijklmnopqrst");
        result.Should().NotContain("4532-1234-5678-9010");

        result.Should().Contain("[EMAIL_REDACTED]");
        result.Should().Contain("[IP_REDACTED]");
        result.Should().Contain("[PHONE_REDACTED]");
        result.Should().Contain("[KEY_REDACTED]");
        result.Should().Contain("[CARD_REDACTED]");
    }

    [Fact]
    public void Sanitize_Should_Preserve_Stack_Trace_Structure()
    {
        var input = @"System.Exception: Failed to connect to database at 192.168.1.100
   at MyApp.DatabaseService.Connect(String connectionString) in /home/user/app/DatabaseService.cs:line 42
   at MyApp.UserService.GetUser(String email) in /home/john.doe@example.com/app/UserService.cs:line 15";

        var result = _sanitizer.Sanitize(input);

        // Should maintain line structure
        result.Should().Contain("at MyApp.DatabaseService.Connect");
        result.Should().Contain("at MyApp.UserService.GetUser");
        result.Should().Contain(":line 42");
        result.Should().Contain(":line 15");

        // Should redact PII
        result.Should().NotContain("192.168.1.100");
        result.Should().NotContain("john.doe@example.com");
        result.Should().Contain("[IP_REDACTED]");
        result.Should().Contain("[EMAIL_REDACTED]");
    }

    [Fact]
    public void Sanitize_Should_Handle_Text_Without_PII()
    {
        var input = "This is a normal error message without any sensitive information.";
        var result = _sanitizer.Sanitize(input);
        result.Should().Be(input);
    }

    [Fact]
    public void Sanitize_Should_Be_Case_Insensitive_For_API_Keys()
    {
        var input1 = "API_KEY=abcdefghijklmnopqrstuvwxyz";
        var input2 = "api_key=abcdefghijklmnopqrstuvwxyz";
        var input3 = "Api_Key=abcdefghijklmnopqrstuvwxyz";

        _sanitizer.Sanitize(input1).Should().Contain("[KEY_REDACTED]");
        _sanitizer.Sanitize(input2).Should().Contain("[KEY_REDACTED]");
        _sanitizer.Sanitize(input3).Should().Contain("[KEY_REDACTED]");
    }
}
