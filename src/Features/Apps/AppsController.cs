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

public class OwnershipTransferRequest
{
    public string NewOwnerEmail { get; set; } = "";
    public DateTime RequestedAt { get; set; }
    public string Status { get; set; } = "";
}

public class IncomingTransferRequest
{
    public string AppId { get; set; } = "";
    public string AppName { get; set; } = "";
    public string CurrentOwnerEmail { get; set; } = "";
    public DateTime RequestedAt { get; set; }
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

public class InitiateOwnershipTransferRequestBody
{
    [Required]
    [EmailAddress]
    public string NewOwnerEmail { get; set; } = "";
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

    // ownership transfer endpoints
    [HttpGet("/api/_apps/{appId}/ownership-transfer")]
    public async Task<IActionResult> GetOwnershipTransferStatus(string appId)
    {
        var app = await GetOwnedApp(appId);
        if (app == null)
            return NotFound();

        var transfer = await _db.Connection.QueryFirstOrDefaultAsync<OwnershipTransferRequest?>(
            @"SELECT new_owner_email, requested_at, status
              FROM app_ownership_transfers
              WHERE app_id = @appId AND status = 'pending'",
            new { appId });

        return Ok(transfer);
    }

    [HttpPost("/api/_apps/{appId}/ownership-transfer")]
    public async Task<IActionResult> InitiateOwnershipTransfer(string appId, [FromBody] InitiateOwnershipTransferRequestBody body)
    {
        var app = await GetOwnedApp(appId);
        if (app == null)
            return NotFound();

        var user = this.GetCurrentUserIdentity();

        // check if user is trying to transfer to themselves
        if (body.NewOwnerEmail.ToLower() == user.Email.ToLower())
            return BadRequest(new { errors = new { newOwnerEmail = "Cannot transfer ownership to yourself" } });

        // check if there's already a pending transfer
        var existingTransfer = await _db.Connection.QueryFirstOrDefaultAsync<int>(
            @"SELECT COUNT(*) FROM app_ownership_transfers 
              WHERE app_id = @appId AND status = 'pending'",
            new { appId });

        if (existingTransfer > 0)
            return BadRequest(new { errors = new { newOwnerEmail = "There is already a pending ownership transfer for this app" } });

        // create the ownership transfer request
        await _db.Connection.ExecuteAsync(@"
            INSERT INTO app_ownership_transfers (app_id, current_owner_id, new_owner_email, requested_at, status)
            VALUES (@appId, @currentOwnerId, @newOwnerEmail, @requestedAt, 'pending')",
            new
            {
                appId,
                currentOwnerId = user.Id,
                newOwnerEmail = body.NewOwnerEmail.ToLower(),
                requestedAt = DateTime.UtcNow
            });

        return Ok(new { });
    }

    [HttpDelete("/api/_apps/{appId}/ownership-transfer")]
    public async Task<IActionResult> CancelOwnershipTransfer(string appId)
    {
        var app = await GetOwnedApp(appId);
        if (app == null)
            return NotFound();

        await _db.Connection.ExecuteAsync(@"
            DELETE FROM app_ownership_transfers 
            WHERE app_id = @appId AND status = 'pending'",
            new { appId });

        return Ok(new { });
    }

    [HttpGet("/api/_ownership-transfer-requests")]
    public async Task<IActionResult> GetIncomingOwnershipTransferRequests()
    {
        var user = this.GetCurrentUserIdentity();

        var requests = await _db.Connection.QueryAsync<IncomingTransferRequest>(
            @"SELECT t.app_id, a.name as app_name, u.email as current_owner_email, t.requested_at
              FROM app_ownership_transfers t
              INNER JOIN apps a ON a.id = t.app_id
              INNER JOIN users u ON u.id = t.current_owner_id
              WHERE t.new_owner_email = @userEmail 
              AND t.status = 'pending'
              AND a.deleted_at IS NULL
              ORDER BY t.requested_at DESC",
            new { userEmail = user.Email.ToLower() });

        return Ok(requests);
    }

    [HttpPost("/api/_apps/{appId}/ownership-transfer/accept")]
    public async Task<IActionResult> AcceptOwnershipTransfer(string appId)
    {
        var user = this.GetCurrentUserIdentity();

        // get the pending transfer request
        var transfer = await _db.Connection.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT t.current_owner_id, u.email as current_owner_email, a.name as app_name
              FROM app_ownership_transfers t
              INNER JOIN apps a ON a.id = t.app_id
              INNER JOIN users u ON u.id = t.current_owner_id
              WHERE t.app_id = @appId 
              AND t.new_owner_email = @userEmail 
              AND t.status = 'pending'
              AND a.deleted_at IS NULL",
            new { appId, userEmail = user.Email.ToLower() });

        if (transfer == null)
            return NotFound("No pending ownership transfer found");

        using (var cn = _db.Connection) {
            cn.Open();

            using (var transaction = cn.BeginTransaction())
            {
                try
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
                            email = transfer.current_owner_email,
                            createdAt = DateTime.UtcNow
                        }, transaction);

                    // mark transfer as accepted
                    await cn.ExecuteAsync(@"
                        UPDATE app_ownership_transfers 
                        SET status = 'accepted', completed_at = @completedAt
                        WHERE app_id = @appId AND status = 'pending'",
                        new { appId, completedAt = DateTime.UtcNow }, transaction);

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

    [HttpPost("/api/_apps/{appId}/ownership-transfer/reject")]
    public async Task<IActionResult> RejectOwnershipTransfer(string appId)
    {
        var user = this.GetCurrentUserIdentity();

        // check if there's a pending transfer for this user
        var existingTransfer = await _db.Connection.QueryFirstOrDefaultAsync<int>(
            @"SELECT COUNT(*) FROM app_ownership_transfers t
              INNER JOIN apps a ON a.id = t.app_id
              WHERE t.app_id = @appId 
              AND t.new_owner_email = @userEmail 
              AND t.status = 'pending'
              AND a.deleted_at IS NULL",
            new { appId, userEmail = user.Email.ToLower() });

        if (existingTransfer == 0)
            return NotFound("No pending ownership transfer found");

        // mark transfer as rejected
        await _db.Connection.ExecuteAsync(@"
            UPDATE app_ownership_transfers 
            SET status = 'rejected', completed_at = @completedAt
            WHERE app_id = @appId 
            AND new_owner_email = @userEmail
            AND status = 'pending'",
            new { appId, userEmail = user.Email.ToLower(), completedAt = DateTime.UtcNow });

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
