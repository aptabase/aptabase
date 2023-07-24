namespace Microsoft.AspNetCore.Http;

internal static class HttpContextExtensions
{
    public static string ResolveClientIpAddress(this HttpContext httpContext)
    {
        var cfViewerAddress = httpContext.Request.Headers["CloudFront-Viewer-Address"];
        if (cfViewerAddress.Count > 0)
        {
            var parts = (cfViewerAddress[0] ?? string.Empty).Split(":");
            if (parts.Length == 1)
                return parts[0];
                
            if (parts.Length >= 1)
                return string.Join(":", parts[0..^1]);
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "";
    }

    public static async Task EnsureSuccessWithLog(this HttpResponseMessage response, ILogger logger)
    {
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Request failed with {StatusCode} with body {ResponseBody}", response.StatusCode, responseBody);
            response.EnsureSuccessStatusCode();
        }
    }
}