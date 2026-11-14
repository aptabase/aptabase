using Xunit;
using System.Net;
using Microsoft.Extensions.Primitives;

namespace Aptabase.UnitTests.Features.Ingestion;

public class HttpExtensionsTests
{
    private static readonly IPAddress Remote = IPAddress.Parse("203.0.113.5");

    private static IHeaderDictionary Headers(params (string Name, string Value)[] headers)
    {
        var dict = new HeaderDictionary();
        foreach (var (name, value) in headers)
            dict.Append(name, value);
        return dict;
    }

    private static IHeaderDictionary AllProxyHeaders() => Headers(
        ("Cf-Connecting-Ip", "203.0.113.1"),
        ("X-Real-Ip", "203.0.113.2"),
        ("X-Forwarded-For", "203.0.113.3"),
        ("CloudFront-Viewer-Address", "203.0.113.4:443"));

    [Fact]
    public void ResolveClientIpAddressPrioritizesCfConnectingIp()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("Cf-Connecting-Ip", "203.0.113.1");
        context.Request.Headers.Append("X-Real-Ip", "203.0.113.2");
        context.Request.Headers.Append("X-Forwarded-For", "203.0.113.3");
        context.Request.Headers.Append("CloudFront-Viewer-Address", "203.0.113.4:443");
        context.Connection.RemoteIpAddress = Remote;

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

    [Theory]
    [InlineData("X-Forwarded-For", "203.0.113.3")]
    [InlineData("X-Real-Ip", "203.0.113.2")]
    [InlineData("CloudFront-Viewer-Address", "203.0.113.4")]
    public void TrustedHeaderIgnoresEveryOtherHeader(string trustedHeader, string expected)
    {
        var value = ClientIpResolver.Resolve(AllProxyHeaders(), Remote, trustedHeader, 1);

        Assert.Equal(expected, value);
    }

    [Fact]
    public void TrustedHeaderMissingFallsBackToConnectionAddressNotToOtherHeaders()
    {
        var headers = Headers(("Cf-Connecting-Ip", "203.0.113.1"), ("X-Forwarded-For", "203.0.113.3"));

        var value = ClientIpResolver.Resolve(headers, Remote, "X-Real-Ip", 1);

        Assert.Equal("203.0.113.5", value);
    }

    [Theory]
    [InlineData("1.1.1.1, 203.0.113.9", 1, "203.0.113.9")]
    [InlineData("1.1.1.1, 203.0.113.9, 198.51.100.1", 2, "203.0.113.9")]
    [InlineData("203.0.113.9", 1, "203.0.113.9")]
    [InlineData("203.0.113.9", 3, "203.0.113.9")]
    [InlineData("1.1.1.1,203.0.113.9:1234", 1, "203.0.113.9")]
    public void TrustedForwardedForTakesEntryBeforeTrustedProxies(string header, int proxies, string expected)
    {
        var headers = Headers(("X-Forwarded-For", header));

        var value = ClientIpResolver.Resolve(headers, Remote, "X-Forwarded-For", proxies);

        Assert.Equal(expected, value);
    }

    [Fact]
    public void SpoofedPrefixInForwardedForIsIgnoredWhenProxyAppends()
    {
        var headers = Headers(("X-Forwarded-For", "9.9.9.9, 203.0.113.9"));

        var value = ClientIpResolver.Resolve(headers, Remote, "X-Forwarded-For", 1);

        Assert.Equal("203.0.113.9", value);
    }
}
