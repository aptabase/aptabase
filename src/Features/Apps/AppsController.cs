using Dapper;
using Aptabase.Data;
using Aptabase.Features.Blob;
using Microsoft.AspNetCore.Mvc;
using Aptabase.Features.Authentication;
using System.ComponentModel.DataAnnotations;

namespace Aptabase.Features.Apps;

public class ApplicationShare
{
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class CreateAppRequestBody
{
    [Required]
    [StringLength(40, MinimumLength = 2)]
    public string Name { get; set; } = "";
}

public class UpdateAppRequestBody
{
    public string Icon { get; set; } = "";

    [Required]
    [StringLength(40, MinimumLength = 2)]
    public string Name { get; set; } = "";
}

[ApiController, IsAuthenticated]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AppsController : Controller
{
    private readonly IDbContext _db;
    private readonly EnvSettings _env;
    private readonly IBlobService _blobService;

    public AppsController(IDbContext db, EnvSettings env, IBlobService blobService)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
    }

    [HttpGet("/api/_apps")]
    public async Task<IActionResult> ListApps()
    {
        var user = this.GetCurrentUser();

        var apps = await _db.Connection.QueryAsync<Application>(
            @"SELECT a.id, a.name, a.icon_path, a.app_key, a.owner_id = @userId as has_ownership
              FROM apps a
              LEFT JOIN app_shares s
              ON s.app_id = a.id
              WHERE (a.owner_id = @userId OR s.email = @userEmail)
              AND a.deleted_at IS NULL
              GROUP BY a.id, a.name, a.icon_path, a.app_key
              ORDER by a.name", new { userId = user.Id, userEmail = user.Email });
              
        return Ok(apps);
    }

    [HttpPost("/api/_apps")]
    public async Task<IActionResult> Create([FromBody] CreateAppRequestBody body)
    {
        var user = this.GetCurrentUser();
        var app = new Application
        {
            Id = NanoId.New(),
            Name = body.Name,
            AppKey = $"A-{_env.Region}-{NanoId.Numbers(10)}"
        };

        await _db.Connection.ExecuteScalarAsync<string>("INSERT INTO apps (id, owner_id, name, app_key) VALUES (@appId, @ownerId, @name, @appKey)", new
        {
            appId = app.Id,
            ownerId = user.Id,
            name = app.Name,
            appKey = app.AppKey
        });

        return Ok(app);
    }

    [HttpPut("/api/_apps/{appId}")]
    public async Task<IActionResult> Update(string appId, [FromBody] UpdateAppRequestBody body, CancellationToken cancellationToken)
    {
        var app = await GetOwnedApp(appId);
        if (app == null)
            return NotFound();

        if (!string.IsNullOrEmpty(body.Icon))
        {            
		    var content = Convert.FromBase64String(body.Icon);
            app.IconPath = await _blobService.UploadAsync("icons", content, "image/png", cancellationToken);
        }

        app.Name = body.Name;
        await _db.Connection.ExecuteScalarAsync<string>("UPDATE apps SET name = @name, icon_path = @iconPath WHERE id = @appId", new
        {
            appId = app.Id,
            name = app.Name,
            iconPath = app.IconPath,
        });

        return Ok(app);
    }

    [HttpDelete("/api/_apps/{appId}")]
    public async Task<IActionResult> Delete(string appId)
    {
        var app = await GetOwnedApp(appId);
        if (app == null)
            return NotFound();

        await _db.Connection.ExecuteScalarAsync<string>("UPDATE apps SET deleted_at = now() WHERE id = @appId", new
        {
            appId = app.Id,
        });

        return Ok(new { });
    }

    [HttpGet("/api/_apps/{appId}/shares")]
    public async Task<IActionResult> ListAppShares(string appId)
    {
        var app = await GetOwnedApp(appId);
        if (app == null)
            return NotFound();
            
        var shares = await _db.Connection.QueryAsync<ApplicationShare>(
                            @"SELECT email, created_at
                            FROM app_shares
                            WHERE app_id = @appId", new { appId });
        return Ok(shares);
    }

    [HttpPut("/api/_apps/{appId}/shares/{email}")]
    public async Task<IActionResult> ShareApp(string appId, string email)
    {
        var app = await GetOwnedApp(appId);
        if (app == null)
            return NotFound();

        await _db.Connection.ExecuteScalarAsync<string>(@"
            INSERT INTO app_shares (app_id, email)
            VALUES (@appId, @email)
            ON CONFLICT DO NOTHING", new
        {
            appId,
            email,
        });

        return Ok(new { });
    }

    [HttpDelete("/api/_apps/{appId}/shares/{email}")]
    public async Task<IActionResult> UnshareApp(string appId, string email)
    {
        var app = await GetOwnedApp(appId);
        if (app == null)
            return NotFound();

        await _db.Connection.ExecuteScalarAsync<string>(@"DELETE FROM app_shares WHERE app_id = @appId AND email = @email", new
        {
            appId,
            email,
        });

        return Ok(new { });
    }

    private async Task<Application?> GetOwnedApp(string appId)
    {
        var user = this.GetCurrentUser();
        return await _db.Connection.QueryFirstOrDefaultAsync<Application>(
                @"SELECT id, name, icon_path, app_key, true as has_ownership
                FROM apps
                WHERE id = @appId
                AND owner_id = @userId
                AND deleted_at IS NULL", new { appId, userId = user.Id });
    }
}
