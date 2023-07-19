namespace Aptabase.Application.Notification;

public class LoggerEmailClient : IEmailClient
{
    private readonly TemplateEngine _engine = new();
    private readonly ILogger _logger;

    public LoggerEmailClient(ILogger<LoggerEmailClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendEmailAsync(string to, string subject, string templateName, Dictionary<string, string>? properties, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending email to '{to}' with template '{templateName}' and '{properties}'", to, templateName, properties);
        await Task.CompletedTask;
    }
}