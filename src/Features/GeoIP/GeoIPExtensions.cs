namespace Aptabase.Features.GeoIP;

public static class GeoIPExtensions
{
    public static void AddGeoIPClient(this IServiceCollection services, EnvSettings env)
    {
        if (env.IsManagedCloud)
        {
            services.AddSingleton<GeoIPClient, CloudGeoClient>();
            return;
        }

        services.AddSingleton<GeoIPClient, DatabaseGeoClient>();
    }
}