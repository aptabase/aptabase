using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aptabase.Features.Authentication;

public class IsAuthenticatedAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.IsAuthenticated())
        {
            context.Result = new UnauthorizedResult();
            return;              
        }

        base.OnActionExecuting(context);
    }
}