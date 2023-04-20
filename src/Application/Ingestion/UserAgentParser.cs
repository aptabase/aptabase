using System.Text.RegularExpressions;

namespace Aptabase.Application.Ingestion;

public static class UserAgentParser
{
    private static Dictionary<string, string> osKeys = new Dictionary<string, string>
    {

        { "ipad", "iPadOS" },
        { "iphone", "iOS" },
        { "ios", "iOS" },

        { "windows nt", "Windows" },

        { "mac os x", "macOS" },
        { "macos", "macOS" },
        { "macintosh", "macOS" },

        { "android", "Android" },

        { "cros", "Chrome OS" },
        { "ubuntu", "Ubuntu" },
        { "fedora", "Fedora" },
        { "arch linux", "Arch Linux" },
        { "archlinux", "Arch Linux" },
        { "linux", "Linux" }
    };

    public static (string, string) ParseOperatingSystem(string userAgent)
    {
        var lcUserAgent = userAgent.ToLowerInvariant();

        foreach (var (key, value) in osKeys)
        {
            if (lcUserAgent.Contains(key))
                return (value, "");
        }

        return ("", "");
    }


    private static Dictionary<string, string> browserKeys = new Dictionary<string, string>
    {
        { "Edg", "Edge" },
        { "Firefox", "Firefox" },
        { "OPiOS", "Opera" },
        { "OPR", "Opera" },
        { "YaBrowser", "Yandex Browser" }
    };

    private static Regex browserRegex = new Regex(@"\((?<info>.*?)\)(\s|$)|(?<name>.*?)\/(?<version>.*?)(\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static (string, string) ParseBrowser(string userAgent)
    {
        var lcUserAgent = userAgent.ToLowerInvariant();
        var matches = browserRegex.Matches(userAgent);

        var chromeVersion = "";
        foreach (Match match in matches)
        {
            var name = match.Groups["name"].Value;
            var version = match.Groups["version"].Value;

            foreach (var (key, prettyName) in browserKeys)
            {
                if (name == key)
                    return (prettyName, version);
            }

            if (name == "Chrome")
                chromeVersion = version;

            if (name == "Safari")
            {
                // It's not Safari unless it has a "Version" group
                var safariVersion = matches.FirstOrDefault(x => x.Groups["name"].Value == "Version")?.Groups["version"].Value ?? "";
                if (!string.IsNullOrEmpty(safariVersion))
                    return ("Safari", safariVersion);
            }
        }

        // if we didn't find a browser, but we found a chrome version, we assume it's chrome
        if (!string.IsNullOrEmpty(chromeVersion))
            return ("Chrome", chromeVersion);

        return ("", "");
    }
}