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

    protected virtual IActionResult MissingAppKey() => AppNotFound("");

    protected virtual IActionResult AppNotFound(string appKey) => new NotFoundObjectResult($"Appplication not found with given app key: {appKey}");

    protected virtual IActionResult AppLocked() => new BadRequestObjectResult("Owner account is locked.");

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
