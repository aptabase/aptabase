using Aptabase.Data;
using Aptabase.Data.Migrations;
using Aptabase.Features;
using Aptabase.Features.Apps;
using Aptabase.Features.Authentication;
using Aptabase.Features.Billing;
using Aptabase.Features.Billing.LemonSqueezy;
using Aptabase.Features.Blob;
using Aptabase.Features.GeoIP;
using Aptabase.Features.Ingestion;
using Aptabase.Features.Ingestion.Buffer;
using Aptabase.Features.Notification;
using Aptabase.Features.Privacy;
using Aptabase.Features.Stats;
using ClickHouse.Client.ADO;
using Features.Cache;
using FluentMigrator.Runner;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;

            // Set to 1/4 of the default value to better support mobile devices on slow networks
            options.Limits.MinRequestBodyDataRate = new MinDataRate(bytesPerSecond: 60, gracePeriod: TimeSpan.FromSeconds(10));
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        var appEnv = EnvSettings.Load();

        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

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
                    .WithMethods("POST", "OPTIONS")
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
                        }).AddGitHub(appEnv).AddGoogle(appEnv).AddAuthentik(appEnv);

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

            c.AddPolicy("Stats", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.ResolveClientIpAddress(),
                partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 1000,
                    Window = TimeSpan.FromHours(1)
                })
            );

            c.AddPolicy("EventIngestion", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.ResolveClientIpAddress(),
                partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 20,
                    Window = TimeSpan.FromSeconds(1)
                })
            );

            c.AddPolicy("FeatureFlags", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.ResolveClientIpAddress(),
                partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 10,
                    Window = TimeSpan.FromSeconds(1)
                })
            );
        });

        builder.Services.AddSingleton(appEnv);
        builder.Services.AddHealthChecks();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddSingleton<ICache, DatabaseCache>();
        builder.Services.AddSingleton<IAppQueries, AppQueries>();
        builder.Services.AddSingleton<IBillingQueries, BillingQueries>();
        builder.Services.AddSingleton<IPrivacyQueries, PrivacyQueries>();
        builder.Services.AddSingleton<IUserHasher, DailyUserHasher>();
        builder.Services.AddSingleton<IAuthTokenManager, AuthTokenManager>();
        builder.Services.AddSingleton<IIngestionCache, IngestionCache>();
        builder.Services.AddSingleton<IBlobService, DatabaseBlobService>();
        builder.Services.AddSingleton<IEventBuffer, InMemoryEventBuffer>();
        builder.Services.AddHostedService<EventBackgroundWritter>();
        builder.Services.AddHostedService<PurgeDailySaltsCronJob>();

        if (appEnv.IsBillingEnabled)
        {
            builder.Services.AddHostedService<OveruseNotificationCronJob>();
            builder.Services.AddHostedService<TrialExpiredCronJob>();
            builder.Services.AddHostedService<TrialNotificationCronJob>();
            builder.Services.AddHostedService<ResetOveruseCronJob>();
        }

        builder.Services.AddGeoIPClient(appEnv);
        builder.Services.AddEmailClient(appEnv);
        builder.Services.AddLemonSqueezy(appEnv);

        if (!string.IsNullOrEmpty(appEnv.ClickHouseConnectionString))
        {
            builder.Services.AddSingleton(x => new ClickHouseConnection(appEnv.ClickHouseConnectionString));
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
        builder.Services.AddSingleton<IDbContext, DbContext>();
        builder.Services.AddNpgsqlDataSource(appEnv.ConnectionString);

        builder.Services.AddFluentMigratorCore().ConfigureRunner(
            r => r.AddPostgres()
                .WithGlobalConnectionString(appEnv.ConnectionString)
                .WithVersionTable(new VersionTable())
                .ScanIn(typeof(Program).Assembly).For.Migrations()
            );

        if (appEnv.IsDevelopment)
        {
            builder.AddServiceDefaults();
        }

        var app = builder.Build();
        
        app.UseResponseCompression();

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            // App may be running on HTTP behind a HTTPS proxy/load balancer
            // So we need to use the X-Forwarded-Proto forwarded header
            ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedFor
        });

        if (appEnv.IsProduction)
        {
            app.Use(async (context, next) =>
            {
                context.Request.Scheme = "https";
                await next();
            });
        }

        app.MapHealthChecks("/healthz");
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseRateLimiter();
        app.UseCors();
        app.UseAuthentication();
        app.MapControllers();
        app.UseResponseCaching();

        if (appEnv.IsProduction)
        {
            app.UseHsts();
            app.MapFallbackToFile("index.html", new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("Content-Security-Policy", "default-src 'self' 'unsafe-inline'; img-src 'self' data: https:; style-src 'self' 'unsafe-inline' https://client.crisp.chat; script-src 'self' 'unsafe-inline' https://client.crisp.chat; font-src 'self' https://client.crisp.chat; connect-src 'self' https://raw.githubusercontent.com wss://client.relay.crisp.chat https://client.crisp.chat;");
                    ctx.Context.Response.Headers.Append("X-Frame-Options", "DENY");
                    ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                    ctx.Context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
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

        RunMigrations(app.Services);

        app.Run();
    }

    public static void RunMigrations(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        
        // Execute Postgres migrations
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();

        var env = scope.ServiceProvider.GetRequiredService<EnvSettings>();
        if (!string.IsNullOrEmpty(env.ClickHouseConnectionString))
        {
            // Execute ClickHouse migrations (if applicable)
            var chRunner = scope.ServiceProvider.GetRequiredService<IClickHouseMigrationRunner>();
            chRunner.MigrateUp();
        }
    }
}
