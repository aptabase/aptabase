using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0012)]
public class IncreaseProviderUidSize : Migration
{
    public override void Up()
    {
        // Increase provider_uid size from 40 to 80 to accommodate longer OAuth provider user IDs (Authentik)
        Alter.Column("provider_uid").OnTable("user_providers").AsString(80).NotNullable();
    }

    public override void Down()
    {
        // Revert back to 40 characters
        Alter.Column("provider_uid").OnTable("user_providers").AsString(40).NotNullable();
    }
} 