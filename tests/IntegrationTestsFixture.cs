using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aptabase.IntegrationTests;

public class IntegrationTestsFixture : IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    public IntegrationTestsFixture()
    {
        _factory = new CustomWebApplicationFactory<Program>();
    }

    public HttpClient CreateClient()
    {
        var opts = new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        };
        return _factory.CreateClient(opts);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}


[CollectionDefinition("Integration Tests")]
public class IntegrationTests : ICollectionFixture<IntegrationTestsFixture>
{
    
}