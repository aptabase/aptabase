namespace Microsoft.AspNetCore.Http;

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

    public static void EnsureSuccessWithLog(this HttpResponseMessage response, ILogger logger)
    {
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = response.Content.ReadAsStringAsync();
            logger.LogError("Tinybird returned {StatusCode} with body {ResponseBody}", response.StatusCode, responseBody);
            response.EnsureSuccessStatusCode();
        }
    }
}