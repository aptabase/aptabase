using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0013)]
public class AddErrorTracking : Migration
{
    public override void Up()
    {
        Alter.Table("apps")
            .AddColumn("error_quota").AsInt32().WithDefaultValue(10000)
            .AddColumn("error_count").AsInt32().WithDefaultValue(0);
    }

    public override void Down()
    {
        Delete.Column("error_quota").FromTable("apps");
        Delete.Column("error_count").FromTable("apps");
    }
}
