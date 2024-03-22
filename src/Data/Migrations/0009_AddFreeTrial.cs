using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0009)]
public class AddFreeTrial : Migration
{
    public override void Up()
    {
        Alter.Table("users")
            .AddColumn("free_trial_ends_at").AsDateTimeOffset().Nullable()
            .AddColumn("free_quota").AsInt64().Nullable();
    }

    public override void Down()
    {
        Delete.Column("free_trial_ends_at").FromTable("users");
        Delete.Column("free_quota").FromTable("users");
    }
}