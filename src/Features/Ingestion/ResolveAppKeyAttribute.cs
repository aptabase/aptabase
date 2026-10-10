using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aptabase.Features.Ingestion;

// Resolves the App-Key header and rejects unknown or locked apps. This is a resource filter so it runs
// before model binding: rejected requests never have their body parsed or validated, which keeps a fleet
// of devices still sending to a locked or deleted app down to the cost of an in-memory cache lookup.
[AttributeUsage(AttributeTargets.Method)]
public class ResolveAppKeyAttribute : Attribute, IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var cache = httpContext.RequestServices.GetRequiredService<IIngestionCache>();

        var appKey = httpContext.Request.Headers["App-Key"].ToString().ToUpper();
        var app = await cache.FindByAppKey(appKey, httpContext.RequestAborted);
        if (string.IsNullOrEmpty(app.Id))
        {
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<ResolveAppKeyAttribute>>();
            logger.LogWarning("Appplication not found with given app key: {AppKey}", appKey);
            context.Result = new NotFoundObjectResult($"Appplication not found with given app key: {appKey}");
            return;
        }

        if (app.IsLocked)
        {
            var rejections = httpContext.RequestServices.GetRequiredService<IRejectionStats>();
            rejections.Record("locked", appKey, httpContext.ResolveClientIpAddress(), httpContext.Request.Headers.UserAgent);
            context.Result = new BadRequestObjectResult("Owner account is locked.");
            return;
        }

        httpContext.Items[typeof(CachedApplication)] = app;
        await next();
    }

    public static CachedApplication GetApp(HttpContext httpContext)
    {
        return httpContext.Items[typeof(CachedApplication)] as CachedApplication
            ?? throw new InvalidOperationException($"{nameof(ResolveAppKeyAttribute)} must run before the app is read.");
    }
}
