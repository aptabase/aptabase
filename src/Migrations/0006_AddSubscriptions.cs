using FluentMigrator;

namespace Aptabase.Migrations;

[Migration(0006)]
public class AddSubscriptions : Migration
{
    public override void Up()
    {
        Create.Table("subscriptions")
            .WithColumn("id").AsInt64().NotNullable().PrimaryKey()
            .WithNanoIdColumn("owner_id").NotNullable()
            .WithColumn("customer_id").AsInt64().NotNullable()
            .WithColumn("product_id").AsInt64().NotNullable()
            .WithColumn("variant_id").AsInt64().NotNullable()
            .WithColumn("status").AsString().NotNullable()
            .WithColumn("ends_at").AsDateTimeOffset().Nullable()
            .WithTimestamps();

        Create.Index("idx_subscriptions_owner")
              .OnTable("subscriptions")
              .OnColumn("owner_id");
    }

    public override void Down()
    {
        Delete.Index("idx_subscriptions_owner");
        Delete.Table("billing");
    }
}