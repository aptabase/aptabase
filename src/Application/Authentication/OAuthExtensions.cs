using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aptabase.Application;
using Aptabase.Application.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace Microsoft.AspNetCore.Authentication;

public static class OAuthExtensions
{
    public class GitHubUser
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("email")]
        public string Email { get; set; } = "";
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

    public static AuthenticationBuilder AddGitHub(this AuthenticationBuilder builder, EnvSettings env)
    {
        if (string.IsNullOrWhiteSpace(env.OAuthGitHubClientId))
            return builder;

        return builder.AddOAuth("github", o =>
        {
            o.ClientId = env.OAuthGitHubClientId;
            o.ClientSecret = env.OAuthGitHubClientSecret;
            o.CallbackPath = new PathString("/api/_auth/github/callback");
            o.CorrelationCookie.SameSite = SameSiteMode.Unspecified;
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
                OnCreatingTicket = async context =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                    var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                    response.EnsureSuccessStatusCode();

                    var ghUser = await response.Content.ReadFromJsonAsync<GitHubUser>();
                    if (ghUser is null)
                        throw new Exception("Failed to retrieve GitHub user information.");

                    var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                    var user = await authService.FindOrCreateAccountWithOAuth(ghUser.Name, ghUser.Email, "github", ghUser.Id.ToString(), context.HttpContext.RequestAborted);
                    context.RunClaimActions(JsonSerializer.SerializeToElement(new { id = user.Id, name = user.Name, email = user.Email }));
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
            o.CorrelationCookie.SameSite = SameSiteMode.Unspecified;
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
                OnCreatingTicket = async context =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                    var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                    response.EnsureSuccessStatusCode();

                    var googleUser = await response.Content.ReadFromJsonAsync<GoogleUser>();
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
}