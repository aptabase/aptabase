using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aptabase.Features;
using Aptabase.Features.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

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
    }

    public static AuthenticationBuilder AddGitHub(this AuthenticationBuilder builder, EnvSettings env)
    {
        if (string.IsNullOrWhiteSpace(env.OAuthGitHubClientId))
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
            o.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
                    var ghUser = await MakeOAuthRequest<GitHubUser>(context, context.Options.UserInformationEndpoint);
                    if (ghUser is null)
                        throw new Exception("Failed to retrieve GitHub user information.");

                    if (string.IsNullOrWhiteSpace(ghUser.Name))
                        ghUser.Name = ghUser.Login;

                    if (string.IsNullOrWhiteSpace(ghUser.Email))
                        ghUser.Email = await GetGitHubPreferredEmail(context);

                    if (string.IsNullOrWhiteSpace(ghUser.Email))
                        throw new Exception("Could not find a verified email, can't login with GitHub.");

                    var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                    var user = await authService.FindOrCreateAccountWithOAuth(ghUser.Name, ghUser.Email, "github", ghUser.Id.ToString(), context.HttpContext.RequestAborted);
                    context.RunClaimActions(JsonSerializer.SerializeToElement(new { id = user.Id, name = user.Name, email = user.Email }));
                }
            };
        });
    }

 public static AuthenticationBuilder AddAuthentik(this AuthenticationBuilder builder, EnvSettings env)
{
   

    return builder.AddOpenIdConnect("authentik", o =>
    {
        o.ClientId = env.OAuthAuthentikClientId;
        o.ClientSecret = env.OAuthAuthentikClientSecret;
        o.Authority = "https://sso.informatik.sexy/application/o/neulandnextanalytics/";
        o.MetadataAddress = "https://sso.informatik.sexy/application/o/neulandnextanalytics/.well-known/openid-configuration";
        o.CallbackPath = new PathString("/api/_auth/authentik/callback");
        o.ResponseType = "code"; // Standard OAuth 2.0 flow

        o.Scope.Add("openid");
        o.Scope.Add("profile");
        o.Scope.Add("email");

        o.SaveTokens = true; // Saves tokens in the authentication ticket
        o.GetClaimsFromUserInfoEndpoint = true;

        o.TokenValidationParameters.ValidateIssuer = true;
        o.TokenValidationParameters.ValidateAudience = true;

        o.CorrelationCookie.SameSite = env.IsDevelopment ? SameSiteMode.Unspecified : SameSiteMode.None;
        o.CorrelationCookie.HttpOnly = true;
        o.CorrelationCookie.IsEssential = true;
        o.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        o.Events = new OpenIdConnectEvents
        {
            OnAccessDenied = context =>
            {
                context.HandleResponse();
                context.HttpContext.Response.Redirect($"{env.SelfBaseUrl}/auth");
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var authentikUser = context.Principal;

                var email = authentikUser?.FindFirst(c => c.Type == "email")?.Value;
                var name = authentikUser?.FindFirst(c => c.Type == "name")?.Value ?? authentikUser?.Identity?.Name;
                var id = authentikUser?.FindFirst(c => c.Type == "sub")?.Value;

                if (string.IsNullOrWhiteSpace(email))
                    throw new Exception("Email not found, can't login with Authentik.");

                var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                var user = await authService.FindOrCreateAccountWithOAuth(name ?? "", email, "authentik", id ?? "", context.HttpContext.RequestAborted);

                // Add custom claims to the identity
                var identity = (System.Security.Claims.ClaimsIdentity)context.Principal.Identity;
                identity.AddClaim(new System.Security.Claims.Claim("id", user.Id.ToString()));
                identity.AddClaim(new System.Security.Claims.Claim("name", user.Name));
                identity.AddClaim(new System.Security.Claims.Claim("email", user.Email));
            }
        };
    });
}


    public static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder, EnvSettings env)
    {
        if (string.IsNullOrWhiteSpace(env.OAuthGoogleClientId))
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
            o.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
                    var googleUser = await MakeOAuthRequest<GoogleUser>(context, context.Options.UserInformationEndpoint);
                    if (googleUser is null)
                        throw new Exception("Failed to retrieve Google user information.");

                    if (!googleUser.EmailVerified)
                        throw new Exception("Email not verified, can't login with Google.");

                    var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                    var user = await authService.FindOrCreateAccountWithOAuth(googleUser.Name, googleUser.Email, "google", googleUser.Id, context.HttpContext.RequestAborted);
                    context.RunClaimActions(JsonSerializer.SerializeToElement(new { id = user.Id, name = user.Name, email = user.Email }));
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