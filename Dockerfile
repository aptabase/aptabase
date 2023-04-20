# Server Build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS server
WORKDIR /src
COPY Aptabase.csproj .
RUN dotnet restore "./Aptabase.csproj"
COPY . .
RUN dotnet publish "Aptabase.csproj" -c Release -o /app/publish /p:UseAppHost=false

# WebApp Build
FROM node:18 as webapp
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm install
COPY . .
RUN npm run build

# Final
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY LICENSE .
COPY --from=server /app/publish .
COPY --from=webapp /src/wwwroot ./wwwroot
ENTRYPOINT ["dotnet", "Aptabase.dll"]
