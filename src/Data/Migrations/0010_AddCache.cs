using FluentMigrator;

namespace Aptabase.Data.Migrations;

[Migration(0010)]
public class AddCache : Migration
{
    public override void Up()
    {
        Create.Table("cache")
            .WithColumn("key").AsString(100).NotNullable().PrimaryKey()
            .WithColumn("value").AsCustom("TEXT").NotNullable()
            .WithColumn("expires_at").AsDateTimeOffset().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("cache");
    }
}