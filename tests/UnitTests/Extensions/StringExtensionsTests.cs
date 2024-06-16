using Xunit;
using FluentAssertions;

namespace Aptabase.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("Hello", "Hello")]
    [InlineData("HelloWorld", "HelloWorld")]
    [InlineData("Hello World", "Hello W...")]
    public void Truncate(string? input, string expected)
    {
        var value = (input ?? "").Truncate(10, "...");

        value.Should().Be(expected);
    }
}