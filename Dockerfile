# Server Build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS server
WORKDIR /work/src

COPY ./src/Aptabase.csproj /work/src

RUN dotnet restore "./Aptabase.csproj"

COPY ./etc/clickhouse /work/etc/clickhouse
COPY ./etc/geoip /work/etc/geoip
COPY ./src /work/src

RUN dotnet publish "Aptabase.csproj" -c Release -o /work/publish /p:UseAppHost=false

# WebApp Build
FROM node:18 as webapp
WORKDIR /work

COPY ./src/package.json ./src/package-lock.json ./
RUN npm install

COPY ./src ./
RUN npm run build

# Final
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app

COPY --from=server /work/publish .
COPY --from=webapp /work/wwwroot ./wwwroot
COPY LICENSE .

ENTRYPOINT ["dotnet", "Aptabase.dll"]