using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0014)]
public class AppRetention : Migration
{
    public override void Up()
    {
        Alter.Table("apps")
            .AddColumn("retention_months").AsInt32().NotNullable().WithDefaultValue(60);
        Execute.Sql("ALTER TABLE apps ADD CONSTRAINT apps_retention_months_check CHECK (retention_months IN (6, 12, 24, 60))");
    }

    public override void Down()
    {
        Execute.Sql("ALTER TABLE apps DROP CONSTRAINT apps_retention_months_check");
        Delete.Column("retention_months").FromTable("apps");
    }
}
