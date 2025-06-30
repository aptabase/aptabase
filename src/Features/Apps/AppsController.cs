using Dapper;
using Aptabase.Data;
using Aptabase.Features.Blob;
using Microsoft.AspNetCore.Mvc;
using Aptabase.Features.Authentication;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Aptabase.Features.Apps;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppRequestPurpose
{
    AppOwnership,
    AppShare
}

public class ApplicationShare
{
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class AppRequest
{
    public string TargetUserEmail { get; set; } = "";
    public DateTime RequestedAt { get; set; }
    public string Status { get; set; } = "";
    public AppRequestPurpose Purpose { get; set; }
}

public class IncomingAppRequest
{
    public string AppId { get; set; } = "";
    public string AppName { get; set; } = "";
    public string RequesterEmail { get; set; } = "";
    public DateTime RequestedAt { get; set; }
    public AppRequestPurpose Purpose { get; set; }
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

public class InitiateAppRequestBody
{
    [Required]
    [EmailAddress]
    public string TargetUserEmail { get; set; } = "";
    
    [Required]
    public AppRequestPurpose Purpose { get; set; }
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
        var user = this.GetCurrentUserIdentity();

        var apps = await _db.Connection.QueryAsync<Application>(
            @"SELECT a.id, a.name, a.icon_path, a.app_key, 
                     a.owner_id = @userId AS has_ownership, a.has_events, 
                     u.lock_reason
              FROM apps a
              LEFT JOIN app_shares s
              ON s.app_id = a.id
              INNER JOIN users u
              ON u.id = a.owner_id
              WHERE (a.owner_id = @userId OR s.email = @userEmail)
              AND a.deleted_at IS NULL
              GROUP BY a.id, a.name, a.icon_path, a.app_key, u.lock_reason
              ORDER by a.name", new { userId = user.Id, userEmail = user.Email });
              
        return Ok(apps);
    }

    [HttpPost("/api/_apps")]
    public async Task<IActionResult> Create([FromBody] CreateAppRequestBody body)
    {
        var user = this.GetCurrentUserIdentity();
        var app = new Application
        {
            Id = NanoId.New(),
            Name = body.Name,
            AppKey = $"A-{_env.Region}-{NanoId.Numbers(10)}"
        };

        await _db.Connection.ExecuteScalarAsync<string>(@"
            INSERT INTO apps (id, owner_id, name, app_key, has_events)
            VALUES (@appId, @ownerId, @name, @appKey, false)",
        new
        {
            appId = app.Id,
            ownerId = user.Id,
            name = app.Name,
            appKey = app.AppKey
        });

        return Ok(app);
    }

