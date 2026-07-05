using Xunit;
using Microsoft.Extensions.Primitives;

namespace Aptabase.UnitTests.Features.Ingestion;

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
        context.Request.Headers.Append("CloudFront-Viewer-Address", new StringValues(headerValue));
        var value = context.ResolveClientIpAddress();
        Assert.Equal(expected, value);
    }

    [Fact]
    public void ResolveClientIpAddress_WithClientIpHeader_TakesPriorityOverEverythingElse()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("CF-Connecting-IP", new StringValues("203.0.113.1"));
        context.Request.Headers.Append("X-Real-Ip", new StringValues("10.0.0.1"));
        context.Request.Headers.Append("X-Forwarded-For", new StringValues("10.0.0.2"));

        var value = context.ResolveClientIpAddress("CF-Connecting-IP");

        Assert.Equal("203.0.113.1", value);
    }

    [Fact]
    public void ResolveClientIpAddress_WithClientIpHeaderMissingFromRequest_FallsBackToExistingLogic()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("X-Real-Ip", new StringValues("10.0.0.1"));

        var value = context.ResolveClientIpAddress("CF-Connecting-IP");

        Assert.Equal("10.0.0.1", value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ResolveClientIpAddress_WithoutClientIpHeaderConfigured_BehavesExactlyAsBefore(string? clientIpHeader)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("CF-Connecting-IP", new StringValues("203.0.113.1"));
        context.Request.Headers.Append("X-Real-Ip", new StringValues("10.0.0.1"));

        var value = context.ResolveClientIpAddress(clientIpHeader ?? "");

        Assert.Equal("10.0.0.1", value);
    }
}