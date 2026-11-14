using Aptabase.Data;
using Dapper;
using Microsoft.Extensions.Caching.Memory;

namespace Aptabase.Features.Api;

public class ReadableApp
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string IconPath { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public bool HasOwnership { get; set; }
}

public interface IApiAccess
{
    /// <summary>Every app the user can read right now: owned or shared, not deleted, owner not locked.</summary>
    Task<IReadOnlyList<ReadableApp>> ListReadableApps(string userId, CancellationToken cancellationToken);

    /// <summary>The app, if the user can read it right now. Null for a missing app and for a foreign one alike.</summary>
    Task<ReadableApp?> GetReadableApp(string userId, string appId, CancellationToken cancellationToken);
}

/// <summary>
/// Live access, as opposed to what a grant stored at consent time. Shares are keyed by
/// email, so the subject's current email is read in the same statement instead of being
/// trusted from a token or a cookie.
/// </summary>
public class ApiAccess : IApiAccess
{
    private const string Sql = @"
        SELECT a.id, a.name, COALESCE(a.icon_path, '') AS icon_path, a.owner_id, (a.owner_id = su.id) AS has_ownership
        FROM apps a
        INNER JOIN users o ON o.id = a.owner_id
        INNER JOIN users su ON su.id = @userId
        WHERE a.deleted_at IS NULL
        AND o.lock_reason IS NULL
        AND (a.owner_id = su.id OR EXISTS (SELECT 1 FROM app_shares s WHERE s.app_id = a.id AND s.email = su.email))";

    private readonly IDbContext _db;

    public ApiAccess(IDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyList<ReadableApp>> ListReadableApps(string userId, CancellationToken cancellationToken)
    {
        using var cn = _db.Connection;
        var rows = await cn.QueryAsync<ReadableApp>(new CommandDefinition(
            $"{Sql} ORDER BY a.name", new { userId }, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<ReadableApp?> GetReadableApp(string userId, string appId, CancellationToken cancellationToken)
    {
        using var cn = _db.Connection;
        return await cn.QueryFirstOrDefaultAsync<ReadableApp>(new CommandDefinition(
            $"{Sql} AND a.id = @appId", new { userId, appId }, cancellationToken: cancellationToken));
    }
}

/// <summary>
/// The query layer remembers for a minute that a grant may read an app, and who owns it.
/// Whatever changes that answer, a removed share, a deleted app, a new owner, calls
/// <see cref="Invalidate"/>, so the instance that made the change stops serving the old
/// answer at once. Other instances catch up when their copy expires.
/// </summary>
public static class ApiAccessCache
{
    private static string GenerationKey(string appId) => $"api-access-gen:{appId}";

    public static string Key(IMemoryCache cache, string grantId, string appId)
    {
        var generation = cache.TryGetValue<string>(GenerationKey(appId), out var value) ? value : "0";
        return $"api-access:{appId}:{generation}:{grantId}";
    }

    public static void Invalidate(IMemoryCache cache, string appId)
        => cache.Set(GenerationKey(appId), Guid.NewGuid().ToString("N"), TimeSpan.FromMinutes(5));

    public static void Invalidate(HttpContext context, string appId)
        => Invalidate(context.RequestServices.GetRequiredService<IMemoryCache>(), appId);
}
