using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0002)]
public class UserProvider : Migration
{
    public override void Up()
    {
        Create.Table("user_providers")
            .WithColumn("provider_name").AsString(40).NotNullable().PrimaryKey()
            .WithColumn("provider_uid").AsString(40).NotNullable().PrimaryKey()
            .WithNanoIdColumn("user_id").ForeignKey("users", "id")
            .WithTimestamps();
    }

    public override void Down()
    {
        Delete.Table("user_providers");
    }
}