    [HttpGet("/api/_apps/{appId}")]
    public async Task<IActionResult> GetAppById(string appId)
    {
        var user = this.GetCurrentUserIdentity();

        var app = await _db.Connection.QueryFirstOrDefaultAsync<Application>(
            @"SELECT a.id, a.name, a.icon_path, a.app_key, 
                     a.owner_id = @userId as has_ownership, a.has_events,
                     u.lock_reason
              FROM apps a
              LEFT JOIN app_shares s
              ON s.app_id = a.id
              INNER JOIN users u
              ON u.id = a.owner_id
              WHERE a.id = @appId
              AND (a.owner_id = @userId OR s.email = @userEmail)
              AND a.deleted_at IS NULL
              GROUP BY a.id, a.name, a.icon_path, a.app_key, u.lock_reason
              ORDER by a.name",
            new { appId, userId = user.Id, userEmail = user.Email }
        );

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
            email = email.ToLower(),
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

    // app request endpoints
    [HttpGet("/api/_apps/{appId}/requests")]
    public async Task<IActionResult> GetAppRequestStatus(string appId, [FromQuery] AppRequestPurpose purpose)
    {
        var app = await GetOwnedApp(appId);
        if (app == null)
            return NotFound();

        var request = await _db.Connection.QueryFirstOrDefaultAsync<AppRequest?>(
            @"SELECT target_user_email, requested_at, status, purpose
              FROM app_requests
              WHERE app_id = @appId AND purpose = @purpose AND status = 'pending'",
            new { appId, purpose = purpose.ToString() });

        return Ok(request);
    }

    [HttpPost("/api/_apps/{appId}/requests")]
    public async Task<IActionResult> InitiateAppRequest(string appId, [FromBody] InitiateAppRequestBody body)
    {
        var app = await GetOwnedApp(appId);
        if (app == null)
            return NotFound();

        var user = this.GetCurrentUserIdentity();

        // check if user is trying to request to themselves
        if (body.TargetUserEmail.ToLower() == user.Email.ToLower())
            return BadRequest(new { errors = new { targetUserEmail = "Cannot create request to yourself" } });

        // check if there's already a pending request
        var existingRequest = await _db.Connection.QueryFirstOrDefaultAsync<int>(
            @"SELECT COUNT(*) FROM app_requests 
              WHERE app_id = @appId AND purpose = @purpose AND status = 'pending'",
            new { appId, purpose = body.Purpose.ToString() });

        if (existingRequest > 0)
            return BadRequest(new { errors = new { targetUserEmail = "There is already a pending request for this app" } });

        // create the app request
        await _db.Connection.ExecuteAsync(@"
            INSERT INTO app_requests (app_id, current_owner_id, target_user_email, purpose, requested_at, status)
            VALUES (@appId, @requesterUserId, @targetUserEmail, @purpose, @requestedAt, 'pending')",
            new
            {
                appId,
                requesterUserId = user.Id,
                targetUserEmail = body.TargetUserEmail.ToLower(),
                purpose = body.Purpose.ToString(),
                requestedAt = DateTime.UtcNow
            });

        return Ok(new { });
    }

    [HttpDelete("/api/_apps/{appId}/requests")]
    public async Task<IActionResult> CancelAppRequest(string appId, [FromQuery] AppRequestPurpose purpose)
    {
        var app = await GetOwnedApp(appId);
        if (app == null)
            return NotFound();

        await _db.Connection.ExecuteAsync(@"
            DELETE FROM app_requests 
            WHERE app_id = @appId AND purpose = @purpose AND status = 'pending'",
            new { appId, purpose = purpose.ToString() });

        return Ok(new { });
    }

    [HttpGet("/api/_app-requests")]
    public async Task<IActionResult> GetIncomingAppRequests([FromQuery] AppRequestPurpose? purpose = null)
    {
        var user = this.GetCurrentUserIdentity();

        var purposeFilter = purpose.HasValue ? "AND r.purpose = @purpose" : "";
        var requests = await _db.Connection.QueryAsync<IncomingAppRequest>(
            $@"SELECT r.app_id, a.name as app_name, u.email as requester_email, r.requested_at, r.purpose
              FROM app_requests r
              INNER JOIN apps a ON a.id = r.app_id
              INNER JOIN users u ON u.id = r.current_owner_id
              WHERE r.target_user_email = @userEmail 
              AND r.status = 'pending'
              AND a.deleted_at IS NULL
              {purposeFilter}
              ORDER BY r.requested_at DESC",
            new { userEmail = user.Email.ToLower(), purpose = purpose?.ToString() });

        return Ok(requests);
    }

    [HttpPost("/api/_apps/{appId}/requests/accept")]
    public async Task<IActionResult> AcceptAppRequest(string appId, [FromQuery] AppRequestPurpose purpose)
    {
        var user = this.GetCurrentUserIdentity();

        // get the pending request
        var request = await _db.Connection.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT r.current_owner_id, u.email as requester_email, a.name as app_name, r.purpose
              FROM app_requests r
              INNER JOIN apps a ON a.id = r.app_id
              INNER JOIN users u ON u.id = r.current_owner_id
              WHERE r.app_id = @appId 
              AND r.target_user_email = @userEmail 
              AND r.purpose = @purpose
              AND r.status = 'pending'
              AND a.deleted_at IS NULL",
            new { appId, userEmail = user.Email.ToLower(), purpose = purpose.ToString() });

        if (request == null)
            return NotFound("No pending app request found");

        using (var cn = _db.Connection) {
            cn.Open();

            using (var transaction = cn.BeginTransaction())
            {
                try
                {
                    if (purpose == AppRequestPurpose.AppOwnership)
                    {
                        // transfer ownership
                        await cn.ExecuteAsync(@"
                            UPDATE apps SET owner_id = @newOwnerId WHERE id = @appId",
                            new { appId, newOwnerId = user.Id }, transaction);

                        // add previous owner to shares
                        await cn.ExecuteAsync(@"
                            INSERT INTO app_shares (app_id, email, created_at)
                            VALUES (@appId, @email, @createdAt)
                            ON CONFLICT DO NOTHING",
                            new 
                            { 
                                appId, 
                                email = request.requester_email,
                                createdAt = DateTime.UtcNow
                            }, transaction);
                    }
                    else if (purpose == AppRequestPurpose.AppShare)
                    {
                        // add user to shares
                        await cn.ExecuteAsync(@"
                            INSERT INTO app_shares (app_id, email, created_at)
                            VALUES (@appId, @email, @createdAt)
                            ON CONFLICT DO NOTHING",
                            new 
                            { 
                                appId, 
                                email = user.Email.ToLower(),
                                createdAt = DateTime.UtcNow
                            }, transaction);
                    }

                    // mark request as accepted
                    await cn.ExecuteAsync(@"
                        UPDATE app_requests 
                        SET status = 'accepted', completed_at = @completedAt
                        WHERE app_id = @appId AND purpose = @purpose AND status = 'pending'",
                        new { appId, purpose = purpose.ToString(), completedAt = DateTime.UtcNow }, transaction);

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
        
        return Ok(new { });
    }

    [HttpPost("/api/_apps/{appId}/requests/reject")]
    public async Task<IActionResult> RejectAppRequest(string appId, [FromQuery] AppRequestPurpose purpose)
    {
        var user = this.GetCurrentUserIdentity();

        // check if there's a pending request for this user
        var existingRequest = await _db.Connection.QueryFirstOrDefaultAsync<int>(
            @"SELECT COUNT(*) FROM app_requests r
              INNER JOIN apps a ON a.id = r.app_id
              WHERE r.app_id = @appId 
              AND r.target_user_email = @userEmail 
              AND r.purpose = @purpose
              AND r.status = 'pending'
              AND a.deleted_at IS NULL",
            new { appId, userEmail = user.Email.ToLower(), purpose = purpose.ToString() });

        if (existingRequest == 0)
            return NotFound("No pending app request found");

        // mark request as rejected
        await _db.Connection.ExecuteAsync(@"
            UPDATE app_requests 
            SET status = 'rejected', completed_at = @completedAt
            WHERE app_id = @appId 
            AND target_user_email = @userEmail
            AND purpose = @purpose
            AND status = 'pending'",
            new { appId, userEmail = user.Email.ToLower(), purpose = purpose.ToString(), completedAt = DateTime.UtcNow });

        return Ok(new { });
    }

    private async Task<Application?> GetOwnedApp(string appId)
    {
        var user = this.GetCurrentUserIdentity();
        return await _db.Connection.QueryFirstOrDefaultAsync<Application>(
            @"SELECT a.id, a.name, a.icon_path, a.app_key, true as has_ownership, 
                     a.has_events, u.lock_reason
              FROM apps a
              INNER JOIN users u
              ON u.id = a.owner_id
              WHERE a.id = @appId
              AND a.owner_id = @userId
              AND a.deleted_at IS NULL",
            new { appId, userId = user.Id }
        );
    }
}
