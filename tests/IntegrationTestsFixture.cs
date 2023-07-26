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
        return _factory.CreateClient();
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