using Aptabase.Features.Authentication;

namespace Microsoft.AspNetCore.Mvc;

public static class AuthExtensions
{
    public static bool IsAuthenticated(this HttpContext context)
    {
        return context.User.Identity?.IsAuthenticated ?? false;
    }

    public static UserAccount GetCurrentUser(this Controller controller)
    {
        if (!controller.HttpContext.IsAuthenticated())
            throw new InvalidOperationException("User is not authenticated.");

        var context = controller.HttpContext;
        var id = context.User.FindFirst(x => x.Type == "id")?.Value ?? "";
        var email = context.User.FindFirst(x => x.Type == "email")?.Value ?? "";
        var name = context.User.FindFirst(x => x.Type == "name")?.Value ?? "";
        return new UserAccount(id, name, email);
    }
}