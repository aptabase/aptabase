using Aptabase.Features.Notification;
using Aptabase.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Dapper;

namespace Aptabase.Features.Authentication;

public interface IAuthService
{
    Task<bool> SendSignInEmailAsync(string email, CancellationToken cancellationToken);
    Task SendRegisterEmailAsync(string name, string email, CancellationToken cancellationToken);
    Task SignInAsync(UserAccount user);
    Task SignOutAsync();
    Task<UserAccount?> FindUserById(string id, CancellationToken cancellationToken);
    Task<UserAccount?> FindUserByEmail(string email, CancellationToken cancellationToken);
    Task<UserAccount?> FindUserByOAuthProvider(string providerName, string providerUid, CancellationToken cancellationToken);
    Task<UserAccount> FindOrCreateAccountWithOAuth(string name, string email, string providerName, string providerUid, CancellationToken cancellationToken);
    Task<UserAccount> CreateAccountAsync(string name, string email, CancellationToken cancellationToken);
    Task AttachUserAuthProviderAsync(UserAccount user, string providerName, string providerUid, CancellationToken cancellationToken);
}

public class AuthService : IAuthService
{
    private readonly ILogger _logger;
    private readonly IDbContext _db;
    private readonly IAuthTokenManager _tokenManager;
    private readonly EnvSettings _env;
    private readonly IEmailClient _emailClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        ILogger<AuthService> logger,
        IDbContext db,
        EnvSettings env,
        IHttpContextAccessor httpContextAccessor,
        IEmailClient emailClient,
        IAuthTokenManager tokenManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
        _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
        _db = db ?? throw new ArgumentNullException(nameof(db)); ;
    }

    public async Task<bool> SendSignInEmailAsync(string email, CancellationToken cancellationToken)
    {
        var user = await FindUserByEmail(email, cancellationToken);
        if (user != null)
        {
            var token = _tokenManager.CreateAuthToken(AuthTokenType.SignIn, user.Name, user.Email);
            await _emailClient.SendEmailAsync(email, "Log in to Aptabase", "SignIn", new()
            {
                { "name", user.Name },
                { "url", GenerateAuthUrl(token) }
            }, cancellationToken);

            return true;
        }

        return false;
    }

    public async Task SendRegisterEmailAsync(string name, string email, CancellationToken cancellationToken)
    {
        var user = await FindUserByEmail(email, cancellationToken);
        if (user != null)
        {
            await SendSignInEmailAsync(email, cancellationToken);
            return;
        }

        var token = _tokenManager.CreateAuthToken(AuthTokenType.Register, name, email);

        await _emailClient.SendEmailAsync(email, "Confirm your registration", "Register", new()
        {
            { "name", name },
            { "url", GenerateAuthUrl(token) }
        }, cancellationToken);
    }

    public async Task<UserAccount> CreateAccountAsync(string name, string email, CancellationToken cancellationToken)
    {
        var userId = NanoId.New();
        var cmd = new CommandDefinition("INSERT INTO users (id, name, email) VALUES (@userId, @name, @email)", new { userId, name, email = email.ToLower() }, cancellationToken: cancellationToken);
        await _db.Connection.ExecuteAsync(cmd);

        return new UserAccount(new UserIdentity(userId, name, email));
    }

    public async Task SignOutAsync()
    {
        if (_httpContextAccessor.HttpContext == null)
            return;

        await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task SignInAsync(UserAccount user)
    {
        if (_httpContextAccessor.HttpContext == null)
        {
            _logger.LogError("Tried to authenticate without an active HTTP Context");
            return;
        }

        var claims = new List<Claim>
        {
            new Claim("id", user.Id),
            new Claim("name", user.Name),
            new Claim("email", user.Email),
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            ExpiresUtc = DateTime.UtcNow.AddDays(365),
            IsPersistent = true,
        };

        await _httpContextAccessor.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
    }

    public async Task<UserAccount?> FindUserById(string id, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition($"SELECT id, name, email, lock_reason FROM users WHERE id = @id", new { id }, cancellationToken: cancellationToken);
        return await _db.Connection.QuerySingleOrDefaultAsync<UserAccount>(cmd);
    }

    public async Task<UserAccount?> FindUserByEmail(string email, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition($"SELECT id, name, email, lock_reason FROM users WHERE email = @email", new { email = email.ToLower() }, cancellationToken: cancellationToken);
        return await _db.Connection.QuerySingleOrDefaultAsync<UserAccount>(cmd);
    }

    public async Task<UserAccount> FindOrCreateAccountWithOAuth(string name, string email, string providerName, string providerUid, CancellationToken cancellationToken)
    {
        var user = await this.FindUserByOAuthProvider(providerName, providerUid, cancellationToken);
        if (user is not null)
            return user;

        user = await FindUserByEmail(email, cancellationToken);
        if (user is not null)
        {
            await AttachUserAuthProviderAsync(user, providerName, providerUid, cancellationToken);
            return user;
        }

        user = await CreateAccountAsync(name, email, cancellationToken);
        await AttachUserAuthProviderAsync(user, providerName, providerUid, cancellationToken);
        return user;
    }

    public async Task<UserAccount?> FindUserByOAuthProvider(string providerName, string providerUid, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition($@"
            SELECT u.id, u.name, u.email, u.lock_reason
            FROM user_providers up
            INNER JOIN users u
            ON u.id = up.user_id
            WHERE up.provider_name = @name
            AND up.provider_uid = @uid", new { name = providerName, uid = providerUid }, cancellationToken: cancellationToken);
        return await _db.Connection.QuerySingleOrDefaultAsync<UserAccount>(cmd);
    }

    public Task AttachUserAuthProviderAsync(UserAccount user, string providerName, string providerUid, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition($@"
            INSERT INTO user_providers (provider_name, provider_uid, user_id)
            VALUES (@providerName, @providerUid, @userId)", new { userId = user.Id, providerName, providerUid }, cancellationToken: cancellationToken);
        return _db.Connection.ExecuteAsync(cmd);
    }

    private string GenerateAuthUrl(string token) => $"{_env.SelfBaseUrl}/api/_auth/continue?token={token}";
}