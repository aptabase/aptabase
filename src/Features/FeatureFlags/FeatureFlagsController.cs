using Aptabase.Data;
using Aptabase.Features.Apps;
using Aptabase.Features.Authentication;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Aptabase.Features.FeatureFlags;

public class CreateFeatureFlagRequestBody : UpdateFeatureFlagRequestBody
{
    public string AppId { get; set; } = "";
}

public class UpdateFeatureFlagRequestBody
{
    [Required]
    [StringLength(255, MinimumLength = 2)]
    public string Key { get; set; } = "";

    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string Value { get; set; } = "";

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Environment { get; set; } = "";
}

[ApiController, IsAuthenticated]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class FeatureFlagsController : Controller
{
    private readonly IDbContext _db;
    private readonly IAppQueries _appQueries;

    public FeatureFlagsController(IDbContext db, IAppQueries appQueries)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _appQueries = appQueries ?? throw new ArgumentNullException(nameof(appQueries));
    }

    [HttpGet("/api/_flags/{appId}")]
    public async Task<IActionResult> List(string appId)
    {
        var user = this.GetCurrentUserIdentity();

        var app = await _appQueries.GetOwnedAppAsync(appId, user.Id);

        if (app == null)
        {
            return NotFound();
        }

        var flags = await _db.Connection.QueryAsync<FeatureFlag>(@"
            SELECT f.id, f.app_id, f.key, f.value, f.environment, f.conditions
            FROM feature_flags f
            WHERE f.app_id = @appId
            LIMIT 50",
            new
            {
                appId,
            });

        return Ok(flags);
    }

    [HttpPost("/api/_flags")]
    public async Task<IActionResult> Create([FromBody] CreateFeatureFlagRequestBody body)
    {
        var user = this.GetCurrentUserIdentity();

        var app = await _appQueries.GetOwnedAppAsync(body.AppId, user.Id);

        if (app == null)
        {
            return NotFound();
        }

        var flag = new FeatureFlag
        {
            Id = NanoId.New(),
            AppId = body.AppId,
            Key = body.Key,
            Value = body.Value,
            Environment = body.Environment,
        };

        await _db.Connection.ExecuteAsync(@"
            INSERT INTO feature_flags (id, app_id, key, value, environment, created_at, modified_at)
            VALUES (@id, @appId, @key, @value, @environment, now(), now())",
            new
            {
                id = flag.Id,
                appId = flag.AppId,
                key = flag.Key,
                value = flag.Value,
                environment = flag.Environment,
            });

        return Ok(app);
    }

    [HttpPut("/api/_flags/{flagId}")]
    public async Task<IActionResult> Update(string flagId, [FromBody] UpdateFeatureFlagRequestBody body)
    {
        var user = this.GetCurrentUserIdentity();

        var flag = await GetOwnedFeatureFlagAsync(flagId, user.Id);

        if (flag == null)
        {
            return NotFound();
        }

        flag.Key = body.Key;
        flag.Value = body.Value;
        flag.Environment = body.Environment;

        await _db.Connection.ExecuteAsync(
            "UPDATE feature_flags SET Key = @key, Value = @value, Environment = @environment, WHERE id = @flagId",
            new
            {
                flagId,
                key = flag.Key,
                value = flag.Value,
                environment = flag.Environment,
            });

        return Ok(flag);
    }

    [HttpDelete("/api/_flags/{flagId}")]
    public async Task<IActionResult> Delete(string flagId)
    {
        var user = this.GetCurrentUserIdentity();

        var flag = await GetOwnedFeatureFlagAsync(flagId, user.Id);

        if (flag == null)
        {
            return NotFound();
        }

        await _db.Connection.ExecuteAsync(
            "DELETE FROM feature_flags WHERE id = @flagId",
            new
            {
                flagId,
            });

        return NoContent();
    }
    
    private Task<FeatureFlag?> GetOwnedFeatureFlagAsync(string flagId, string userId)
    {
        return _db.Connection.QueryFirstOrDefaultAsync<FeatureFlag>(@"
            SELECT f.id, f.app_id, f.key, f.value, f.environment, f.conditions
            FROM feature_flags f
            JOIN apps a
            ON f.app_id = a.id
            INNER JOIN users u
            ON u.id = a.owner_id
            WHERE f.id = @flagId
            AND a.owner_id = @userId
            AND a.deleted_at IS NULL",
            new
            {
                flagId,
                userId,
            });
    }
}
