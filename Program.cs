using FluentMigrator.Runner;
using Aptabase.Migrations;
using Aptabase.Features;
using System.Net.Http.Headers;
using Aptabase.Data;
using Aptabase.Features.Ingestion;
using Aptabase.Features.Authentication;
using Aptabase.Features.Billing.LemonSqueezy;
using System.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Aptabase.Features.Notification;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Aptabase.Features.Query;
using Aptabase.Features.Blob;
using Aptabase.CronJobs;
using Aptabase.Features.GeoIP;
using ClickHouse.Client.ADO;
using ClickHouse.Client;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var appEnv = EnvSettings.Load();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowAptabaseCom", policy =>
    {
        policy.WithOrigins("https://aptabase.com")
              .WithMethods("GET")
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
    
    options.AddPolicy(name: "AllowAny", policy =>
    {
        policy.AllowAnyOrigin()
              .WithHeaders("content-type", "app-key")
              .WithMethods("POST")
              .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});

if (appEnv.IsManagedCloud)
{
    builder.Services.AddDataProtection()
                    .PersistKeysToAWSSystemsManager("/aptabase/production/aspnet-dataprotection/");
}

builder.Services.AddControllers();
builder.Services.AddResponseCaching();

builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromDays(365);
                    options.Cookie.Name = "auth-session";
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.MaxAge = TimeSpan.FromDays(365);
                }).AddGitHub(appEnv).AddGoogle(appEnv);

builder.Services.AddRateLimiter(c =>
{
    c.RejectionStatusCode = 429;

    c.AddPolicy("SignUp", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.ResolveClientIpAddress(),
        partition => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 4,
            Window = TimeSpan.FromHours(1)
        })
    );

    c.AddPolicy("EventIngestion", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Request.Headers["App-Key"].ToString(),
        partition => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 10000, // avg 3 events per second
            Window = TimeSpan.FromHours(1)
        })
    );
});

builder.Services.AddSingleton(appEnv);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IUserHashService, DailyUserHashService>();
builder.Services.AddSingleton<IAuthTokenManager, AuthTokenManager>();
builder.Services.AddSingleton<IIngestionValidator, IngestionValidator>();
builder.Services.AddSingleton<IBlobService, DatabaseBlobService>();
builder.Services.AddHostedService<PurgeDailySaltsCronJob>();
builder.Services.AddGeoIPClient(appEnv);
builder.Services.AddEmailClient(appEnv);
builder.Services.AddLemonSqueezy(appEnv);

if (!string.IsNullOrEmpty(appEnv.ClickHouseConnectionString))
{
    builder.Services.AddSingleton<IClickHouseConnection>(x => new ClickHouseConnection(appEnv.ClickHouseConnectionString));
    builder.Services.AddSingleton<IClickHouseMigrationRunner, ClickHouseMigrationRunner>();
    builder.Services.AddSingleton<IQueryClient, ClickHouseQueryClient>();
    builder.Services.AddSingleton<IIngestionClient, ClickHouseIngestionClient>();
}
else
{
    builder.Services.AddSingleton<IQueryClient, TinybirdQueryClient>();
    builder.Services.AddSingleton<IIngestionClient, TinybirdIngestionClient>();
    builder.Services.AddHttpClient("Tinybird", client =>
    {
        client.BaseAddress = new Uri(appEnv.TinybirdBaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", appEnv.TinybirdToken);
    });
}

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
var dbFactory = new DbConnectionFactory(appEnv.ConnectionString);
builder.Services.AddSingleton<IDbConnectionFactory>(dbFactory);
builder.Services.AddScoped<IDbConnection>(_sp => dbFactory.Create());

builder.Services.AddFluentMigratorCore().ConfigureRunner(
    r => r.AddPostgres()
          .WithGlobalConnectionString(appEnv.ConnectionString)
          .WithVersionTable(new VersionTable())
          .ScanIn(typeof(Program).Assembly).For.Migrations()
    );

var app = builder.Build();

if (appEnv.IsManagedCloud)
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedProtoHeaderName = "Cloudfront-Forwarded-Proto",
        ForwardedHeaders = ForwardedHeaders.XForwardedProto
    });
}
else
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseRateLimiter();
app.UseCors();
app.UseAuthentication();
app.MapControllers();
app.UseResponseCaching();

if (appEnv.IsProduction)
{
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-store,no-cache");
        }
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            if (ctx.Context.Request.Path.StartsWithSegments("/assets"))
                ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000,immutable");
        }
    });
}

using (var scope = app.Services.CreateScope())
{
    // Execute Postgres migrations
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();

    if (!string.IsNullOrEmpty(appEnv.ClickHouseConnectionString))
    {
        // Execute ClickHouse migrations (if applicable)
        var chRunner = scope.ServiceProvider.GetRequiredService<IClickHouseMigrationRunner>();
        chRunner.MigrateUp();
    }
}

app.Run();
