using Aptabase.Features.Authentication;

namespace Microsoft.AspNetCore.Mvc;

public static class AuthExtensions
{
    public static bool IsAuthenticated(this HttpContext context)
    {
        return context.User.Identity?.IsAuthenticated ?? false;
    }

    public static UserIdentity GetCurrentUserIdentity(this Controller controller)
    {
        return controller.HttpContext.GetCurrentUserIdentity();
    }

    public static UserIdentity GetCurrentUserIdentity(this HttpContext context)
    {
        if (!context.IsAuthenticated())
            throw new InvalidOperationException("User is not authenticated.");

        var id = context.User.FindFirst(x => x.Type == "id")?.Value ?? "";
        var email = context.User.FindFirst(x => x.Type == "email")?.Value ?? "";
        var name = context.User.FindFirst(x => x.Type == "name")?.Value ?? "";
        return new UserIdentity(id, name, email);
    }
}