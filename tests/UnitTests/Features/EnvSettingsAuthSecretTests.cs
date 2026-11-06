using Aptabase.Features;
using AwesomeAssertions;
using Xunit;

namespace Aptabase.UnitTests.Features;

public class EnvSettingsAuthSecretTests
{
    private const string SelfHostingExampleSecret = "c4rI4x8kz5DgKJ1is5Eiu9bNncSQ6ROD";
    private const string LaunchSettingsExampleSecret = "E9F5C811CD74B9A3921A5DB502C0EDBE64B746000663AF04D91C249687FA969B";

    [Fact]
    public void Accepts_a_long_random_secret()
    {
        var secret = "qjrp73j3lm3ypZgR07sC9XfviHFP9CensnwEJouYEZw=";

        EnvSettings.ValidateAuthSecret(secret, isDevelopment: false).Should().Be(secret);
    }

    [Fact]
    public void Trims_surrounding_whitespace()
    {
        var secret = "qjrp73j3lm3ypZgR07sC9XfviHFP9CensnwEJouYEZw=";

        EnvSettings.ValidateAuthSecret($"  {secret}\n", isDevelopment: false).Should().Be(secret);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("0123456789abcdefghijklmnopqrstu")] // 31 chars
    public void Rejects_secrets_shorter_than_32_characters(string secret)
    {
        var act = () => EnvSettings.ValidateAuthSecret(secret, isDevelopment: false);

        act.Should().Throw<Exception>().WithMessage("*at least 32 characters*");
    }

    [Fact]
    public void Accepts_exactly_32_characters()
    {
        var secret = "0123456789abcdefghijklmnopqrstuv";

        EnvSettings.ValidateAuthSecret(secret, isDevelopment: false).Should().Be(secret);
    }

    [Theory]
    [InlineData(SelfHostingExampleSecret)]
    [InlineData(LaunchSettingsExampleSecret)]
    public void Rejects_publicly_known_example_secrets_outside_development(string secret)
    {
        var act = () => EnvSettings.ValidateAuthSecret(secret, isDevelopment: false);

        act.Should().Throw<Exception>().WithMessage("*publicly known example value*");
    }

    [Fact]
    public void Rejects_known_example_secret_when_padded_with_whitespace()
    {
        var act = () => EnvSettings.ValidateAuthSecret($" {SelfHostingExampleSecret} ", isDevelopment: false);

        act.Should().Throw<Exception>().WithMessage("*publicly known example value*");
    }

    [Fact]
    public void Tolerates_launch_settings_example_secret_in_development()
    {
        EnvSettings.ValidateAuthSecret(LaunchSettingsExampleSecret, isDevelopment: true)
            .Should().Be(LaunchSettingsExampleSecret);
    }

    [Fact]
    public void Still_enforces_minimum_length_in_development()
    {
        var act = () => EnvSettings.ValidateAuthSecret("short", isDevelopment: true);

        act.Should().Throw<Exception>().WithMessage("*at least 32 characters*");
    }
}
