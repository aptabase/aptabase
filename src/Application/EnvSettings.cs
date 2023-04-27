using System.Text;

namespace Aptabase.Application;

public class EnvSettings
{
    // Required
    public string SelfBaseUrl { get; private set; } = "";
    public string Region { get; private set; } = "";
    public string ConnectionString { get; private set; } = "";
    public string TinybirdBaseUrl { get; private set; } = "";
    public string TinybirdToken { get; private set; } = "";
    public byte[] AuthSecret { get; private set; } = new byte[0];

    public bool IsManagedCloud => Region == "EU" || Region == "US";
    public bool IsProduction => !IsDevelopment;
    public bool IsDevelopment { get; private set; }

    // SMTP
    public string SmtpHost { get; private set; } = "";
    public int SmtpPort { get; private set; } = 0;

    // OAuth (Optional)
    public string OAuthGitHubClientId { get; private set; } = "";
    public string OAuthGitHubClientSecret { get; private set; } = "";
    public string OAuthGoogleClientId { get; private set; } = "";
    public string OAuthGoogleClientSecret { get; private set; } = "";

    public static EnvSettings Load()
    {
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        var region = isDevelopment ? "DEV" : Get("REGION").ToUpper();
        if (string.IsNullOrEmpty(region))
            region = "SH"; // Self Hosted

        return new EnvSettings
        {
            IsDevelopment = isDevelopment,
            Region = region,
            SelfBaseUrl = MustGet("BASE_URL"),
            ConnectionString = MustGet("DATABASE_URL"),
            TinybirdBaseUrl = MustGet("TINYBIRD_BASE_URL"),
            TinybirdToken = MustGet("TINYBIRD_TOKEN"),
            AuthSecret = Encoding.ASCII.GetBytes(MustGet("AUTH_SECRET")),

            SmtpHost = Get("SMTP_HOST"),
            SmtpPort = GetInt("SMTP_PORT"),

            OAuthGitHubClientId = Get("OAUTH_GITHUB_CLIENT_ID"),
            OAuthGitHubClientSecret = Get("OAUTH_GITHUB_CLIENT_SECRET"),
            OAuthGoogleClientId = Get("OAUTH_GOOGLE_CLIENT_ID"),
            OAuthGoogleClientSecret = Get("OAUTH_GOOGLE_CLIENT_SECRET"),
        };
    }

    private EnvSettings()
    {

    }

    private static string Get(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? "";
    }

    private static int GetInt(string name)
    {
        var value = Environment.GetEnvironmentVariable(name) ?? "";
        if (int.TryParse(value, out var result))
            return result;
        return 0;
    }

    private static string MustGet(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (value == null)
            throw new Exception($"Missing {name} environment variable");
        return value;
    }
}