using System.Text.RegularExpressions;

namespace Aptabase.IntegrationTests;

public class MailCatcherMessage
{
    public long Id { get; set; }
    public string[] Recipients { get; set; } = Array.Empty<string>();
}

public static class MailCatcher
{
    public static async Task<string> GetLinkSentTo(string email)
    {
        using var client = new HttpClient();
        var messages = await client.GetFromJsonAsync<IEnumerable<MailCatcherMessage>>("http://localhost:1080/messages");
        var message = messages?.OrderByDescending(x => x.Id)
                               .FirstOrDefault(m => m.Recipients.Contains($"<{email}>"));
        if (message == null)
        {
            throw new Exception($"No message found for {email}.");
        }

        var html = await client.GetStringAsync($"http://localhost:1080/messages/{message.Id}.html");
        var link = Regex.Match(html, @"<a\s+(?:[^>]*?\s+)?href=([""'])(.*?)\1").Groups[2].Value;
        var baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "";
        return link.Replace(baseUrl, "");
    }
}