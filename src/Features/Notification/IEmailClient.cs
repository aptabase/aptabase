namespace Aptabase.Features.Notification;

public interface IEmailClient
{
    Task SendEmailAsync(string to, string subject, string templateName, Dictionary<string, string>? properties, CancellationToken cancellationToken);
}