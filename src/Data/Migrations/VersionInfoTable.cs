using FluentMigrator.Runner.VersionTableInfo;

namespace Aptabase.Data.Migrations;

[VersionTableMetaData]
public class VersionTable : IVersionTableMetaData
{
    public string ColumnName => "version";
    public string SchemaName => "";
    public string TableName => "migration_history";
    public string UniqueIndexName => "idx_version";
    public string AppliedOnColumnName => "applied_at";
    public string DescriptionColumnName => "description";
    public object? ApplicationContext { get; set; }
    public bool OwnsSchema => true;
}