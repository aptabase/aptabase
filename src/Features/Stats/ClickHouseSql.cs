namespace Aptabase.Features.Stats;

public static class ClickHouseSql
{
    public static string Escape(string value)
        => value.Replace("\\", "\\\\").Replace("'", "\\'");

    public static string Literal(string value)
        => $"'{Escape(value)}'";
}
