using System.Net.Mail;

namespace Aptabase.Application.Notification;

public class SmtpEmailClient : IEmailClient
{
    private readonly EnvSettings _env;
    private readonly SmtpClient _smtp;
    private readonly TemplateEngine _engine = new();

    public SmtpEmailClient(EnvSettings env)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _smtp = new SmtpClient(env.SmtpHost, env.SmtpPort);
    }

    public async Task SendEmailAsync(string to, string subject, string templateName, Dictionary<string, string> properties, CancellationToken cancellationToken)
    {
        var body = await _engine.Render(templateName, properties);
        await _smtp.SendMailAsync("Aptabase <notification@aptabase.com>", to, subject, body, cancellationToken);
    }
}