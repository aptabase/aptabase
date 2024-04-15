using MimeKit;
using MailKit.Net.Smtp;

namespace Aptabase.Features.Notification;

public class SmtpEmailClient : IEmailClient
{
    private readonly EnvSettings _env;
    private readonly TemplateEngine _engine = new();

    public SmtpEmailClient(EnvSettings env)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
    }

    private static string[] BillingTemplates => new[] { "TrialEndsSoon", "UsageLevel80", "UsageLevel90", "UsageLevel100" };

    public async Task SendEmailAsync(string to, string subject, string templateName, Dictionary<string, string>? properties, CancellationToken cancellationToken)
    {
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_env.SmtpHost, _env.SmtpPort, IsTLSPort(_env.SmtpPort), cancellationToken);

        if (!string.IsNullOrEmpty(_env.SmtpUsername))
            await smtp.AuthenticateAsync(_env.SmtpUsername, _env.SmtpPassword, cancellationToken);

        var body = await _engine.Render(templateName, subject, properties);
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress("", _env.SmtpFromAddress));
        msg.To.Add(new MailboxAddress("", to));
        if (_env.IsManagedCloud && BillingTemplates.Contains(templateName))
        {
            msg.Bcc.Add(new MailboxAddress("", "goenning@aptabase.com"));
        }
        msg.Subject = subject;
        msg.Body = new TextPart("html") { Text = body };
        await smtp.SendAsync(msg, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);
    }

    private static bool IsTLSPort(int port) => port == 587 || port == 465;
}