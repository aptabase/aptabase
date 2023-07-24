using System.Net;
using System.Net.Mail;

namespace Aptabase.Features.Notification;

public class SmtpEmailClient : IEmailClient
{
    private readonly EnvSettings _env;
    private readonly SmtpClient _smtp;
    private readonly TemplateEngine _engine = new();

    public SmtpEmailClient(EnvSettings env)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _smtp = new SmtpClient(env.SmtpHost, env.SmtpPort);
        _smtp.EnableSsl = IsTLSPort(env.SmtpPort);

        if (!string.IsNullOrEmpty(env.SmtpUsername))
            _smtp.Credentials = new NetworkCredential(env.SmtpUsername, env.SmtpPassword);
    }

    public async Task SendEmailAsync(string to, string subject, string templateName, Dictionary<string, string>? properties, CancellationToken cancellationToken)
    {
        var body = await _engine.Render(templateName, subject, properties);
        var msg = new MailMessage(_env.SmtpFromAddress, to, subject, body)
        {
            IsBodyHtml = true
        };
        await _smtp.SendMailAsync(msg, cancellationToken);
    }

    private bool IsTLSPort(int port) => port == 587 || port == 465;
}