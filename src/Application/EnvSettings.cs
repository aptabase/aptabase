using System.Text;

namespace Aptabase.Application;

public class EnvSettings
{
    public string SelfBaseUrl { get; private set; } = "";
    public string Region { get; private set; } = "";
    public string ConnectionString { get; private set; } = "";
    public string TinybirdBaseUrl { get; private set; } = "";
    public string TinybirdToken { get; private set; } = "";
    public string OAuthGitHubClientId { get; private set; } = "";
    public string OAuthGitHubClientSecret { get; private set; } = "";
    public string OAuthGoogleClientId { get; private set; } = "";
    public string OAuthGoogleClientSecret { get; private set; } = "";
    public byte[] AuthSecret { get; private set; } = new byte[0];
    public bool IsProduction => Region != "DEV";
    public bool IsDevelopment => !IsProduction;

    private EnvSettings()
    {

    }

    public static EnvSettings Load()
    {
        return new EnvSettings
        {
            Region = MustGet("REGION").ToUpper(),
            SelfBaseUrl = MustGet("BASE_URL"),
            ConnectionString = MustGet("DATABASE_URL"),
            TinybirdBaseUrl = MustGet("TINYBIRD_BASE_URL"),
            TinybirdToken = MustGet("TINYBIRD_TOKEN"),
            OAuthGitHubClientId = MustGet("OAUTH_GITHUB_CLIENT_ID"),
            OAuthGitHubClientSecret = MustGet("OAUTH_GITHUB_CLIENT_SECRET"),
            OAuthGoogleClientId = MustGet("OAUTH_GOOGLE_CLIENT_ID"),
            OAuthGoogleClientSecret = MustGet("OAUTH_GOOGLE_CLIENT_SECRET"),
            AuthSecret = Encoding.ASCII.GetBytes(MustGet("AUTH_SECRET")),
        };
    }

    private static string MustGet(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (value == null)
            throw new Exception($"Missing {name} environment variable");
        return value;
    }
}