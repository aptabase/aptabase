using Aptabase.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aptabase.Features.Stats;

public interface IAppScopedRequest
{
    string AppId { get; }
}

public class HasReadAccessToApp : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var db = context.HttpContext.RequestServices.GetService<IDbContext>() ?? throw new InvalidOperationException("Could not get database context.");
        var user = context.HttpContext.GetCurrentUserIdentity();

        if (!TryGetAppId(context.ActionArguments, out var appId))
        {
            context.Result = new BadRequestObjectResult("Invalid app id.");
            return;
        }

        var hasAccess = await db.HasReadAccessToApp(appId, user, context.HttpContext.RequestAborted);
        if (!hasAccess)
        {
            context.Result = new StatusCodeResult(403);
            return;
        }

        await next();
    }

    public static bool TryGetAppId(IDictionary<string, object?> actionArguments, out string appId)
    {
        foreach (var (name, value) in actionArguments)
        {
            var candidate = value switch
            {
                IAppScopedRequest scoped => scoped.AppId,
                string s when string.Equals(name, "appId", StringComparison.OrdinalIgnoreCase) => s,
                _ => null,
            };

            if (!string.IsNullOrEmpty(candidate))
            {
                appId = candidate;
                return true;
            }
        }

        appId = "";
        return false;
    }
}
