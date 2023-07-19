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

        if (!string.IsNullOrEmpty(env.SmtpHost))
        {
            services.AddSingleton<IEmailClient, SmtpEmailClient>();
            return;
        }

        services.AddSingleton<IEmailClient, LoggerEmailClient>();
    }
}