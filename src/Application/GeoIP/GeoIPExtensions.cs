namespace Aptabase.Application.GeoIP;

public static class GeoIPExtensions
{
    public static void AddGeoIPClient(this IServiceCollection services, EnvSettings env)
    {
        if (env.IsManagedCloud)
        {
            services.AddSingleton<IGeoIPClient, CloudGeoClient>();
            return;
        }

        services.AddSingleton<IGeoIPClient, DatabaseGeoClient>();
    }
}