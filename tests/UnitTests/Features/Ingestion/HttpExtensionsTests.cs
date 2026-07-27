using Xunit;
using Microsoft.Extensions.Primitives;

namespace Aptabase.UnitTests.Features.Ingestion;

public class HttpExtensionsTests
{
    [Fact]
    public void ResolveClientIpAddressPrioritizesCfConnectingIp()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("Cf-Connecting-Ip", "203.0.113.1");
        context.Request.Headers.Append("X-Real-Ip", "203.0.113.2");
        context.Request.Headers.Append("X-Forwarded-For", "203.0.113.3");
        context.Request.Headers.Append("CloudFront-Viewer-Address", "203.0.113.4:443");
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.5");

        var value = context.ResolveClientIpAddress();

        Assert.Equal("203.0.113.1", value);
    }

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
        context.Request.Headers.Append("CloudFront-Viewer-Address", new StringValues(headerValue));
        var value = context.ResolveClientIpAddress();
        Assert.Equal(expected, value);
    }
}
