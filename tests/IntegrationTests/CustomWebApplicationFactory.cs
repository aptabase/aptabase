using Aptabase.Features.OAuth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aptabase.IntegrationTests;

// Stands in for the public internet: OAuth clients are identified by a metadata document
// the server fetches, and tests must not depend on a real host serving one.
public class FakeClientMetadataFetcher : IClientMetadataFetcher
{
    public const string ClientId = "https://client.test/oauth/metadata.json";
    public const string RedirectUri = "http://127.0.0.1/callback";

    public Task<(string Body, TimeSpan? MaxAge)> FetchAsync(Uri url, CancellationToken cancellationToken)
    {
        if (url.ToString() != ClientId)
            throw new ClientMetadataException("The client metadata document could not be fetched.");

        var body = $$"""
            { "client_id": "{{ClientId}}", "client_name": "Test Client", "redirect_uris": ["{{RedirectUri}}", "https://client.test/callback"] }
            """;
        return Task.FromResult<(string, TimeSpan?)>((body, null));
    }
}

public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("BASE_URL", "http://localhost:3000");
        Environment.SetEnvironmentVariable("DATABASE_URL", "Server=localhost;Port=5432;User Id=aptabase;Password=aptabase_pw;Database=aptabase");
        Environment.SetEnvironmentVariable("CLICKHOUSE_URL", "Host=localhost;Port=8123;Username=aptabase;Password=aptabase_pw");
        Environment.SetEnvironmentVariable("AUTH_SECRET", "qjrp73j3lm3ypZgR07sC9XfviHFP9CensnwEJouYEZw=");
        Environment.SetEnvironmentVariable("SMTP_HOST", "localhost");
        Environment.SetEnvironmentVariable("SMTP_PORT", "1025");
        Environment.SetEnvironmentVariable("SMTP_FROM_ADDRESS", "abc@hi.com");
        Environment.SetEnvironmentVariable("QUERY_API_ENABLED", "true");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IClientMetadataFetcher>();
            services.AddSingleton<IClientMetadataFetcher, FakeClientMetadataFetcher>();
        });
    }
}
