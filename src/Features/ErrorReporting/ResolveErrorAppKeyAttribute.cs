using Aptabase.Features.Ingestion;
using Microsoft.AspNetCore.Mvc;

namespace Aptabase.Features.ErrorReporting;

// Same App-Key resolution as event ingestion, keeping the responses /api/v0/error has always returned.
// Deployed SDKs drop any 4xx other than 429, so these must not change to a retryable status.
public class ResolveErrorAppKeyAttribute : ResolveAppKeyAttribute
{
    protected override IActionResult MissingAppKey() => new BadRequestObjectResult(new { error = "Missing App-Key header" });

    protected override IActionResult AppNotFound(string appKey) => new UnauthorizedObjectResult(new { error = "Invalid App-Key" });

    protected override IActionResult AppLocked() => new BadRequestObjectResult(new { error = "Owner account is locked" });
}
