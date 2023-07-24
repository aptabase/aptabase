using System.Data.Common;

namespace Aptabase.Data;

public interface IDbContext
{
    DbConnection Connection { get; }
}

public class DbContext : IDbContext
{
    private readonly DbDataSource _ds;

    public DbContext(DbDataSource ds)
    {
        _ds = ds ?? throw new ArgumentNullException(nameof(ds));
    }

    public DbConnection Connection => _ds.CreateConnection();
}