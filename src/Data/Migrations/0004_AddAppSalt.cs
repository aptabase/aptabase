using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0004)]
public class AddAppSalt : Migration
{

    public override void Up()
    {
        Create.Table("app_salts")
            .WithNanoIdColumn("app_id").ForeignKey("apps", "id").PrimaryKey()
            .WithColumn("date").AsString(10).NotNullable().PrimaryKey()
            .WithColumn("salt").AsBinary(16).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("app_salts");
    }
}