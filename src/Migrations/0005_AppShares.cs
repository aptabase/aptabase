using FluentMigrator;

namespace Aptabase.Migrations;

[Migration(0005)]
public class AppShares : Migration
{

    public override void Up()
    {
        Create.Table("app_shares")
            .WithNanoIdColumn("app_id").ForeignKey("apps", "id").PrimaryKey()
            .WithColumn("email").AsString(300).NotNullable().PrimaryKey()
            .WithTimestamps();
    }

    public override void Down()
    {
        Delete.Table("app_shares");
    }
}