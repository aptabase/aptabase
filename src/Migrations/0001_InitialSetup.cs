using FluentMigrator;

namespace Aptabase.Migrations;

[Migration(0001)]
public class InitialSetup : Migration
{
    public override void Up()
    {
        Create.Table("apps")
            .WithNanoIdColumn("id").PrimaryKey()
            .WithNanoIdColumn("owner_id")
            .WithColumn("name").AsString(40).NotNullable()
            .WithColumn("app_key").AsString(20).NotNullable()
            .WithColumn("deleted_at").AsDateTime().Nullable()
            .WithTimestamps();

        Create.Index("idx_apps_owner")
                .OnTable("apps")
                .OnColumn("owner_id").Ascending();

        Create.Index("idx_apps_key")
                .OnTable("apps")
                .OnColumn("app_key").Ascending()
                .WithOptions().Unique();

        Create.Table("users")
            .WithNanoIdColumn("id").PrimaryKey()
            .WithColumn("name").AsString(40).NotNullable()
            .WithColumn("email").AsString(300).NotNullable()
            .WithTimestamps();

        Create.Index("idx_users_email")
              .OnTable("users")
              .OnColumn("email")
              .Unique();
    }

    public override void Down()
    {
        Delete.Table("apps");
        Delete.Table("users");
    }
}