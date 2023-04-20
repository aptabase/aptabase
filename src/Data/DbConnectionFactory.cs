using System.Data.Common;
using Npgsql;

namespace Aptabase.Data;

public interface IDbConnectionFactory
{
    DbConnection Create();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connString)
    {
        _connectionString = connString;
    }

    public DbConnection Create() => new NpgsqlConnection(_connectionString);
}