using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0017)]
public class ApiGrantUsage : Migration
{
    // One row per grant, per app, per owner, per month. The owner at request time is part of
    // the key, so an ownership transfer neither moves already consumed requests to the new
    // owner nor keeps charging the old one: the new owner gets a row of their own.
    public override void Up()
    {
        Create.Table("api_grant_usage")
            .WithNanoIdColumn("grant_id").ForeignKey("oauth_grants", "id").PrimaryKey()
            .WithNanoIdColumn("app_id").ForeignKey("apps", "id").PrimaryKey()
            .WithColumn("period").AsDate().NotNullable().PrimaryKey()
            .WithNanoIdColumn("owner_id").ForeignKey("users", "id").PrimaryKey()
            .WithColumn("requests").AsInt64().WithDefaultValue(0).NotNullable()
            .WithColumn("rejected").AsInt64().WithDefaultValue(0).NotNullable()
            .WithColumn("modified_at").AsDateTimeOffset().WithDefault(SystemMethods.CurrentUTCDateTime).NotNullable();

        Create.Index("idx_api_grant_usage_owner_period").OnTable("api_grant_usage")
            .OnColumn("owner_id").Ascending().OnColumn("period").Ascending();
        Create.Index("idx_api_grant_usage_app_period").OnTable("api_grant_usage")
            .OnColumn("app_id").Ascending().OnColumn("period").Ascending();
    }

    public override void Down()
    {
        Delete.Table("api_grant_usage");
    }
}
