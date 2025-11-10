using System.Text;

namespace Aptabase.Features;

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

    // The full connection string to ClickHouse using .NET format
    // E.g: Host=my.clickhouse;Protocol=https;Port=12345;Username=user
    // Variable Name: CLICKHOUSE_URL
    public string ClickHouseConnectionString { get; private set; } = "";

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
    public byte[] AuthSecret { get; private set; } = [];

    public string? MailCatcherConnectionString { get; private set; }

    // The host of the SMTP server
    // E.g: smtp.someprovider.com
    // Variable Name: SMTP_HOST
    public string SmtpHost { get; private set; } = "";

    // The port used for sending emails via SMTP
    // E.g: 587
    // Variable Name: SMTP_PORT
    public int SmtpPort { get; private set; } = 0;

    // The username for the SMTP server
    // Variable Name: SMTP_USERNAME
    public string SmtpUsername { get; private set; } = "";

    // The password for the SMTP server
    // Variable Name: SMTP_PASSWORD
    public string SmtpPassword { get; private set; } = "";

    // Address to send the email from
    // E.g.: notification@yourdomain.com 
    // Variable Name: SMTP_FROM_ADDRESS
    public string SmtpFromAddress { get; private set; } = "";

    // The GitHub Client ID for OAuth
    // Variable Name: OAUTH_GITHUB_CLIENT_ID
    public string OAuthGitHubClientId { get; private set; } = "";

    // The GitHub Client Secret for OAuth
    // Variable Name: OAUTH_GITHUB_CLIENT_SECRET
    public string OAuthGitHubClientSecret { get; private set; } = "";

    // The Google Client ID for OAuth
    // Variable Name: OAUTH_GOOGLE_CLIENT_ID
    public string OAuthGoogleClientId { get; private set; } = "";

    // The Google Client Secret for OAuth
    // Variable Name: OAUTH_GOOGLE_CLIENT_SECRET
    public string OAuthGoogleClientSecret { get; private set; } = "";

    // The Authentik Client ID for OAuth
    // Variable Name: OAUTH_AUTHENTIK_CLIENT_ID
    public string OAuthAuthentikClientId { get; private set; } = "";

    // The Authentik Client Secret for OAuth
    // Variable Name: OAUTH_AUTHENTIK_CLIENT_SECRET
    public string OAuthAuthentikClientSecret { get; private set; } = "";

    // The Authentik Authorize URL for OAuth
    // Variable Name: OAUTH_AUTHENTIK_AUTHORIZE_URL
    public string OAuthAuthentikAuthorizeUrl { get; private set; } = "";

    // The Authentik Token URL for OAuth
    // Variable Name: OAUTH_AUTHENTIK_TOKEN_URL
    public string OAuthAuthentikTokenUrl { get; private set; } = "";

    // The Authentik Userinfo URL for OAuth
    // Variable Name: OAUTH_AUTHENTIK_USERINFO_URL
    public string OAuthAuthentikUserinfoUrl { get; private set; } = "";

    // Whether to disable email-based authentication (magic links)
    // Variable Name: DISABLE_EMAIL_AUTH
    public bool DisableEmailAuth { get; private set; } = false;

    //  The following properties are derived from the other settings
    public bool IsManagedCloud => Region == "EU" || Region == "US";
    public bool IsBillingEnabled => IsManagedCloud || IsDevelopment;
    public bool IsProduction => !IsDevelopment;
    public bool IsDevelopment { get; private set; }
    public string Region { get; private set; } = "";
    public string LemonSqueezyApiKey { get; private set; } = "";
    public string LemonSqueezySigningSecret { get; private set; } = "";
    public string EtcDirectoryPath { get; private set; } = "";

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
            ConnectionString = GetOrNull("ConnectionStrings__postgresdb") ?? MustGet("DATABASE_URL"),
            ClickHouseConnectionString = GetOrNull("ConnectionStrings__clickhousedb") ?? Get("CLICKHOUSE_URL"),
            TinybirdBaseUrl = Get("TINYBIRD_BASE_URL"),
            TinybirdToken = Get("TINYBIRD_TOKEN"),
            AuthSecret = Encoding.ASCII.GetBytes(MustGet("AUTH_SECRET")),
            LemonSqueezyApiKey = Get("LEMONSQUEEZY_API_KEY"),
            LemonSqueezySigningSecret = Get("LEMONSQUEEZY_SIGNING_SECRET"),

            SmtpHost = Get("SMTP_HOST"),
            SmtpPort = GetInt("SMTP_PORT"),
            SmtpUsername = Get("SMTP_USERNAME"),
            SmtpPassword = Get("SMTP_PASSWORD"),
            SmtpFromAddress = Get("SMTP_FROM_ADDRESS"),
            MailCatcherConnectionString = GetOrNull("ConnectionStrings__mailcatcher"),

            OAuthGitHubClientId = Get("OAUTH_GITHUB_CLIENT_ID"),
            OAuthGitHubClientSecret = Get("OAUTH_GITHUB_CLIENT_SECRET"),
            OAuthGoogleClientId = Get("OAUTH_GOOGLE_CLIENT_ID"),
            OAuthGoogleClientSecret = Get("OAUTH_GOOGLE_CLIENT_SECRET"),
            OAuthAuthentikClientId = Get("OAUTH_AUTHENTIK_CLIENT_ID"),
            OAuthAuthentikClientSecret = Get("OAUTH_AUTHENTIK_CLIENT_SECRET"),
            OAuthAuthentikAuthorizeUrl = Get("OAUTH_AUTHENTIK_AUTHORIZE_URL"),
            OAuthAuthentikTokenUrl = Get("OAUTH_AUTHENTIK_TOKEN_URL"),
            OAuthAuthentikUserinfoUrl = Get("OAUTH_AUTHENTIK_USERINFO_URL"),
            DisableEmailAuth = GetBool("DISABLE_EMAIL_AUTH"),

            // On the container, the etc directory is mounted at ./etc
            // But during development, it's at ../etc
            EtcDirectoryPath = Directory.Exists("./etc") ? "./etc" : "../etc"
        };
    }

    private EnvSettings()
    {

    }

    private static string? GetOrNull(string name)
    {
        return Environment.GetEnvironmentVariable(name);
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
        return Environment.GetEnvironmentVariable(name) ?? throw new Exception($"Missing {name} environment variable");
    }

    private static bool GetBool(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value))
            return false;
        if (bool.TryParse(value, out var result))
            return result;
        return false;
    }
}