using System.Net;
using Aptabase.Features;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

public static class HttpContextExtensions
{
    public static string ResolveClientIpAddress(this HttpContext httpContext)
    {
        var env = httpContext.RequestServices?.GetService(typeof(EnvSettings)) as EnvSettings;
        return ClientIpResolver.Resolve(
            httpContext.Request.Headers,
            httpContext.Connection.RemoteIpAddress,
            env?.TrustedClientIpHeader,
            env?.TrustedProxyCount ?? 1);
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

public static class ClientIpResolver
{
    public static string Resolve(IHeaderDictionary headers, IPAddress? remoteAddress, string? trustedHeader, int trustedProxyCount)
    {
        if (!string.IsNullOrWhiteSpace(trustedHeader))
        {
            if (headers.TryGetValue(trustedHeader, out var trusted) && !StringValues.IsNullOrEmpty(trusted))
            {
                var entries = trusted.ToString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (entries.Length > 0)
                {
                    var index = Math.Max(0, entries.Length - Math.Max(1, trustedProxyCount));
                    return StripPort(entries[index]);
                }
            }

            return remoteAddress?.ToString() ?? "";
        }

        if (headers.TryGetValue("Cf-Connecting-Ip", out var cfIp) && !string.IsNullOrEmpty(cfIp))
            return cfIp.ToString();

        if (headers.TryGetValue("X-Real-Ip", out var ip) && !string.IsNullOrEmpty(ip))
            return ip.ToString();

        if (headers.TryGetValue("X-Forwarded-For", out var forwardedIp) && !string.IsNullOrEmpty(forwardedIp))
            return forwardedIp.ToString();

        var cfViewerAddress = headers["CloudFront-Viewer-Address"];
        if (cfViewerAddress.Count > 0)
            return StripPort(cfViewerAddress[0] ?? string.Empty);

        return remoteAddress?.ToString() ?? "";
    }

    private static string StripPort(string value)
    {
        if (value.StartsWith('['))
        {
            var close = value.IndexOf(']');
            return close > 0 ? value[..(close + 1)] : value;
        }

        var colons = value.Count(c => c == ':');
        return colons == 1 ? value[..value.IndexOf(':')] : value;
    }
}
