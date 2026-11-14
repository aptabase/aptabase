using System.Data.Common;
using System.Text.Json;
using Aptabase.Data;
using Dapper;
using Microsoft.Extensions.Caching.Memory;

namespace Aptabase.Features.OAuth;

public class OAuthGrant
{
    public const string KindUser = "user";
    public const string KindService = "service";

    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";
    public string SubjectId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string Scopes { get; set; } = "";
    public string? AppIdsJson { get; set; }
    public string? SecretHash { get; set; }
    public string? SecretPrefix { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public DateTime ConsentedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int DeniedCount { get; set; }
    public DateTime? DeniedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Only filled by TouchGrant: app-access denials across all of the subject's grants
    // in the current window.
    public int SubjectDenied { get; set; }

    public bool IsService => Kind == KindService;
    public bool IsRevoked => RevokedAt.HasValue;

    private string[]? _scopeList;
    public string[] ScopeList => _scopeList ??= Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    public bool HasScope(string scope) => ScopeList.Contains(scope);

    private bool _appIdsParsed;
    private string[]? _appIds;

    /// <summary>
    /// The apps this grant may reach, or null for every app the subject can read. This is
    /// an upper bound and never a source of access: live access is checked on top of it.
    /// </summary>
    public string[]? AppIds
    {
        get
        {
            if (!_appIdsParsed)
            {
                _appIds = string.IsNullOrEmpty(AppIdsJson) ? null : JsonSerializer.Deserialize<string[]>(AppIdsJson);
                _appIdsParsed = true;
            }

            return _appIds;
        }
    }

    public bool CoversApp(string appId) => AppIds is null || AppIds.Contains(appId, StringComparer.Ordinal);
}

public class OAuthToken
{
    public const string KindCode = "code";
    public const string KindRefresh = "refresh";

    public string TokenHash { get; set; } = "";
    public string Kind { get; set; } = "";
    public string GrantId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string Resource { get; set; } = "";
    public string? RedirectUri { get; set; }
    public string? CodeChallenge { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? ReplacedByHash { get; set; }

    // Seconds since the token was used, computed by the database so that the grace
    // window does not depend on the clocks of the application instances.
    public double? UsedSecondsAgo { get; set; }
}

public record RefreshOutcome(OAuthGrant Grant, string Resource, string RefreshToken);

public static class RevokedReason
{
    public const string User = "user";
    public const string Owner = "owner";
    public const string TokenReuse = "token_reuse";
    public const string AccessFailures = "access_failures";
    public const string AppTransferred = "app_transferred";
}

public interface IOAuthStore
{
    Task<OAuthGrant?> GetGrant(string id, CancellationToken cancellationToken);
    Task<OAuthGrant?> TouchGrant(string id, CancellationToken cancellationToken);
    Task<OAuthGrant?> FindUserGrant(string subjectId, string clientId, CancellationToken cancellationToken);
    Task<(OAuthGrant Grant, bool Created)> UpsertUserGrant(string subjectId, string clientId, string clientName, string[] scopes, string[]? appIds, CancellationToken cancellationToken);
    Task<OAuthGrant> CreateServiceGrant(string ownerId, string appId, string name, string[] scopes, string secretHash, string secretPrefix, CancellationToken cancellationToken);
    Task<bool> RotateServiceSecret(string grantId, string appId, string secretHash, string secretPrefix, CancellationToken cancellationToken);
    Task<bool> RevokeGrant(string grantId, string reason, CancellationToken cancellationToken);
    Task<IEnumerable<OAuthGrant>> ListUserGrants(string subjectId, CancellationToken cancellationToken);
    Task<IEnumerable<OAuthGrant>> ListServiceGrants(string appId, CancellationToken cancellationToken);
    Task<int> CountLiveServiceGrants(string appId, CancellationToken cancellationToken);

    Task<string> CreateAuthorizationCode(string grantId, ConsentRequest request, CancellationToken cancellationToken);
    Task<OAuthToken?> ConsumeAuthorizationCode(string code, CancellationToken cancellationToken);
    Task<string?> CreateRefreshToken(string grantId, string clientId, string resource, CancellationToken cancellationToken);
    Task<RefreshOutcome?> RotateRefreshToken(string refreshToken, string clientId, CancellationToken cancellationToken);

