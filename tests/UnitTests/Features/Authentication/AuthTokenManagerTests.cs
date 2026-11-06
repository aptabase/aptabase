using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Aptabase.Features;
using Aptabase.Features.Authentication;
using AwesomeAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Aptabase.UnitTests.Features.Authentication;

public class AuthTokenManagerTests
{
    private const string Secret = "qjrp73j3lm3ypZgR07sC9XfviHFP9CensnwEJouYEZw=";
    private const string OtherSecret = "c4rI4x8kz5DgKJ1is5Eiu9bNncSQ6ROD";

    private static AuthTokenManager CreateManager(string secret = Secret, string region = "SH")
    {
        var env = (EnvSettings)Activator.CreateInstance(typeof(EnvSettings), nonPublic: true)!;
        typeof(EnvSettings).GetProperty(nameof(EnvSettings.AuthSecret))!.SetValue(env, Encoding.ASCII.GetBytes(secret));
        typeof(EnvSettings).GetProperty(nameof(EnvSettings.Region))!.SetValue(env, region);
        return new AuthTokenManager(env);
    }

    [Fact]
    public void Round_trips_type_name_email_and_a_unique_id()
    {
        var manager = CreateManager();

        var token = manager.CreateAuthToken(AuthTokenType.SignIn, "Dana", "dana@example.com");
        var parsed = manager.ParseAuthToken(token);

        parsed.Type.Should().Be(AuthTokenType.SignIn);
        parsed.Name.Should().Be("Dana");
        parsed.Email.Should().Be("dana@example.com");
        parsed.Id.Should().NotBeNullOrWhiteSpace();
        parsed.Id.Length.Should().BeLessThanOrEqualTo(22);
        parsed.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Every_token_gets_a_different_id()
    {
        var manager = CreateManager();

        var first = manager.ParseAuthToken(manager.CreateAuthToken(AuthTokenType.SignIn, "Dana", "dana@example.com"));
        var second = manager.ParseAuthToken(manager.CreateAuthToken(AuthTokenType.SignIn, "Dana", "dana@example.com"));

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Rejects_token_signed_with_a_different_secret()
    {
        var token = CreateManager(OtherSecret).CreateAuthToken(AuthTokenType.SignIn, "Dana", "dana@example.com");

        var act = () => CreateManager(Secret).ParseAuthToken(token);

        act.Should().Throw<SecurityTokenException>();
    }

    [Fact]
    public void Rejects_token_issued_for_a_different_region()
    {
        var token = CreateManager(Secret, "EU").CreateAuthToken(AuthTokenType.SignIn, "Dana", "dana@example.com");

        var act = () => CreateManager(Secret, "SH").ParseAuthToken(token);

        act.Should().Throw<SecurityTokenException>();
    }

    [Fact]
    public void Rejects_token_without_an_id()
    {
        var handler = new JwtSecurityTokenHandler();
        var legacyToken = handler.WriteToken(handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "aptabase-sh",
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("type", "SignIn"),
                new Claim("name", "Dana"),
                new Claim("email", "dana@example.com"),
            }),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Secret)), SecurityAlgorithms.HmacSha256),
        }));

        var act = () => CreateManager().ParseAuthToken(legacyToken);

        act.Should().Throw<SecurityTokenException>().WithMessage("*no valid id*");
    }

    [Fact]
    public void Rejects_unsigned_token()
    {
        var handler = new JwtSecurityTokenHandler();
        var unsigned = handler.WriteToken(new JwtSecurityToken(
            issuer: "aptabase-sh",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, "abc"),
                new Claim("type", "SignIn"),
                new Claim("name", "Dana"),
                new Claim("email", "dana@example.com"),
            },
            expires: DateTime.UtcNow.AddMinutes(15)));

        var act = () => CreateManager().ParseAuthToken(unsigned);

        act.Should().Throw<SecurityTokenException>();
    }
}
