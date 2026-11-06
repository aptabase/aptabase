using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0014)]
public class AuthTokenUses : Migration
{
    // Records the id (jti) of every sign-in / registration link that has been redeemed,
    // so that a link can only be used once within its lifetime.
    public override void Up()
    {
        Create.Table("auth_token_uses")
            .WithNanoIdColumn("token_id").PrimaryKey()
            .WithColumn("expires_at").AsDateTime().NotNullable()
            .WithColumn("used_at").AsDateTime().NotNullable();

        Create.Index("idx_auth_token_uses_expires_at")
            .OnTable("auth_token_uses")
            .OnColumn("expires_at");
    }

    public override void Down()
    {
        Delete.Table("auth_token_uses");
    }
}