    Task<int> RecordDenied(string grantId, CancellationToken cancellationToken);
}

public class OAuthStore : IOAuthStore
{
    public const int RefreshGraceSeconds = 60;
    public const int DeniedWindowMinutes = 10;

    private const string GrantColumns = @"id, kind, subject_id, client_id, client_name, scopes, app_ids::text AS app_ids_json,
        secret_hash, secret_prefix, revoked_at, revoked_reason, consented_at, last_used_at, denied_count, denied_at, created_at";

    private const string TokenColumns = @"token_hash, kind, grant_id, client_id, resource, redirect_uri, code_challenge,
        expires_at, used_at, replaced_by_hash, EXTRACT(EPOCH FROM (now() - used_at)) AS used_seconds_ago";

    // Refresh tokens live 30 days from each rotation, never past 90 days from consent
    private const string RefreshExpiry = "LEAST(now() + interval '30 days', g.consented_at + interval '90 days')";

    private readonly IDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;

    public OAuthStore(IDbContext db, IMemoryCache cache, ILogger<OAuthStore> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OAuthGrant?> GetGrant(string id, CancellationToken cancellationToken)
    {
        if (id.Length > 22) return null;

        using var cn = _db.Connection;
        return await cn.QueryFirstOrDefaultAsync<OAuthGrant>(new CommandDefinition(
            $"SELECT {GrantColumns} FROM oauth_grants WHERE id = @id", new { id }, cancellationToken: cancellationToken));
    }

    // The load the bearer gate runs, at most once a minute per grant and instance. Stamping
    // last_used_at in the same statement means "last used" costs nothing extra, and it
    // moves on cache hits and on 403s too.
    public async Task<OAuthGrant?> TouchGrant(string id, CancellationToken cancellationToken)
    {
        if (id.Length > 22) return null;

        using var cn = _db.Connection;
        return await cn.QueryFirstOrDefaultAsync<OAuthGrant>(new CommandDefinition(
            $@"UPDATE oauth_grants SET last_used_at = now()
               WHERE id = @id AND revoked_at IS NULL
               RETURNING {GrantColumns},
                 (SELECT COALESCE(SUM(d.denied_count), 0)::int FROM oauth_grants d
                  WHERE d.subject_id = oauth_grants.subject_id
                  AND d.denied_at > now() - interval '{DeniedWindowMinutes} minutes') AS subject_denied",
            new { id }, cancellationToken: cancellationToken));
    }

    public async Task<OAuthGrant?> FindUserGrant(string subjectId, string clientId, CancellationToken cancellationToken)
    {
        using var cn = _db.Connection;
        return await cn.QueryFirstOrDefaultAsync<OAuthGrant>(new CommandDefinition(
            $@"SELECT {GrantColumns} FROM oauth_grants
               WHERE subject_id = @subjectId AND client_id = @clientId AND kind = 'user' AND revoked_at IS NULL",
            new { subjectId, clientId }, cancellationToken: cancellationToken));
    }

    // Re-consent replaces the live grant in place: same row and id, exactly the scopes and
    // apps the user just approved, a fresh consented_at, and no token issued before it survives.
    public async Task<(OAuthGrant Grant, bool Created)> UpsertUserGrant(string subjectId, string clientId, string clientName, string[] scopes, string[]? appIds, CancellationToken cancellationToken)
    {
        await using var cn = _db.Connection;
        await cn.OpenAsync(cancellationToken);
        await using var tx = await cn.BeginTransactionAsync(cancellationToken);

        var row = await cn.QueryFirstAsync<(string Id, bool Created)>(new CommandDefinition(
            @"INSERT INTO oauth_grants (id, kind, subject_id, client_id, client_name, scopes, app_ids)
              VALUES (@id, 'user', @subjectId, @clientId, @clientName, @scopes, @appIds::jsonb)
              ON CONFLICT (subject_id, client_id) WHERE revoked_at IS NULL AND kind = 'user'
              DO UPDATE SET client_name = EXCLUDED.client_name, scopes = EXCLUDED.scopes, app_ids = EXCLUDED.app_ids,
                            consented_at = now(), modified_at = now()
              RETURNING id, (xmax = 0) AS created",
            new
            {
                id = NanoId.New(),
                subjectId,
                clientId,
                clientName,
                scopes = OAuthScopes.Format(scopes),
                appIds = appIds is null ? null : JsonSerializer.Serialize(appIds),
            }, tx, cancellationToken: cancellationToken));

        await cn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM oauth_tokens WHERE grant_id = @id", new { id = row.Id }, tx, cancellationToken: cancellationToken));

        var grant = await cn.QueryFirstAsync<OAuthGrant>(new CommandDefinition(
            $"SELECT {GrantColumns} FROM oauth_grants WHERE id = @id", new { id = row.Id }, tx, cancellationToken: cancellationToken));

        await tx.CommitAsync(cancellationToken);
        Forget(grant.Id);
        return (grant, row.Created);
    }

    public async Task<OAuthGrant> CreateServiceGrant(string ownerId, string appId, string name, string[] scopes, string secretHash, string secretPrefix, CancellationToken cancellationToken)
    {
        var id = NanoId.New();

        using var cn = _db.Connection;
        return await cn.QueryFirstAsync<OAuthGrant>(new CommandDefinition(
            $@"INSERT INTO oauth_grants (id, kind, subject_id, client_id, client_name, scopes, app_ids, secret_hash, secret_prefix)
               VALUES (@id, 'service', @ownerId, @id, @name, @scopes, @appIds::jsonb, @secretHash, @secretPrefix)
               RETURNING {GrantColumns}",
            new
            {
                id,
                ownerId,
                name,
                scopes = OAuthScopes.Format(scopes),
                appIds = JsonSerializer.Serialize(new[] { appId }),
                secretHash,
                secretPrefix,
            }, cancellationToken: cancellationToken));
    }

    public async Task<bool> RotateServiceSecret(string grantId, string appId, string secretHash, string secretPrefix, CancellationToken cancellationToken)
    {
        using var cn = _db.Connection;
        var rows = await cn.ExecuteAsync(new CommandDefinition(
            @"UPDATE oauth_grants SET secret_hash = @secretHash, secret_prefix = @secretPrefix, modified_at = now()
              WHERE id = @grantId AND kind = 'service' AND revoked_at IS NULL AND app_ids->>0 = @appId",
            new { grantId, appId, secretHash, secretPrefix }, cancellationToken: cancellationToken));
        return rows == 1;
    }

    public async Task<bool> RevokeGrant(string grantId, string reason, CancellationToken cancellationToken)
    {
        await using var cn = _db.Connection;
        await cn.OpenAsync(cancellationToken);
        await using var tx = await cn.BeginTransactionAsync(cancellationToken);
        var revoked = await RevokeGrant(cn, tx, grantId, reason, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        Forget(grantId);
        return revoked;
    }

    // The bearer gate keeps a grant for up to a minute. On the instance that changed it,
    // the change is visible at once; the others catch up when their copy expires.
    private void Forget(string grantId) => _cache.Remove(Api.ApiGate.GrantCacheKey(grantId));

    private static async Task<bool> RevokeGrant(DbConnection cn, DbTransaction tx, string grantId, string reason, CancellationToken cancellationToken)
    {
        var rows = await cn.ExecuteAsync(new CommandDefinition(
            @"UPDATE oauth_grants SET revoked_at = now(), revoked_reason = @reason, modified_at = now()
              WHERE id = @grantId AND revoked_at IS NULL",
            new { grantId, reason }, tx, cancellationToken: cancellationToken));

        await cn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM oauth_tokens WHERE grant_id = @grantId", new { grantId }, tx, cancellationToken: cancellationToken));

        return rows == 1;
    }

    // Revoked grants stay visible for a month so the owner can see why access stopped
    public async Task<IEnumerable<OAuthGrant>> ListUserGrants(string subjectId, CancellationToken cancellationToken)
    {
        using var cn = _db.Connection;
        return await cn.QueryAsync<OAuthGrant>(new CommandDefinition(
            $@"SELECT {GrantColumns} FROM oauth_grants
               WHERE subject_id = @subjectId AND kind = 'user'
               AND (revoked_at IS NULL OR revoked_at > now() - interval '30 days')
               ORDER BY revoked_at DESC NULLS FIRST, consented_at DESC",
            new { subjectId }, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<OAuthGrant>> ListServiceGrants(string appId, CancellationToken cancellationToken)
    {
        using var cn = _db.Connection;
        return await cn.QueryAsync<OAuthGrant>(new CommandDefinition(
            $@"SELECT {GrantColumns} FROM oauth_grants
               WHERE kind = 'service' AND app_ids->>0 = @appId
               AND (revoked_at IS NULL OR revoked_at > now() - interval '30 days')
               ORDER BY revoked_at DESC NULLS FIRST, created_at DESC",
            new { appId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CountLiveServiceGrants(string appId, CancellationToken cancellationToken)
    {
        using var cn = _db.Connection;
        return await cn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM oauth_grants WHERE kind = 'service' AND app_ids->>0 = @appId AND revoked_at IS NULL",
            new { appId }, cancellationToken: cancellationToken));
    }

    public async Task<string> CreateAuthorizationCode(string grantId, ConsentRequest request, CancellationToken cancellationToken)
    {
        var code = OAuthCrypto.RandomToken();

        using var cn = _db.Connection;
        await PurgeExpiredTokens(cn, null, cancellationToken);
        await cn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO oauth_tokens (token_hash, kind, grant_id, client_id, resource, redirect_uri, code_challenge, expires_at)
              VALUES (@hash, 'code', @grantId, @clientId, @resource, @redirectUri, @codeChallenge, now() + interval '5 minutes')",
            new
            {
                hash = OAuthCrypto.Sha256Hex(code),
                grantId,
                clientId = request.ClientId,
                resource = request.Resource,
                redirectUri = request.RedirectUri,
                codeChallenge = request.CodeChallenge,
            }, cancellationToken: cancellationToken));

        return code;
    }

    // Single use by construction: whoever deletes the row gets it, everyone else gets nothing
    public async Task<OAuthToken?> ConsumeAuthorizationCode(string code, CancellationToken cancellationToken)
    {
        using var cn = _db.Connection;
        return await cn.QueryFirstOrDefaultAsync<OAuthToken>(new CommandDefinition(
            @"DELETE FROM oauth_tokens WHERE token_hash = @hash AND kind = 'code' AND expires_at > now()
              RETURNING token_hash, kind, grant_id, client_id, resource, redirect_uri, code_challenge, expires_at",
            new { hash = OAuthCrypto.Sha256Hex(code) }, cancellationToken: cancellationToken));
    }

    public async Task<string?> CreateRefreshToken(string grantId, string clientId, string resource, CancellationToken cancellationToken)
    {
        await using var cn = _db.Connection;
        await cn.OpenAsync(cancellationToken);
        await PurgeExpiredTokens(cn, null, cancellationToken);
        return await InsertRefreshToken(cn, null, grantId, clientId, resource, cancellationToken);
    }

    // Returns null when the grant is revoked or its 90 days are over
    private static async Task<string?> InsertRefreshToken(DbConnection cn, DbTransaction? tx, string grantId, string clientId, string resource, CancellationToken cancellationToken)
    {
        var token = OAuthCrypto.RandomToken();
        var rows = await cn.ExecuteAsync(new CommandDefinition(
            $@"INSERT INTO oauth_tokens (token_hash, kind, grant_id, client_id, resource, expires_at)
               SELECT @hash, 'refresh', g.id, @clientId, @resource, {RefreshExpiry}
               FROM oauth_grants g
               WHERE g.id = @grantId AND g.revoked_at IS NULL
               AND g.consented_at + interval '90 days' > now()",
            new { hash = OAuthCrypto.Sha256Hex(token), grantId, clientId, resource }, tx, cancellationToken: cancellationToken));

        return rows == 1 ? token : null;
    }

    private static Task PurgeExpiredTokens(DbConnection cn, DbTransaction? tx, CancellationToken cancellationToken)
        => cn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM oauth_tokens WHERE expires_at < now() - interval '7 days'", transaction: tx, cancellationToken: cancellationToken));

    // Every use of a refresh token replaces it. Presenting a used one again is how a stolen
    // token shows itself, so it revokes the grant, with one exception: a client that retries
    // within a minute because the response never arrived. The server only keeps hashes, so
    // it cannot return the same pair again. It mints a new one and removes the replacement
    // nobody received, which leaves exactly one live refresh token.
    public async Task<RefreshOutcome?> RotateRefreshToken(string refreshToken, string clientId, CancellationToken cancellationToken)
    {
        var hash = OAuthCrypto.Sha256Hex(refreshToken);

        await using var cn = _db.Connection;
        await cn.OpenAsync(cancellationToken);
        await using var tx = await cn.BeginTransactionAsync(cancellationToken);

        var current = await cn.QueryFirstOrDefaultAsync<OAuthToken>(new CommandDefinition(
            $"SELECT {TokenColumns} FROM oauth_tokens WHERE token_hash = @hash AND kind = 'refresh' FOR UPDATE",
            new { hash }, tx, cancellationToken: cancellationToken));

        if (current is null || !string.Equals(current.ClientId, clientId, StringComparison.Ordinal) || current.ExpiresAt <= DateTime.UtcNow)
        {
            await tx.CommitAsync(cancellationToken);
            return null;
        }

        if (current.UsedAt.HasValue)
        {
            var retried = current.UsedSecondsAgo <= RefreshGraceSeconds && current.ReplacedByHash is not null
                && await cn.ExecuteAsync(new CommandDefinition(
                    "DELETE FROM oauth_tokens WHERE token_hash = @replacement AND kind = 'refresh' AND used_at IS NULL",
                    new { replacement = current.ReplacedByHash }, tx, cancellationToken: cancellationToken)) == 1;

            if (!retried)
            {
                _logger.LogWarning("Refresh token reuse detected, revoking grant {GrantId}", current.GrantId);
                await RevokeGrant(cn, tx, current.GrantId, RevokedReason.TokenReuse, cancellationToken);
                await tx.CommitAsync(cancellationToken);
                Forget(current.GrantId);
                return null;
            }
        }

        await PurgeExpiredTokens(cn, tx, cancellationToken);
        var replacement = await InsertRefreshToken(cn, tx, current.GrantId, current.ClientId, current.Resource, cancellationToken);
        if (replacement is null)
        {
            await tx.CommitAsync(cancellationToken);
            return null;
        }

        await cn.ExecuteAsync(new CommandDefinition(
            "UPDATE oauth_tokens SET used_at = COALESCE(used_at, now()), replaced_by_hash = @replacement WHERE token_hash = @hash",
            new { hash, replacement = OAuthCrypto.Sha256Hex(replacement) }, tx, cancellationToken: cancellationToken));

        var grant = await cn.QueryFirstAsync<OAuthGrant>(new CommandDefinition(
            $"SELECT {GrantColumns} FROM oauth_grants WHERE id = @id", new { id = current.GrantId }, tx, cancellationToken: cancellationToken));

        await tx.CommitAsync(cancellationToken);
        return new RefreshOutcome(grant, current.Resource, replacement);
    }

    // The failure bucket: a fixed window per grant, summed over all of the subject's grants,
    // revoked ones included, so minting a new grant does not reset it. Lives in Postgres
    // because a probe spread over several instances would never trip an in-memory count.
    public async Task<int> RecordDenied(string grantId, CancellationToken cancellationToken)
    {
        using var cn = _db.Connection;
        return await cn.ExecuteScalarAsync<int>(new CommandDefinition(
            $@"WITH bumped AS (
                 UPDATE oauth_grants
                 SET denied_count = CASE WHEN denied_at IS NULL OR denied_at <= now() - interval '{DeniedWindowMinutes} minutes' THEN 1 ELSE denied_count + 1 END,
                     denied_at = CASE WHEN denied_at IS NULL OR denied_at <= now() - interval '{DeniedWindowMinutes} minutes' THEN now() ELSE denied_at END
                 WHERE id = @grantId
                 RETURNING id, subject_id, denied_count
               )
               SELECT (SELECT denied_count FROM bumped)
                    + COALESCE((SELECT SUM(g.denied_count) FROM oauth_grants g, bumped b
                                WHERE g.subject_id = b.subject_id AND g.id <> b.id
                                AND g.denied_at > now() - interval '{DeniedWindowMinutes} minutes'), 0)::int",
            new { grantId }, cancellationToken: cancellationToken));
    }
}
