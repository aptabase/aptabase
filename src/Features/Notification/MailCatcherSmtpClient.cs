using System.Net.Mail;

namespace Aptabase.Features.Notification;

public class MailCatcherSmtpClient : IEmailClient
{
    private readonly TemplateEngine _engine = new();
    private readonly SmtpClient _client;

    public MailCatcherSmtpClient(string host, int port)
    {
        _client = new SmtpClient(host, port);
    }

    public async Task SendEmailAsync(string to, string subject, string templateName, Dictionary<string, string>? properties, CancellationToken cancellationToken)
    {
        var body = await _engine.Render(templateName, subject, properties);

        using var message = new MailMessage("hi@aptabase.com", to)
        {
            Subject = subject,
            Body = body
        };

        await _client.SendMailAsync(message, cancellationToken);
    }
}