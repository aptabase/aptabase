using System.Reflection;
using System.Text;

namespace Aptabase.Features.Notification;

public class TemplateEngine
{
    private Assembly _assembly = Assembly.GetEntryAssembly() ?? throw new Exception("Failed to find the entry assembly");
    private Dictionary<string, string> _templates = new();

    public async Task<string> Render(string name, string subject, Dictionary<string, string>? properties)
    {
        var baseTemplate = await GetTemplate("Base");
        var emailTemplate = await GetTemplate(name);

        if (properties is not null)
        {
            foreach (var (key, value) in properties)
                emailTemplate = emailTemplate.Replace($"##{key.ToUpper()}##", value);
        }

        return baseTemplate.Replace("##SUBJECT##", subject).Replace("##BODY##", emailTemplate);
    }

    public async Task<string> GetTemplate(string name)
    {
        if (_templates.ContainsKey(name))
            return _templates[name];

        var resourceStream = _assembly.GetManifestResourceStream($"Aptabase.assets.Templates.{name}.html");
        if (resourceStream == null)
            throw new Exception($"Failed to find the embedded resource named {name}");

        var reader = new StreamReader(resourceStream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        _templates.Add(name, content);
        return content;
    }
}