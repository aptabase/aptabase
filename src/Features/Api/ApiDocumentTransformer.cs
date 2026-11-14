using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Aptabase.Features.Api;

/// <summary>
/// Adds what the generator cannot infer to the OpenAPI document: where the API lives,
/// and that every operation needs an OAuth 2.1 bearer token.
/// </summary>
public class ApiDocumentTransformer : IOpenApiDocumentTransformer
{
    private readonly EnvSettings _env;

    public ApiDocumentTransformer(EnvSettings env)
    {
        _env = env;
    }

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Aptabase Query API",
            Version = "v1",
            Description = "Read access to the analytics of your Aptabase apps. Authenticate with an OAuth 2.1 access token: "
                + "interactive clients use the authorization code flow with PKCE, unattended clients use client credentials with a service client created in the app settings. "
                + $"Request tokens with resource={_env.Origin}. "
                + "The app key that ships inside your apps is public and never grants read access.",
        };

        document.Servers = [new OpenApiServer { Url = _env.Origin }];

        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "OAuth 2.1. Discovery document at /.well-known/oauth-authorization-server.",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{_env.Origin}/oauth/authorize"),
                    TokenUrl = new Uri($"{_env.Origin}/oauth/token"),
                    Scopes = Scopes(),
                },
                ClientCredentials = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri($"{_env.Origin}/oauth/token"),
                    Scopes = Scopes(),
                },
            },
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["oauth2"] = scheme;

        document.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("oauth2", document)] = [OAuth.OAuthScopes.StatsRead],
            },
        ];

        return Task.CompletedTask;
    }

    private static Dictionary<string, string> Scopes() => new()
    {
        [OAuth.OAuthScopes.StatsRead] = "Aggregated statistics, app list and usage",
        [OAuth.OAuthScopes.SessionsRead] = "Individual sessions and events, which may contain personal data in event properties",
    };
}
