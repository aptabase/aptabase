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

    // OAuth will be enabled if at least one pair is configured.
    // Set this callback: https://YOUR_BASE_URL/api/_auth/github/callback

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

    //  The following properties are derived from the other settings
    public bool IsManagedCloud => Region == "EU" || Region == "US";
    public bool IsBillingEnabled => IsManagedCloud || IsDevelopment;
    public bool IsProduction => !IsDevelopment;
    public bool IsDevelopment { get; private set; }
    public string Region { get; private set; } = "";
    public string LemonSqueezyApiKey { get; private set; } = "";
    public string LemonSqueezySigningSecret { get; private set; } = "";
    public string EtcDirectoryPath { get; private set; } = "";

    // Whether per-app error-reporting quota enforcement is active.
    // Variable Name: ERROR_QUOTA_ENABLED (default: false). Not yet enabled.
    public bool ErrorQuotaEnabled { get; private set; }

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
            SelfBaseUrl = NormalizeBaseUrl(MustGet("BASE_URL")),
            ConnectionString = GetOrNull("ConnectionStrings__postgresdb") ?? MustGet("DATABASE_URL"),
            ClickHouseConnectionString = GetOrNull("ConnectionStrings__clickhousedb") ?? Get("CLICKHOUSE_URL"),
            TinybirdBaseUrl = Get("TINYBIRD_BASE_URL"),
            TinybirdToken = Get("TINYBIRD_TOKEN"),
            AuthSecret = Encoding.ASCII.GetBytes(ValidateAuthSecret(MustGet("AUTH_SECRET"), isDevelopment)),
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

            // On the container, the etc directory is mounted at ./etc
            // But during development, it's at ../etc
            EtcDirectoryPath = Directory.Exists("./etc") ? "./etc" : "../etc",

            ErrorQuotaEnabled = GetBool("ERROR_QUOTA_ENABLED")
        };
    }

    private EnvSettings()
    {

    }

    // Minimum length of AUTH_SECRET in bytes. It is the HMAC-SHA256 key that signs
    // sign-in and registration links, so it must carry at least 256 bits of entropy.
    public const int MinAuthSecretLength = 32;

    // AUTH_SECRET values that have been published as examples. Anyone who knows one of
    // these can mint a valid sign-in link for any e-mail address, so a server must
    // refuse to start with them. The first one shipped in aptabase/self-hosting's
    // docker-compose.yml and was copied into several third-party deployment templates.
    private static readonly string[] KnownExampleAuthSecrets =
    {
        "c4rI4x8kz5DgKJ1is5Eiu9bNncSQ6ROD",
        "E9F5C811CD74B9A3921A5DB502C0EDBE64B746000663AF04D91C249687FA969B",
    };

    public static string ValidateAuthSecret(string value, bool isDevelopment)
    {
        var secret = value.Trim();

        if (secret.Length < MinAuthSecretLength)
            throw new Exception($"AUTH_SECRET must be at least {MinAuthSecretLength} characters long. Generate one with: openssl rand -base64 32");

        // Developers copy launchSettings.example.json as-is, so the example value is
        // tolerated when ASPNETCORE_ENVIRONMENT=Development. Everywhere else it is refused.
        if (!isDevelopment && KnownExampleAuthSecrets.Any(known => string.Equals(known, secret, StringComparison.Ordinal)))
            throw new Exception("AUTH_SECRET is set to a publicly known example value, which lets anyone sign in as any user. Generate your own with: openssl rand -base64 32");

        return secret;
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

    private static bool GetBool(string name)
    {
        var value = Environment.GetEnvironmentVariable(name) ?? "";
        return bool.TryParse(value, out var result) && result;
    }

    private static string MustGet(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? throw new Exception($"Missing {name} environment variable");
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        var value = baseUrl.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(value))
            throw new Exception("BASE_URL must not be empty, e.g. https://analytics.yourdomain.com");

        if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            value = $"https://{value}";
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out _))
            throw new Exception("BASE_URL must be an absolute URL, e.g. https://analytics.yourdomain.com");

        return value;
    }
}
