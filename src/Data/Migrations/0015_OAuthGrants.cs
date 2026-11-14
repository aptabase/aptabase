using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0015)]
public class OAuthGrants : Migration
{
    // One row per consent (kind 'user') or service client (kind 'service').
    // This is the principal every bearer request resolves to.
    public override void Up()
    {
        Create.Table("oauth_grants")
            .WithNanoIdColumn("id").PrimaryKey()
            .WithColumn("kind").AsString(10).NotNullable()
            .WithNanoIdColumn("subject_id").ForeignKey("users", "id")
            .WithColumn("client_id").AsString(255).NotNullable()
            .WithColumn("client_name").AsString(80).NotNullable()
            .WithColumn("scopes").AsString(200).NotNullable()
            .WithColumn("app_ids").AsCustom("jsonb").Nullable()
            .WithColumn("secret_hash").AsString(64).Nullable()
            .WithColumn("secret_prefix").AsString(12).Nullable()
            .WithColumn("revoked_at").AsDateTimeOffset().Nullable()
            .WithColumn("revoked_reason").AsString(20).Nullable()
            .WithColumn("consented_at").AsDateTimeOffset().WithDefault(SystemMethods.CurrentUTCDateTime).NotNullable()
            .WithColumn("last_used_at").AsDateTimeOffset().Nullable()
            .WithColumn("denied_count").AsInt32().WithDefaultValue(0).NotNullable()
            .WithColumn("denied_at").AsDateTimeOffset().Nullable()
            .WithTimestamps();

        Create.Index("idx_oauth_grants_subject_id").OnTable("oauth_grants").OnColumn("subject_id");

        // Re-consent for the same user and client replaces the live grant in place
        Execute.Sql(@"CREATE UNIQUE INDEX idx_oauth_grants_subject_client
                      ON oauth_grants (subject_id, client_id)
                      WHERE revoked_at IS NULL AND kind = 'user'");
    }

    public override void Down()
    {
        Delete.Table("oauth_grants");
    }
}
