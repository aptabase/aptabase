using System.Data;
using Aptabase.Application.Authentication;
using Aptabase.Application.Query;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace Aptabase.Controllers;

public class BillingUsage
{
    public long Count { get; set; }
    public long Quota { get; set; }
}

[ApiController, IsAuthenticated]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class BillingController : Controller
{
    private readonly IQueryClient _queryClient;
    private readonly IDbConnection _db;

    public BillingController(IDbConnection db, IQueryClient queryClient)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _queryClient = queryClient ?? throw new ArgumentNullException(nameof(queryClient));
    }

    [HttpGet("/api/_billing")]
    public async Task<IActionResult> BillingState(CancellationToken cancellationToken)
    {
        var user = this.GetCurrentUser();
        var releaseAppIds = await _db.QueryAsync<string>(@"SELECT id FROM apps WHERE owner_id = @userId", new { userId = user.Id });
        var debugAppIds = releaseAppIds.Select(id => $"{id}_DEBUG");
        var appIds = releaseAppIds.Concat(debugAppIds);

        var (year, month) = (DateTime.UtcNow.Year, DateTime.UtcNow.Month);

        var rows = await _queryClient.QueryAsync<BillingUsage>(
            $@"SELECT countMerge(events) as Count
               FROM billing_usage_v1_mv
               WHERE app_id IN ('{string.Join("','", appIds)}')
               AND year = {year}
               AND month = {month}", cancellationToken);
        
        return Ok(new {
            Count = rows.FirstOrDefault()?.Count ?? 0,
            Quota = 20000
        });
    }
}
