using ClickHouse.Client;
using Dapper;

namespace Aptabase.Data;

public interface IClickHouseMigrationRunner
{ 
    void MigrateUp();
}

public class ClickHouseMigrationRunner : IClickHouseMigrationRunner
{
    private readonly IClickHouseConnection _conn;
    private readonly ILogger<ClickHouseMigrationRunner> _logger;

    public ClickHouseMigrationRunner(IClickHouseConnection conn, ILogger<ClickHouseMigrationRunner> logger)
    {
        _conn = conn ?? throw new ArgumentNullException(nameof(conn));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void MigrateUp()
    {
        var files = Directory.GetFiles("./etc/clickhouse").OrderBy(x => x);
        foreach (string file in files)
        {
            var content = File.ReadAllText(file);
            _logger.LogInformation($"Executing ClickHouse migration: {file}");
            _conn.Execute(content);
        }
    }
}