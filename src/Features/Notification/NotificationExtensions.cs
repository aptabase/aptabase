namespace Aptabase.Features.Notification;

public static class NotificationExtensions
{
    public static void AddEmailClient(this IServiceCollection services, EnvSettings env)
    {
        if (env.IsManagedCloud)
        {
            services.AddSingleton<IEmailClient, SESEmailClient>();
            return;
        }

        if (!string.IsNullOrEmpty(env.MailCatcherConnectionString))
        {
            services.AddSingleton<IEmailClient, MailCatcherSmtpClient>(sp =>
            {
                var smtpUri = new Uri(env.MailCatcherConnectionString);

                return new MailCatcherSmtpClient(smtpUri.Host, smtpUri.Port);
            });
            return;
        }

        if (!string.IsNullOrEmpty(env.SmtpHost))
        {
            services.AddSingleton<IEmailClient, SmtpEmailClient>();
            return;
        }

        services.AddSingleton<IEmailClient, LoggerEmailClient>();
    }
}