using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0012)]
public class AppOwnershipTransfers : Migration
{

    public override void Up()
    {
        Create.Table("app_requests")
            .WithNanoIdColumn("app_id").ForeignKey("apps", "id").NotNullable()
            .WithNanoIdColumn("current_owner_id").ForeignKey("users", "id").NotNullable()
            .WithColumn("target_user_email").AsString(300).NotNullable()
            .WithColumn("purpose").AsString(20).NotNullable() // 'AppOwnership' or 'AppShare'
            .WithColumn("requested_at").AsDateTime().NotNullable()
            .WithColumn("completed_at").AsDateTime().Nullable()
            .WithColumn("status").AsString(20).NotNullable().WithDefaultValue("pending");

        // create indexes
        Create.Index("idx_app_requests_app_id")
            .OnTable("app_requests")
            .OnColumn("app_id");

        Create.Index("idx_app_requests_target_user_email")
            .OnTable("app_requests")
            .OnColumn("target_user_email");

        Create.Index("idx_app_requests_purpose")
            .OnTable("app_requests")
            .OnColumn("purpose");
    }

    public override void Down()
    {
        Delete.Table("app_requests");
    }
} 