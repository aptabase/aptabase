
using Aptabase.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aptabase.Features.Stats;

public class HasReadAccessToApp : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var db = context.HttpContext.RequestServices.GetService<IDbContext>() ?? throw new InvalidOperationException("Could not get database context.");
        var user = context.HttpContext.GetCurrentUserIdentity();
        var appId = context.HttpContext.Request.Query["AppId"].ToString();

        var hasAccess = await db.HasReadAccessToApp(appId, user, context.HttpContext.RequestAborted);
        if (!hasAccess)
        {
            context.Result = new StatusCodeResult(403);
            return;
        }

        await next();
    }
}