using Dapper;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Aptabase.Application.Authentication;

public enum AuthTokenType
{
    SignIn,
    Register
}

public class ParsedAuthToken
{
    public AuthTokenType Type { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public interface IAuthTokenManager
{
    string CreateAuthToken(AuthTokenType type, string name, string email);
    ParsedAuthToken ParseAuthToken(string token);
}

public class AuthTokenManager : IAuthTokenManager
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler(); 
    private readonly SymmetricSecurityKey _signingKey; 
    private readonly string _issuer; 

    public AuthTokenManager(EnvSettings env)
    {
        _issuer = $"aptabase-{env.Region.ToLower()}";
        _signingKey = new SymmetricSecurityKey(env.AuthSecret);
    }

    public string CreateAuthToken(AuthTokenType type, string name, string email)
    {
        var claims = new[]
        {
            new Claim("type", type.ToString()),
            new Claim("name", name),
            new Claim("email", email)
        };
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _issuer,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(TimeSpan.FromMinutes(15)),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public ParsedAuthToken ParseAuthToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentNullException(nameof(token));

        _tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ValidIssuer = _issuer,
            ValidateIssuer = true,
            IssuerSigningKey = _signingKey,
            ValidateIssuerSigningKey = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        }, out SecurityToken validatedToken);

        var jwtToken = (JwtSecurityToken)validatedToken;

        return new ParsedAuthToken
        {
            Type = (AuthTokenType)Enum.Parse(typeof(AuthTokenType), jwtToken.Claims.First(x => x.Type == "type").Value),
            Name = jwtToken.Claims.First(x => x.Type == "name").Value,
            Email = jwtToken.Claims.First(x => x.Type == "email").Value
        };
    }
}