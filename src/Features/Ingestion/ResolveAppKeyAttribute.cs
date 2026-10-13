using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aptabase.Features.Ingestion;

// Resolves the App-Key header and rejects missing, unknown or locked app keys. This is a resource filter so it
// runs before model binding: rejected requests never have their body parsed or validated, which keeps a fleet
// of devices still sending to a locked or deleted app down to the cost of an in-memory cache lookup.
//
// Rejections are counted in IRejectionStats rather than logged per request: anyone can send any App-Key, so a
// per-request log line would let a single client drive unbounded log volume.
[AttributeUsage(AttributeTargets.Method)]
public class ResolveAppKeyAttribute : Attribute, IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        var appKey = httpContext.Request.Headers["App-Key"].ToString().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(appKey))
        {
            RecordRejection(httpContext, "missing_key", appKey);
            context.Result = MissingAppKey();
            return;
        }

        var cache = httpContext.RequestServices.GetRequiredService<IIngestionCache>();
        var app = await cache.FindByAppKey(appKey, httpContext.RequestAborted);
        if (string.IsNullOrEmpty(app.Id))
        {
            RecordRejection(httpContext, "unknown_key", appKey);
            context.Result = AppNotFound(appKey);
            return;
        }

        if (app.IsLocked)
        {
            RecordRejection(httpContext, "locked", appKey);
            context.Result = AppLocked();
            return;
        }

        SetApp(httpContext, app);
        await next();
    }

    // Event ingestion drops rejected events but answers with 200, the same status as success. Deployed SDKs that retry
    // every non-2xx response (e.g. Aptabase.Unity up to 0.3.1) keep failed events queued and re-send the whole queue on
    // every flush, so a 4xx here makes their traffic grow for as long as the key stays missing, unknown or locked.
    protected virtual IActionResult MissingAppKey() => Dropped("Missing App-Key header.");

    protected virtual IActionResult AppNotFound(string appKey) => Dropped($"Application not found with given app key: {appKey}.");

    protected virtual IActionResult AppLocked() => Dropped("Owner account is locked.");

    private static IActionResult Dropped(string reason) => new OkObjectResult(new { error = $"{reason} Events are not being recorded." });

    public static CachedApplication GetApp(HttpContext httpContext)
    {
        return httpContext.Items[typeof(CachedApplication)] as CachedApplication
            ?? throw new InvalidOperationException($"{nameof(ResolveAppKeyAttribute)} must run before the app is read.");
    }

    public static void SetApp(HttpContext httpContext, CachedApplication app)
    {
        httpContext.Items[typeof(CachedApplication)] = app;
    }

    private static void RecordRejection(HttpContext httpContext, string reason, string appKey)
    {
        var rejections = httpContext.RequestServices.GetRequiredService<IRejectionStats>();
        rejections.Record(reason, appKey, httpContext.ResolveClientIpAddress(), httpContext.Request.Headers.UserAgent);
    }
}
