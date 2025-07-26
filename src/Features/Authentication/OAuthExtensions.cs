using Aptabase.Features;
using Aptabase.Features.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Authentication;

public static class OAuthExtensions
{
    public class GitHubUser
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("login")]
        public string Login { get; set; } = "";
        [JsonPropertyName("email")]
        public string Email { get; set; } = "";
    }
    
    public class GitHubEmail
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = "";
        [JsonPropertyName("primary")]
        public bool Primary { get; set; }
        [JsonPropertyName("verified")]
        public bool Verified { get; set; }
    }

    public class GoogleUser
    {
        [JsonPropertyName("sub")]
        public string Id { get; set; } = "";
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("email")]
        public string Email { get; set; } = "";
        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; set; }
    }

    public class AuthentikUser
    {
        [JsonPropertyName("sub")]
        public string Id { get; set; } = "";
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("email")]
        public string Email { get; set; } = "";
        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; set; }
    }

    public static AuthenticationBuilder AddGitHub(this AuthenticationBuilder builder, EnvSettings env)
    {
        if (string.IsNullOrWhiteSpace(env.OAuthGitHubClientId) || string.IsNullOrWhiteSpace(env.OAuthGitHubClientSecret))
            return builder;

        return builder.AddOAuth("github", o =>
        {
            o.ClientId = env.OAuthGitHubClientId;
            o.ClientSecret = env.OAuthGitHubClientSecret;
            o.CallbackPath = new PathString("/api/_auth/github/callback");
            o.Scope.Add("read:user");
            o.Scope.Add("user:email");
            o.CorrelationCookie.SameSite = env.IsDevelopment ? SameSiteMode.Unspecified : SameSiteMode.None;
            o.CorrelationCookie.HttpOnly = true;
            o.CorrelationCookie.IsEssential = true;
            o.CorrelationCookie.SecurePolicy = env.IsDevelopment
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            o.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
            o.TokenEndpoint = "https://github.com/login/oauth/access_token";
            o.UserInformationEndpoint = "https://api.github.com/user";
            o.ClaimActions.MapJsonKey("id", "id");
            o.ClaimActions.MapJsonKey("name", "name");
            o.ClaimActions.MapJsonKey("email", "email");
            o.Events = new OAuthEvents
            {
                OnAccessDenied = context =>
                {
                    context.HandleResponse();
                    context.HttpContext.Response.Redirect($"{env.SelfBaseUrl}/auth");
                    return Task.CompletedTask;
                },
                OnCreatingTicket = async context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OAuthHandler<OAuthOptions>>>();

                    try
                    {
                        var ghUser = await MakeOAuthRequest<GitHubUser>(context, context.Options.UserInformationEndpoint) ?? throw new Exception("Failed to retrieve GitHub user information.");
                        if (string.IsNullOrWhiteSpace(ghUser.Name))
                            ghUser.Name = ghUser.Login;

                        if (string.IsNullOrWhiteSpace(ghUser.Email))
                            ghUser.Email = await GetGitHubPreferredEmail(context);

                        if (string.IsNullOrWhiteSpace(ghUser.Email))
                            throw new Exception("Could not find a verified email, can't login with GitHub.");

                        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                        var user = await authService.FindOrCreateAccountWithOAuthAsync(ghUser.Name, ghUser.Email, "github", ghUser.Id.ToString(), context.HttpContext.RequestAborted);
                        context.RunClaimActions(JsonSerializer.SerializeToElement(new { id = user.Id, name = user.Name, email = user.Email }));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to create OAuth ticket for Google user");
                        throw;
                    }
                },
                OnRemoteFailure = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OAuthHandler<OAuthOptions>>>();

                    logger.LogError("OAuth Remote Failure: {message}", context.Failure?.Message);
                    context.Response.Redirect($"{env.SelfBaseUrl}/auth?error=oauth_failed");
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
    }

    public static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder, EnvSettings env)
    {
        if (string.IsNullOrWhiteSpace(env.OAuthGoogleClientId) || string.IsNullOrWhiteSpace(env.OAuthGoogleClientSecret))
            return builder;

        return builder.AddOAuth("google", o =>
        {
            o.ClientId = env.OAuthGoogleClientId;
            o.ClientSecret = env.OAuthGoogleClientSecret;
            o.Scope.Add("openid");
            o.Scope.Add("profile");
            o.Scope.Add("email");
            o.CallbackPath = new PathString("/api/_auth/google/callback");
            o.CorrelationCookie.SameSite = env.IsDevelopment ? SameSiteMode.Unspecified : SameSiteMode.None;
            o.CorrelationCookie.HttpOnly = true;
            o.CorrelationCookie.IsEssential = true;
            o.CorrelationCookie.SecurePolicy = env.IsDevelopment
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            o.AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
            o.TokenEndpoint = "https://oauth2.googleapis.com/token";
            o.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";
            o.ClaimActions.MapJsonKey("id", "id");
            o.ClaimActions.MapJsonKey("name", "name");
            o.ClaimActions.MapJsonKey("email", "email");
            o.Events = new OAuthEvents
            {
                OnAccessDenied = context =>
                {
                    context.HandleResponse();
                    context.HttpContext.Response.Redirect($"{env.SelfBaseUrl}/auth");
                    return Task.CompletedTask;
                },
                OnCreatingTicket = async context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OAuthHandler<OAuthOptions>>>();

                    try
                    {
                        var googleUser = await MakeOAuthRequest<GoogleUser>(context, context.Options.UserInformationEndpoint) ?? throw new Exception("Failed to retrieve Google user information.");
                        if (!googleUser.EmailVerified)
                            throw new Exception("Email not verified, can't login with Google.");

                        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                        var user = await authService.FindOrCreateAccountWithOAuthAsync(googleUser.Name, googleUser.Email, "google", googleUser.Id, context.HttpContext.RequestAborted);
                        context.RunClaimActions(JsonSerializer.SerializeToElement(new { id = user.Id, name = user.Name, email = user.Email }));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to create OAuth ticket for Google user");
                        throw;
                    }
                },
                OnRemoteFailure = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OAuthHandler<OAuthOptions>>>();

                    logger.LogError("OAuth Remote Failure: {message}", context.Failure?.Message);
                    context.Response.Redirect($"{env.SelfBaseUrl}/auth?error=oauth_failed");
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
    }

    public static AuthenticationBuilder AddAuthentik(this AuthenticationBuilder builder, EnvSettings env)
    {
        if (string.IsNullOrWhiteSpace(env.OAuthAuthentikClientId) || 
            string.IsNullOrWhiteSpace(env.OAuthAuthentikClientSecret) ||
            string.IsNullOrWhiteSpace(env.OAuthAuthentikAuthorizeUrl) ||
            string.IsNullOrWhiteSpace(env.OAuthAuthentikTokenUrl) ||
            string.IsNullOrWhiteSpace(env.OAuthAuthentikUserinfoUrl))
            return builder;

        return builder.AddOAuth("authentik", o =>
        {
            o.ClientId = env.OAuthAuthentikClientId;
            o.ClientSecret = env.OAuthAuthentikClientSecret;
            o.Scope.Add("openid");
            o.Scope.Add("profile");
            o.Scope.Add("email");
            o.CallbackPath = new PathString("/api/_auth/authentik/callback");
            o.CorrelationCookie.SameSite = env.IsDevelopment ? SameSiteMode.Unspecified : SameSiteMode.None;
            o.CorrelationCookie.HttpOnly = true;
            o.CorrelationCookie.IsEssential = true;
            o.CorrelationCookie.SecurePolicy = env.IsDevelopment
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            o.AuthorizationEndpoint = env.OAuthAuthentikAuthorizeUrl;
            o.TokenEndpoint = env.OAuthAuthentikTokenUrl;
            o.UserInformationEndpoint = env.OAuthAuthentikUserinfoUrl;
            o.ClaimActions.MapJsonKey("id", "id");
            o.ClaimActions.MapJsonKey("name", "name");
            o.ClaimActions.MapJsonKey("email", "email");
            o.Events = new OAuthEvents
            {
                OnAccessDenied = context =>
                {
                    context.HandleResponse();
                    context.HttpContext.Response.Redirect($"{env.SelfBaseUrl}/auth");
                    return Task.CompletedTask;
                },
                OnCreatingTicket = async context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OAuthHandler<OAuthOptions>>>();

                    try
                    {
                        var authentikUser = await MakeOAuthRequest<AuthentikUser>(context, context.Options.UserInformationEndpoint) ?? throw new Exception("Failed to retrieve Authentik user information.");
                        if (!authentikUser.EmailVerified)
                            throw new Exception("Email not verified, can't login with Authentik.");

                        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                        logger.LogWarning("Authentik user: {Name}, {Email}, {Id}", authentikUser.Name, authentikUser.Email, authentikUser.Id);
                        var user = await authService.FindOrCreateAccountWithOAuthAsync(authentikUser.Name, authentikUser.Email, "authentik", authentikUser.Id, context.HttpContext.RequestAborted);
                        context.RunClaimActions(JsonSerializer.SerializeToElement(new { id = user.Id, name = user.Name, email = user.Email }));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to create OAuth ticket for Authentik user");
                        throw;
                    }
                },
                OnRemoteFailure = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OAuthHandler<OAuthOptions>>>();

                    logger.LogError("OAuth Remote Failure: {message}", context.Failure?.Message);
                    context.Response.Redirect($"{env.SelfBaseUrl}/auth?error=oauth_failed");
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
    }

    private static async Task<T?> MakeOAuthRequest<T>(OAuthCreatingTicketContext context, string endpoint)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
        using var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private static async Task<string> GetGitHubPreferredEmail(OAuthCreatingTicketContext context)
    {
        var emails = await MakeOAuthRequest<GitHubEmail[]>(context, "https://api.github.com/user/emails");
        return emails?.Where(e => e.Verified).OrderBy(e => e.Primary ? 0 : 1).FirstOrDefault()?.Email ?? "";
    }

}