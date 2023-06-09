using FluentMigrator;

namespace Aptabase.Migrations;

[Migration(0003)]
public class AddBlobs : Migration
{
    public override void Up()
    {
        Create.Table("blobs")
            .WithColumn("path").AsString(80).NotNullable().PrimaryKey()
            .WithColumn("content").AsBinary().NotNullable()
            .WithColumn("content_type").AsString(80).NotNullable()
            .WithTimestamps();

        Alter.Table("apps")
            .AddColumn("icon_path").AsString(80).Nullable();
    }

    public override void Down()
    {
        Delete.Column("icon_path").FromTable("apps");
        Delete.Table("blobs");
    }
}