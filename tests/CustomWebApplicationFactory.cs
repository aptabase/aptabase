using Aptabase.Data;
using Aptabase.Features;
using FluentMigrator.Runner;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Aptabase.IntegrationTests;

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

        builder.ConfigureServices(services =>
        {
            var sp = services.BuildServiceProvider();
            Program.RunMigrations(sp);
        });
    }
}