var builder = DistributedApplication.CreateBuilder(args);

var clickHouseUser = builder.AddParameter("ClickHouseUser");
var clickHousePw = builder.AddParameter("ClickHousePassword");

var clickhouse = builder.AddClickHouse("clickhouse", userName: clickHouseUser, password: clickHousePw, port: 8123)
                    .WithVolume("clickhousedev-data", "/var/lib/clickhouse")
                    .AddDatabase("clickhousedb", "default");

var pgUser = builder.AddParameter("PostgresUser");
var pgPw = builder.AddParameter("PostgresPassword");

var postgres = builder.AddPostgres("postgres", pgUser, pgPw, port: 5432)
                    .WithVolume("pgdev-data", "/var/lib/postgresql/data")
                    .AddDatabase("postgresdb", "aptabase");

var mailcatcher = builder.AddMailCatcher("mailcatcher");

var api = builder.AddProject<Projects.Aptabase>("api")
       .WithReference(clickhouse)
       .WithReference(postgres)
       .WithReference(mailcatcher)
       .WithExternalHttpEndpoints();

builder.AddNpmApp("frontend", "../../src", scriptName: "dev")
    .WithReference(api)
    .WithHttpsEndpoint(port: 3000, isProxied: false)
    .WithExternalHttpEndpoints();

builder.Build().Run();
