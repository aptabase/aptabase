using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0016)]
public class OAuthTokens : Migration
{
    // Every opaque secret the authorization server hands out (authorization codes and
    // refresh tokens), stored as a SHA-256 hash and bound to a grant.
    public override void Up()
    {
        Create.Table("oauth_tokens")
            .WithColumn("token_hash").AsString(64).NotNullable().PrimaryKey()
            .WithColumn("kind").AsString(10).NotNullable()
            .WithNanoIdColumn("grant_id").ForeignKey("oauth_grants", "id")
            .WithColumn("client_id").AsString(255).NotNullable()
            .WithColumn("resource").AsCustom("text").NotNullable()
            .WithColumn("redirect_uri").AsCustom("text").Nullable()
            .WithColumn("code_challenge").AsString(128).Nullable()
            .WithColumn("expires_at").AsDateTimeOffset().NotNullable()
            .WithColumn("used_at").AsDateTimeOffset().Nullable()
            .WithColumn("replaced_by_hash").AsString(64).Nullable()
            .WithColumn("created_at").AsDateTimeOffset().WithDefault(SystemMethods.CurrentUTCDateTime).NotNullable();

        Create.Index("idx_oauth_tokens_grant_id").OnTable("oauth_tokens").OnColumn("grant_id");
        Create.Index("idx_oauth_tokens_expires_at").OnTable("oauth_tokens").OnColumn("expires_at");
    }

    public override void Down()
    {
        Delete.Table("oauth_tokens");
    }
}
