using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0011)]
public class AddFeatureFlags : Migration
{
    public override void Up()
    {
        Create.Table("feature_flags")
            .WithNanoIdColumn("id").PrimaryKey()
            .WithNanoIdColumn("app_id").ForeignKey("apps", "id")
            .WithColumn("key").AsString(255).NotNullable()
            .WithColumn("value").AsString(4000).NotNullable()
            .WithColumn("environment").AsString(50).NotNullable()
            .WithColumn("conditions").AsString(4000).NotNullable()
            .WithTimestamps();

        Create.Index("idx_feature_flags_app_key_env")
            .OnTable("feature_flags")
            .OnColumn("app_id").Ascending()
            .OnColumn("key").Ascending()
            .OnColumn("environment").Ascending();
    }

    public override void Down()
    {
        Delete.Index("idx_feature_flags_app_key_env");
        Delete.Table("feature_flags");
    }
}