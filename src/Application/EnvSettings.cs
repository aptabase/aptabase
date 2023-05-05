using System.Text;

namespace Aptabase.Application;

public class EnvSettings
{
    // The base URL of the application, used for generating links
    // E.g: https://analytics.yourdomain.com
    // Variable Name: BASE_URL
    public string SelfBaseUrl { get; private set; } = "";

    // The full connection string to postgres using .NET format
    // E.g: Server=localhost;Port=5444;User Id=aptabase;Password=aptabase_pw;Database=aptabase
    // Variable Name: DATABASE_URL
    public string ConnectionString { get; private set; } = "";

    // The base URI of the Tinybird API
    // E.g: https://api.tinybird.co
    // Variable Name: TINYBIRD_BASE_URL
    public string TinybirdBaseUrl { get; private set; } = "";

    // The token for Tinybird API, must have write and read access
    // E.g: p.eyJ1Ijo...
    // Variable Name: TINYBIRD_TOKEN
    public string TinybirdToken { get; private set; } = "";

    // A random secret key used for signing auth tokens
    // E.g: GMvqFuPEiRZt6RtaB5OT
    // Variable Name: AUTH_SECRET
    public byte[] AuthSecret { get; private set; } = new byte[0];

    // The host of the SMTP server
    // E.g: smtp.someprovider.com
    // Variable Name: SMTP_HOST
    public string SmtpHost { get; private set; } = "";

    // The port used for sending emails via SMTP
    // E.g: 587
    // Variable Name: SMTP_PORT
    public int SmtpPort { get; private set; } = 0;

    // The GitHub Client ID for OAuth (optional)
    // Variable Name: OAUTH_GITHUB_CLIENT_ID
    public string OAuthGitHubClientId { get; private set; } = "";

    // The GitHub Client Secret for OAuth (optional)
    // Variable Name: OAUTH_GITHUB_CLIENT_SECRET
    public string OAuthGitHubClientSecret { get; private set; } = "";

    // The Google Client ID for OAuth (optional)
    // Variable Name: OAUTH_GOOGLE_CLIENT_ID
    public string OAuthGoogleClientId { get; private set; } = "";

    // The Google Client Secret for OAuth (optional)
    // Variable Name: OAUTH_GOOGLE_CLIENT_SECRET
    public string OAuthGoogleClientSecret { get; private set; } = "";


    //  The following properties are derived from the above settings
    public bool IsManagedCloud => Region == "EU" || Region == "US";
    public bool IsProduction => !IsDevelopment;
    public bool IsDevelopment { get; private set; }
    public string Region { get; private set; } = "";

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