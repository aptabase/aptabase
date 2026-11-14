using System.Security.Cryptography;
using System.Text;
using Aptabase.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Aptabase.Features.OAuth;

public static class OAuthCrypto
{
    /// <summary>256 bits of randomness, URL-safe.</summary>
    public static string RandomToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public static string Sha256Hex(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static bool FixedTimeEquals(string a, string b)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    public static bool IsValidCodeChallenge(string? value)
        => value is { Length: >= 43 and <= 128 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    public static bool IsValidCodeVerifier(string? value)
        => value is { Length: >= 43 and <= 128 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '.' or '_' or '~');

    public static bool VerifyS256(string verifier, string challenge)
    {
        var computed = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return FixedTimeEquals(computed, challenge);
    }
}

/// <summary>
/// The authorization request as validated by /oauth/authorize. It travels to the consent
/// page inside a signed cookie and nowhere else, so the consent step only works in the
/// browser that started the flow.
/// </summary>
public record ConsentRequest(string ClientId, string RedirectUri, string CodeChallenge, string[] Scopes, string Resource, string? State);

public interface IOAuthTokenService
{
    public const int AccessTokenLifetimeSeconds = 3600;
    public const string ConsentCookieName = "oauth-consent";
    public static readonly TimeSpan ConsentLifetime = TimeSpan.FromMinutes(15);

    string CreateAccessToken(OAuthGrant grant, string audience);
    TokenValidationParameters AccessTokenValidationParameters { get; }

    string ProtectConsentRequest(ConsentRequest request);
    ConsentRequest? UnprotectConsentRequest(string? value);
}

public class OAuthTokenService : IOAuthTokenService
{
    private const string AccessTokenType = "at+jwt";
    private const string ConsentTokenType = "consent+jwt";

    private readonly JsonWebTokenHandler _handler = new() { MapInboundClaims = false };
    private readonly string _issuer;
    private readonly SigningCredentials _accessCredentials;
    private readonly SigningCredentials _consentCredentials;
    private readonly TokenValidationParameters _consentValidation;

    public TokenValidationParameters AccessTokenValidationParameters { get; }

    public OAuthTokenService(EnvSettings env)
    {
        _issuer = env.Origin;

        var accessKey = new SymmetricSecurityKey(env.OAuthSigningKey);
        var consentKey = new SymmetricSecurityKey(env.OAuthConsentKey);
        _accessCredentials = new SigningCredentials(accessKey, SecurityAlgorithms.HmacSha256);
        _consentCredentials = new SigningCredentials(consentKey, SecurityAlgorithms.HmacSha256);

        AccessTokenValidationParameters = new TokenValidationParameters
        {
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidTypes = [AccessTokenType],
            ValidIssuer = _issuer,
            ValidateIssuer = true,
            IssuerSigningKey = accessKey,
            ValidateIssuerSigningKey = true,
            // The audience depends on which surface the request targets, so it is checked
            // by the bearer gate, which knows the request path.
            ValidateAudience = false,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        _consentValidation = new TokenValidationParameters
        {
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidTypes = [ConsentTokenType],
            ValidIssuer = _issuer,
            ValidateIssuer = true,
            IssuerSigningKey = consentKey,
            ValidateIssuerSigningKey = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.Zero,
        };
    }

    public string CreateAccessToken(OAuthGrant grant, string audience)
    {
        var now = DateTime.UtcNow;
        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            TokenType = AccessTokenType,
            Issuer = _issuer,
            Audience = audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddSeconds(IOAuthTokenService.AccessTokenLifetimeSeconds),
            SigningCredentials = _accessCredentials,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = grant.IsService ? grant.Id : grant.SubjectId,
                ["client_id"] = grant.ClientId,
                ["grant_id"] = grant.Id,
                ["scope"] = grant.Scopes,
                ["jti"] = NanoId.New(),
            },
        });
    }

    public string ProtectConsentRequest(ConsentRequest request)
    {
        var now = DateTime.UtcNow;
        var claims = new Dictionary<string, object>
        {
            ["client_id"] = request.ClientId,
            ["redirect_uri"] = request.RedirectUri,
            ["code_challenge"] = request.CodeChallenge,
            ["scope"] = OAuthScopes.Format(request.Scopes),
            ["resource"] = request.Resource,
        };

        if (!string.IsNullOrEmpty(request.State))
            claims["state"] = request.State;

        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            TokenType = ConsentTokenType,
            Issuer = _issuer,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(IOAuthTokenService.ConsentLifetime),
            SigningCredentials = _consentCredentials,
            Claims = claims,
        });
    }

    public ConsentRequest? UnprotectConsentRequest(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var result = _handler.ValidateTokenAsync(value, _consentValidation).GetAwaiter().GetResult();
        if (!result.IsValid || result.SecurityToken is not JsonWebToken token)
            return null;

        string? Claim(string name) => token.TryGetClaim(name, out var claim) ? claim.Value : null;

        var clientId = Claim("client_id");
        var redirectUri = Claim("redirect_uri");
        var challenge = Claim("code_challenge");
        var resource = Claim("resource");
        if (clientId is null || redirectUri is null || challenge is null || resource is null)
            return null;

        if (!OAuthScopes.TryParse(Claim("scope"), out var scopes) || scopes.Length == 0)
            return null;

        return new ConsentRequest(clientId, redirectUri, challenge, scopes, resource, Claim("state"));
    }
}
