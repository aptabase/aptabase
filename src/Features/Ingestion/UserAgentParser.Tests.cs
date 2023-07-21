using Xunit;

namespace Aptabase.Features.Ingestion;

public class UserAgentParserTests
{
    [Theory]
    [InlineData("macOS", "", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36")]
    [InlineData("iOS", "", "Mozilla/5.0 (iPhone14,3; U; CPU iPhone OS 15_0 like Mac OS X) AppleWebKit/602.1.50 (KHTML, like Gecko) Version/10.0 Mobile/19A346 Safari/602.1")]
    [InlineData("iOS", "", "Mozilla/5.0 (Apple-iPhone7C2/1202.466; U; CPU like Mac OS X; en) AppleWebKit/420+ (KHTML, like Gecko) Version/3.0 Mobile/1A543 Safari/419.3")]
    [InlineData("Android", "", "Mozilla/5.0 (Windows Phone 10.0; Android 6.0.1; Microsoft; RM-1152) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Mobile Safari/537.36 Edge/15.15254")]
    [InlineData("Windows", "", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36")]
    [InlineData("Linux", "", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36")]
    [InlineData("Chrome OS", "", "Mozilla/5.0 (X11; CrOS armv7l 13597.84.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.105 Safari/537.36")]
    [InlineData("Fedora", "", "Mozilla/5.0 (X11; Fedora; Linux x86_64; rv:111.0) Gecko/20100101 Firefox/111.0")]
    [InlineData("iPadOS", "", "Mozilla/5.0 (iPad; CPU OS 16_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.3 Mobile/15E148 Safari/604.1")]
    [InlineData("Android", "", "Mozilla/5.0 (Linux; Android 10; HD1913) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.5563.57 Mobile Safari/537.36 EdgA/110.0.1587.66")]
    public void ParseOperatingSystem(string expectedName, string expectedVersion, string userAgent)
    {
        var (name, version) = UserAgentParser.ParseOperatingSystem(userAgent);
        Assert.Equal(expectedName, name);
        Assert.Equal(expectedVersion, version);
    }

    [Theory]
    [InlineData("Firefox", "102.0", "Mozilla/5.0 (X11; Linux x86_64; rv:102.0) Gecko/20100101 Firefox/102.0")]
    [InlineData("Chrome", "111.0.0.0", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36")]
    [InlineData("Safari", "16.3", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.3 Safari/605.1.15")]
    [InlineData("Opera", "96.0.0.0", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36 OPR/96.0.0.0")]
    [InlineData("Edge", "111.0.1661.44", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36 Edg/111.0.1661.44")]
    [InlineData("Yandex Browser", "23.1.5.708", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 YaBrowser/23.1.5.708 Yowser/2.5 Safari/537.36")]
    [InlineData("Yandex Browser", "19.6.0.158", "Mozilla/5.0 (Linux; Android 6.0; Philips X818 Build/MRA58K; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/83.0.4103.101 YaBrowser/19.6.0.158 (lite) Mobile Safari/537.36T")]
    [InlineData("Opera", "16.0.15.124050", "Mozilla/5.0 (iPhone; CPU iPhone OS 16_1_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) OPiOS/16.0.15.124050 Mobile/15E148 Safari/9537.53")]
    [InlineData("", "", "")]
    public void ParserBrowser(string expectedName, string expectedVersion, string userAgent)
    {
        var (name, version) = UserAgentParser.ParseBrowser(userAgent);
        Assert.Equal(expectedName, name);
        Assert.Equal(expectedVersion, version);
    }
}