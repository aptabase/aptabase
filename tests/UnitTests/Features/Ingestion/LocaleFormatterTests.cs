using Xunit;
using Aptabase.Features.Ingestion;

namespace Aptabase.UnitTests.Features.Ingestion;

public class LocaleFormatterTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("C", "")]
    [InlineData("en_001", "en")]
    [InlineData("en_150", "en")]
    [InlineData("en_US_POSIX", "en")]
    [InlineData("en-US-POSIX", "en")]
    [InlineData("EN", "en")]
    [InlineData("en-us", "en-us")]
    [InlineData("sr-Latn-BA", "sr-latn-ba")]
    [InlineData("zh-Hans-HK", "zh-hans-hk")]
    [InlineData("es_MX", "es-mx")]
    [InlineData("es-MX", "es-mx")]
    [InlineData("nl_nl", "nl-nl")]
    [InlineData("nl-NL", "nl-nl")]
    [InlineData("es-419", "es-419")]
    [InlineData("zh-Hans", "zh-hans")]
    [InlineData("English", null)]
    [InlineData("nlNL", null)]
    public void FormatLocale(string? locale, string? expectedLocale)
    {
        var result = LocaleFormatter.FormatLocale(locale);
        Assert.Equal(expectedLocale, result);
    }
}