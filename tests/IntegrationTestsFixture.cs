using Aptabase.IntegrationTests.Clients;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aptabase.IntegrationTests;

public class IntegrationTestsFixture : IAsyncLifetime, IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    public AccountClient UserA { get; private set; } = null!;

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

    public async Task InitializeAsync()
    {
        UserA = new AccountClient(this.CreateClient());
        await UserA.CreateAccount("Jon Snow", $"jon.snow.{Guid.NewGuid()}@got.com");
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}


[CollectionDefinition("Integration Tests")]
public class IntegrationTests : ICollectionFixture<IntegrationTestsFixture>
{
    
}