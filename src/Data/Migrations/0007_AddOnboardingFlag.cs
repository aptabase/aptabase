using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0007)]
public class AddOnboardingFlag : Migration
{
    public override void Up()
    {
        Alter.Table("apps")
            .AddColumn("has_events").AsBoolean().Nullable();

        Execute.Sql("UPDATE apps SET has_events = true");

        Alter.Table("apps")
            .AlterColumn("has_events").AsBoolean().NotNullable();
    }

    public override void Down()
    {
        Delete.Column("has_events").FromTable("apps");
    }
}