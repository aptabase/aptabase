using Microsoft.Extensions.Primitives;
using Xunit;

namespace Aptabase.Features.Ingestion;

public class HttpExtensionsTests
{
    [Theory]
    [InlineData(new string[] {}, "")]
    [InlineData(new string[] {""}, "")]
    [InlineData(new string[] {"10.0.0.0"}, "10.0.0.0")]
    [InlineData(new string[] {"10.0.0.0:443"}, "10.0.0.0")]
    [InlineData(new string[] {"10.0.0.0:443", "12.0.0.0:443"}, "10.0.0.0")]
    [InlineData(new string[] {"198.51.100.10:46532"}, "198.51.100.10")]
    [InlineData(new string[] {"[2001:0db8:85a3:0000:0000:8a2e:0370:7334]:8080"}, "[2001:0db8:85a3:0000:0000:8a2e:0370:7334]")]
    public void ResolveClientIpAddress(string[] headerValue, string expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Add("CloudFront-Viewer-Address", new StringValues(headerValue));
        var value = context.ResolveClientIpAddress();
        Assert.Equal(expected, value);
    }
}