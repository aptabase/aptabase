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

        Console.WriteLine("No CloudFront-Viewer-Address header found, falling back to RemoteIpAddress {0}", httpContext.Connection.RemoteIpAddress?.ToString() ?? "}");
        Console.WriteLine("CloudFront-Viewer-Address={0}", cfViewerAddress.ToString());
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