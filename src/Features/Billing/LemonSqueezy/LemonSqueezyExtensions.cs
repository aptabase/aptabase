using System.Net.Http.Headers;

namespace Aptabase.Features.Billing.LemonSqueezy;

public static class LemonSqueezyExtensions
{
    public static void AddLemonSqueezy(this IServiceCollection services, EnvSettings env)
    {
        services.AddSingleton<LemonSqueezyClient>();
        services.AddHttpClient("LemonSqueezy", client =>
        {
            client.BaseAddress = new Uri("https://api.lemonsqueezy.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", env.LemonSqueezyApiKey);
        });
    }
}