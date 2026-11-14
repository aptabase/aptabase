using Aptabase.Features.OAuth;

namespace Aptabase.Features.Api;

public static class QueryApiExtensions
{
    public static IServiceCollection AddQueryApi(this IServiceCollection services)
    {
        services.AddSingleton<IOAuthTokenService, OAuthTokenService>();
        services.AddSingleton<IOAuthStore, OAuthStore>();
        services.AddSingleton<IClientMetadataFetcher, ClientMetadataFetcher>();
        services.AddSingleton<IClientResolver, ClientResolver>();
        services.AddSingleton<TokenFailureLimiter>();

        services.AddSingleton<ApiLimits>();
        services.AddSingleton<IApiAccess, ApiAccess>();
        services.AddSingleton<IApiTiers, ApiTiers>();
        services.AddSingleton<IApiMetering, ApiMetering>();
        services.AddSingleton<IApiQueryService, ApiQueryService>();

        // Stateless Streamable HTTP: no session affinity, so it works behind a load balancer
        // with several instances, and every request is authenticated on its own
        services.AddMcpServer(options =>
        {
            options.ServerInfo = new() { Name = "aptabase", Title = "Aptabase", Version = "1.0.0" };
            options.ServerInstructions = "Read-only access to Aptabase app analytics. Call list_apps first to get an app id, then query it. "
                + "Prefer period presets over explicit dates, and daily granularity over hourly.";
        })
        .WithHttpTransport(options => options.Stateless = true)
        .WithTools<McpTools>();

        services.AddOpenApi("v1", options =>
        {
            options.ShouldInclude = description => description.RelativePath?.StartsWith("api/v1", StringComparison.OrdinalIgnoreCase) == true;
            options.AddDocumentTransformer<ApiDocumentTransformer>();
        });

        return services;
    }

    public static WebApplication UseQueryApi(this WebApplication app)
    {
        // In front of /api/v1 and /mcp, after CORS so that a 401 still carries the headers
        // a browser-based client needs to read it
        app.UseWhen(context => ApiGate.Covers(context.Request.Path), branch => branch.UseMiddleware<ApiGate>());

        app.MapMcp("/mcp").RequireCors(OAuthController.PublicCorsPolicy).ExcludeFromDescription();

        app.MapOpenApi(ApiGate.OpenApiPath).RequireCors(OAuthController.PublicCorsPolicy);

        // The single-page app answers every unmapped path with index.html and a 200. An
        // OAuth or MCP client would try to parse that as metadata, so anything under these
        // prefixes that is not a real route is a 404. /oauth/consent is the one page of
        // the app that lives there.
        static IResult NotFound() => Results.NotFound();
        app.Map("/.well-known/{**rest}", NotFound).ExcludeFromDescription();
        app.Map("/oauth/{**rest:regex(^(?!consent$).*)}", NotFound).ExcludeFromDescription();
        app.Map("/mcp/{**rest}", NotFound).ExcludeFromDescription();

        return app;
    }
}
