# Server Build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS server
WORKDIR /src

COPY ./src/Aptabase.csproj .
RUN dotnet restore "./Aptabase.csproj"
COPY ./etc .
COPY ./src .
RUN dotnet publish "Aptabase.csproj" -c Release -o /app/publish /p:UseAppHost=false

# WebApp Build
FROM node:18 as webapp
WORKDIR /src

COPY ./src/package.json ./src/package-lock.json ./
RUN npm install
COPY ./src .
RUN npm run build

# Final
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app

COPY --from=server /app/publish .
COPY --from=webapp /src/wwwroot ./wwwroot
COPY LICENSE .

ENTRYPOINT ["dotnet", "Aptabase.dll"]