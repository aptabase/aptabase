using System.Net;
using Aptabase.Features.OAuth;
using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Aptabase.UnitTests.Features.OAuth;

public class OAuthClientsTests
{
    [Theory]
    [InlineData("https://claude.ai/oauth/claude-code-client-metadata", true)]
    [InlineData("https://client.example/a/b.json", true)]
    [InlineData("https://client.example", false)]          // no path
    [InlineData("https://client.example/", false)]
    [InlineData("http://client.example/metadata.json", false)]
    [InlineData("https://user:pw@client.example/metadata.json", false)]
    [InlineData("https://client.example/metadata.json#x", false)]
    [InlineData("https://client.example/a/../metadata.json", false)]
    [InlineData("cursor", false)]
    [InlineData("", false)]
    public void Recognises_metadata_document_urls(string clientId, bool expected)
    {
        OAuthClients.IsMetadataDocumentUrl(clientId, out _).Should().Be(expected);
    }

    [Theory]
    [InlineData("https://app.example/callback", true)]
    [InlineData("http://localhost/callback", true)]
    [InlineData("http://127.0.0.1:8123/callback", true)]
    [InlineData("http://[::1]:9000/cb", true)]
    [InlineData("com.example.app:/oauth/callback", true)]
    [InlineData("http://app.example/callback", false)]      // plain http off loopback
    [InlineData("http://localhost.evil.example/callback", false)]
    [InlineData("https://app.example/callback#fragment", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("myapp:/callback", false)]                  // private-use schemes must be reverse-domain
    [InlineData("/relative", false)]
    [InlineData("", false)]
    public void Allows_only_safe_redirect_uri_forms(string uri, bool expected)
    {
        OAuthClients.IsAllowedRedirectUri(uri).Should().Be(expected);
    }

    [Theory]
    // loopback: the port is ignored, everything else must match
    [InlineData("http://localhost/callback", "http://localhost:53124/callback", true)]
    [InlineData("http://127.0.0.1/callback", "http://127.0.0.1:1/callback", true)]
    [InlineData("http://localhost:8787/callback", "http://localhost:9999/callback", true)]
    [InlineData("http://localhost/callback", "http://localhost:53124/other", false)]
    [InlineData("http://localhost/callback", "http://127.0.0.1:53124/callback", false)]
    [InlineData("http://localhost/callback", "https://localhost:53124/callback", false)]
    // everything else: exact string
    [InlineData("https://app.example/callback", "https://app.example/callback", true)]
    [InlineData("https://app.example/callback", "https://app.example:8443/callback", false)]
    [InlineData("https://app.example/callback", "https://app.example/callback/", false)]
    [InlineData("https://app.example/callback", "https://app.example/callback?next=x", false)]
    [InlineData("https://app.example/callback", "https://app.example.evil.test/callback", false)]
    public void Matches_redirect_uris(string registered, string requested, bool expected)
    {
        OAuthClients.RedirectUriMatches([registered], requested).Should().Be(expected);
    }

    [Fact]
    public void Cursor_is_built_in_with_its_documented_redirect_uris()
    {
        var cursor = OAuthClients.FindBuiltIn("cursor");

        cursor.Should().NotBeNull();
        cursor!.Verified.Should().BeTrue();
        OAuthClients.RedirectUriMatches(cursor.RedirectUris, "http://localhost:8787/callback").Should().BeTrue();
        OAuthClients.RedirectUriMatches(cursor.RedirectUris, "https://www.cursor.com/agents/mcp/oauth/callback").Should().BeTrue();
        OAuthClients.RedirectUriMatches(cursor.RedirectUris, "https://cursor.com.evil.test/agents/mcp/oauth/callback").Should().BeFalse();
        OAuthClients.FindBuiltIn("Cursor").Should().BeNull();
    }

    [Theory]
    [InlineData("https://us.aptabase.com", "https://us.aptabase.com")]
    [InlineData("https://us.aptabase.com/", "https://us.aptabase.com")]
    [InlineData("HTTPS://US.Aptabase.com:443/", "https://us.aptabase.com")]
    [InlineData("https://us.aptabase.com/mcp/", "https://us.aptabase.com/mcp")]
    [InlineData("http://localhost:3000/", "http://localhost:3000")]
    public void Normalizes_resource_indicators(string value, string expected)
    {
        OAuthResource.Normalize(value).Should().Be(expected);
    }

    [Theory]
    [InlineData("https://us.aptabase.com?x=1")]
    [InlineData("https://us.aptabase.com/#f")]
    [InlineData("ftp://us.aptabase.com")]
    [InlineData("not a url")]
    [InlineData("")]
    public void Rejects_malformed_resource_indicators(string value)
    {
        OAuthResource.Normalize(value).Should().BeNull();
    }

    [Theory]
    [InlineData("https://us.aptabase.com/", "/mcp", true)]
    [InlineData("https://us.aptabase.com", "/api/v1/apps", true)]
    [InlineData("https://us.aptabase.com/mcp", "/mcp", true)]
    [InlineData("https://us.aptabase.com/mcp", "/api/v1/apps", false)]
    [InlineData("https://us.aptabase.com/api/v1", "/mcp", false)]
    [InlineData("https://eu.aptabase.com", "/mcp", false)]
    [InlineData("https://us.aptabase.com/other", "/mcp", false)]
    public void Audience_is_checked_against_the_surface(string audience, string path, bool expected)
    {
        OAuthResource.AudienceAllowsPath(audience, "https://us.aptabase.com", path).Should().Be(expected);
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("2606:4700:4700::1111", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.1.2.3", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("172.31.255.255", false)]
    [InlineData("172.32.0.1", true)]
    [InlineData("192.168.1.1", false)]
    [InlineData("169.254.169.254", false)]   // cloud metadata
    [InlineData("100.64.0.1", false)]
    [InlineData("0.0.0.0", false)]
    [InlineData("224.0.0.1", false)]
    [InlineData("::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("fc00::1", false)]
    [InlineData("::ffff:10.0.0.1", false)]   // IPv4-mapped private address
    [InlineData("::ffff:8.8.8.8", true)]
    [InlineData("64:ff9b::a00:1", false)]    // NAT64 of 10.0.0.1
    public void Fetcher_only_connects_to_public_addresses(string address, bool expected)
    {
        ClientMetadataFetcher.IsPublicAddress(IPAddress.Parse(address)).Should().Be(expected);
    }

    private class StubFetcher(string body) : IClientMetadataFetcher
    {
        public int Calls { get; private set; }

        public Task<(string Body, TimeSpan? MaxAge)> FetchAsync(Uri url, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<(string, TimeSpan?)>((body, TimeSpan.FromSeconds(1)));
        }
    }

    private const string Url = "https://client.example/metadata.json";

    private static ClientResolver Resolver(IClientMetadataFetcher fetcher)
        => new(fetcher, new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task Resolves_a_metadata_document_and_caches_it()
    {
        var fetcher = new StubFetcher($$"""{"client_id":"{{Url}}","client_name":"My Tool","redirect_uris":["https://client.example/cb","http://evil.example/cb"]}""");
        var resolver = Resolver(fetcher);

        var client = await resolver.ResolveAsync(Url, false, default);
        await resolver.ResolveAsync(Url, false, default);

        client.Name.Should().Be("My Tool");
        client.Verified.Should().BeFalse();
        client.RedirectUris.Should().Equal("https://client.example/cb");
        fetcher.Calls.Should().Be(1, "a max-age below the floor is still cached for an hour");

        await resolver.ResolveAsync(Url, true, default);
        fetcher.Calls.Should().Be(2);
    }

    [Theory]
    [InlineData("""{"client_id":"https://other.example/metadata.json","client_name":"x","redirect_uris":["https://client.example/cb"]}""")]
    [InlineData("""{"client_id":"https://client.example/metadata.json/","client_name":"x","redirect_uris":["https://client.example/cb"]}""")]
    [InlineData("""{"client_id":"https://client.example/metadata.json","redirect_uris":["https://client.example/cb"]}""")]
    [InlineData("""{"client_id":"https://client.example/metadata.json","client_name":"x","redirect_uris":[]}""")]
    [InlineData("""{"client_id":"https://client.example/metadata.json","client_name":"x","redirect_uris":["http://evil.example/cb"]}""")]
    [InlineData("<html>not json</html>")]
    public async Task Rejects_documents_that_do_not_vouch_for_their_own_url(string body)
    {
        var resolver = Resolver(new StubFetcher(body));

        var act = () => resolver.ResolveAsync(Url, false, default);

        await act.Should().ThrowAsync<ClientMetadataException>();
    }

    [Fact]
    public async Task Control_characters_are_stripped_from_the_client_name()
    {
        var resolver = Resolver(new StubFetcher($$"""{"client_id":"{{Url}}","client_name":"Nice\r\nBcc: x@evil.test\u0000 Tool","redirect_uris":["https://client.example/cb"]}"""));

        var client = await resolver.ResolveAsync(Url, false, default);

        client.Name.Should().Be("NiceBcc: x@evil.test Tool");
    }

    [Fact]
    public async Task Built_in_clients_are_never_fetched()
    {
        var fetcher = new StubFetcher("{}");

        var client = await Resolver(fetcher).ResolveAsync("cursor", false, default);

        client.Name.Should().Be("Cursor");
        fetcher.Calls.Should().Be(0);
    }

    [Fact]
    public void Pkce_verifies_s256_only_for_the_right_verifier()
    {
        // RFC 7636 appendix B
        const string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        const string challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

        OAuthCrypto.VerifyS256(verifier, challenge).Should().BeTrue();
        OAuthCrypto.VerifyS256(verifier + "x", challenge).Should().BeFalse();
        OAuthCrypto.IsValidCodeChallenge(challenge).Should().BeTrue();
        OAuthCrypto.IsValidCodeChallenge("short").Should().BeFalse();
        OAuthCrypto.IsValidCodeVerifier("has spaces in it and is long enough to pass length ...").Should().BeFalse();
    }
}
