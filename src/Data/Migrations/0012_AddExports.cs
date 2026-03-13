using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0012)]
public class AddExports : Migration
{
    public override void Up()
    {
        Create.Table("exports")
            .WithNanoIdColumn("id").PrimaryKey()
            .WithNanoIdColumn("app_id").ForeignKey("apps", "id")
            .WithColumn("start_date").AsDateTime().NotNullable()
            .WithColumn("end_date").AsDateTime().NotNullable()
            .WithColumn("s3_bucket").AsString(255).NotNullable()
            .WithColumn("s3_key").AsString(500).NotNullable()
            .WithColumn("format").AsString(50).NotNullable()
            .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(0) // 0 = Pending
            .WithColumn("error_message").AsString().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("completed_at").AsDateTime().Nullable();

        Create.Index("idx_exports_app_id_created")
            .OnTable("exports")
            .OnColumn("app_id").Ascending()
            .OnColumn("created_at").Ascending();

        Create.Index("idx_exports_status_created")
            .OnTable("exports")
            .OnColumn("status").Ascending()
            .OnColumn("created_at").Ascending();
    }

    public override void Down()
    {
        Delete.Index("idx_exports_app_id_created");
        Delete.Index("idx_exports_status_created");
        Delete.Table("exports");
    }
}