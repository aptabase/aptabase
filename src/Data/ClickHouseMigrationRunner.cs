using Dapper;
using ClickHouse.Client.ADO;
using Aptabase.Features;

namespace Aptabase.Data;

public interface IClickHouseMigrationRunner
{ 
    void MigrateUp();
}

public class ClickHouseMigrationRunner : IClickHouseMigrationRunner
{
    private readonly ClickHouseConnection _conn;
    private readonly EnvSettings _env;
    private readonly ILogger<ClickHouseMigrationRunner> _logger;

    public ClickHouseMigrationRunner(ClickHouseConnection conn, EnvSettings env, ILogger<ClickHouseMigrationRunner> logger)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _conn = conn ?? throw new ArgumentNullException(nameof(conn));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void MigrateUp()
    {
        var pathToMigrations = Path.Combine(_env.EtcDirectoryPath, "clickhouse");
        var files = Directory.GetFiles(pathToMigrations).OrderBy(x => x);
        foreach (string file in files)
        {
            var content = File.ReadAllText(file);
            _logger.LogDebug($"Executing ClickHouse migration: {file}");
            _conn.Execute(content);
        }
    }
}