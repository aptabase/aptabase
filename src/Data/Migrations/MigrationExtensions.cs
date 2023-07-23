using FluentMigrator;
using FluentMigrator.Builders.Create.Table;

namespace Aptabase.Data.Migrations;

internal static class MigrationExtensions
{
    public static ICreateTableColumnOptionOrWithColumnSyntax WithNanoIdColumn(this ICreateTableWithColumnSyntax tableWithColumnSyntax, string name)
    {
        return tableWithColumnSyntax
            .WithColumn(name)
            .AsString(22)
            .NotNullable();
    }

    public static ICreateTableColumnOptionOrWithColumnSyntax WithTimestamps(this ICreateTableWithColumnSyntax tableWithColumnSyntax)
    {
        return tableWithColumnSyntax
            .WithColumn("created_at").AsDateTimeOffset().WithDefault(SystemMethods.CurrentUTCDateTime).NotNullable()
            .WithColumn("modified_at").AsDateTimeOffset().WithDefault(SystemMethods.CurrentUTCDateTime).NotNullable();
    }
}