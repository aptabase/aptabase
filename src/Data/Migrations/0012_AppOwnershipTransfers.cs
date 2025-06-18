using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0012)]
public class AppOwnershipTransfers : Migration
{

    public override void Up()
    {
        Create.Table("app_ownership_transfers")
            .WithNanoIdColumn("app_id").ForeignKey("apps", "id").NotNullable()
            .WithNanoIdColumn("current_owner_id").ForeignKey("users", "id").NotNullable()
            .WithColumn("new_owner_email").AsString(300).NotNullable()
            .WithColumn("requested_at").AsDateTime().NotNullable()
            .WithColumn("completed_at").AsDateTime().Nullable()
            .WithColumn("status").AsString(20).NotNullable().WithDefaultValue("pending");

        Create.Index("idx_app_ownership_transfers_app_id")
            .OnTable("app_ownership_transfers")
            .OnColumn("app_id");

        Create.Index("idx_app_ownership_transfers_new_owner_email")
            .OnTable("app_ownership_transfers")
            .OnColumn("new_owner_email");
    }

    public override void Down()
    {
        Delete.Table("app_ownership_transfers");
    }
} 