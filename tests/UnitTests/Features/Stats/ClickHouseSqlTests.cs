using Xunit;
using AwesomeAssertions;
using Aptabase.Features.Stats;

namespace Aptabase.UnitTests.Features.Stats;

public class ClickHouseSqlTests
{
    [Theory]
    [InlineData("abc123", "'abc123'")]
    [InlineData("", "''")]
    [InlineData("it's", "'it\\'s'")]
    [InlineData("a\\b", "'a\\\\b'")]
    [InlineData("a'b'c", "'a\\'b\\'c'")]
    public void Should_Quote_And_Escape(string input, string expected)
    {
        ClickHouseSql.Literal(input).Should().Be(expected);
    }
}
