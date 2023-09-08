using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0008)]
public class AddLockReason : Migration
{
    public override void Up()
    {
        Alter.Table("users")
            .AddColumn("lock_reason").AsFixedLengthString(1).Nullable();
    }

    public override void Down()
    {
        Delete.Column("lock_reason").FromTable("users");
    }
}