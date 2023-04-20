namespace Aptabase.Migrations;


internal static class HttpContextExtensions
{
    public static string ResolveClientIpAddress(this HttpContext httpContext)
    {
        var cfViewerAddress = httpContext.Request.Headers["CloudFront-Viewer-Address"];
        if (cfViewerAddress.Count > 0)
        {
            var parts = (cfViewerAddress[0] ?? string.Empty).Split(":");
            if (parts.Length >= 1)
                return string.Join(",", parts[0..^1]);
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "";
    }
}