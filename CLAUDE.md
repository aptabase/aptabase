# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Aptabase is an open-source privacy-first analytics platform for Mobile, Desktop, and Web apps. It's built as a hybrid .NET 8 backend with a React/TypeScript frontend, using a monolithic architecture where both frontend and backend code live in the `src/` directory.

## Tech Stack

**Backend (.NET 8)**
- ASP.NET Core with minimal API/controllers
- PostgreSQL (primary database) via Npgsql and Dapper
- ClickHouse or Tinybird (analytics data storage)
- FluentMigrator for database migrations
- Feature-based folder organization in `src/Features/`

**Frontend (React + TypeScript)**
- React 18 with TypeScript
- Vite for bundling and dev server
- TailwindCSS for styling
- Jotai for state management
- React Router for routing
- Source code in `src/webapp/`

**Development Infrastructure**
- Docker Compose for local dependencies (PostgreSQL, ClickHouse, Mailcatcher)
- Two parallel dev servers: .NET backend and Vite frontend
- Vite proxy forwards `/api`, `/webhook`, `/uploads` to backend

## Development Commands

### Initial Setup
```bash
# Install dependencies
cd src/
npm install
dotnet restore

# Start Docker services (PostgreSQL, ClickHouse, Mailcatcher)
docker compose up -d

# Copy launch settings
cp src/Properties/launchSettings.example.json src/Properties/launchSettings.json
```

### Development
```bash
# Start both servers (run in separate terminals from src/ directory)
npm run dev          # Vite dev server on https://localhost:3000
dotnet watch         # .NET backend on https://localhost:5251

# Or use Makefile from root
make dev             # Starts both in parallel
```

### Building
```bash
# Frontend only
cd src/
npm run build        # Outputs to src/wwwroot/

# Backend
dotnet build         # From root or src/
```

### Testing
```bash
# Run unit tests
dotnet test tests/UnitTests/

# Run integration tests
dotnet test tests/IntegrationTests/

# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Architecture

### Backend Structure (`src/`)

The backend uses **feature-based organization** where each feature is self-contained in `src/Features/`:

- `Features/Authentication/` - Auth, OAuth (GitHub, Google), session management
- `Features/Apps/` - App creation, management, API keys
- `Features/Stats/` - Analytics queries and aggregations
- `Features/Ingestion/` - Event ingestion pipeline with buffering
- `Features/Billing/` - LemonSqueezy integration, subscription management
- `Features/Privacy/` - Daily user hashing, GDPR compliance
- `Features/GeoIP/` - MaxMind GeoIP2 integration
- `Features/Notification/` - Email notifications via SMTP/AWS SES
- `Features/Export/` - Data export functionality
- `Features/FeatureFlags/` - Feature flag system
- `Features/Blob/` - Binary storage (avatars, etc.)
- `Features/Cache/` - Caching layer

Each feature typically contains:
- API controllers (if exposed)
- Service interfaces and implementations
- Query classes for data access
- Background jobs (cron jobs)

**Key Files:**
- `Program.cs` - Application entry point, dependency injection setup, middleware pipeline
- `Data/Migrations/` - FluentMigrator PostgreSQL migrations
- `Data/ClickHouseMigrationRunner.cs` - ClickHouse schema migrations
- `EnvSettings.cs` - Environment configuration management

### Frontend Structure (`src/webapp/`)

- `features/` - Feature-based React components (analytics, apps, auth, billing, etc.)
- `components/` - Shared UI components
- `atoms/` - Jotai state atoms
- `hooks/` - Custom React hooks
- `fns/` - Utility functions
- `layout/` - Layout components
- `router.tsx` - React Router configuration
- `main.tsx` - Application entry point

**Path Aliases (configured in vite.config.ts):**
- `@components` → `webapp/components`
- `@features` → `webapp/features`
- `@hooks` → `webapp/hooks`
- `@fns` → `webapp/fns`

### Data Flow

1. **Event Ingestion**: SDK → `/api/v0/event` → `InMemoryEventBuffer` → `EventBackgroundWritter` → ClickHouse/Tinybird
2. **Analytics Queries**: Frontend → API Controllers → `IQueryClient` → ClickHouse/Tinybird → Stats aggregation
3. **Authentication**: OAuth provider → callback → `AuthService` → PostgreSQL → Cookie session

### Database Strategy

**PostgreSQL** stores:
- User accounts and authentication
- App metadata, API keys
- Billing subscriptions
- Feature flags
- Binary blobs (small files)

**ClickHouse/Tinybird** stores:
- Analytics events (high volume)
- Time-series data

The system supports both ClickHouse (self-hosted) and Tinybird (managed) as analytics backends. The choice is controlled by environment variables (either `CLICKHOUSE_URL` or `TINYBIRD_BASE_URL`/`TINYBIRD_TOKEN`).

### Environment Configuration

All environment variables are centralized in `EnvSettings.cs`. Key variables:

**Required:**
- `BASE_URL` - Application base URL
- `DATABASE_URL` - PostgreSQL connection string
- `AUTH_SECRET` - Secret for signing auth tokens

**Analytics Backend (one of):**
- `CLICKHOUSE_URL` - ClickHouse connection
- `TINYBIRD_BASE_URL` + `TINYBIRD_TOKEN` - Tinybird API

**Email:**
- `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM_ADDRESS`

**OAuth (optional):**
- `OAUTH_GITHUB_CLIENT_ID`, `OAUTH_GITHUB_CLIENT_SECRET`
- `OAUTH_GOOGLE_CLIENT_ID`, `OAUTH_GOOGLE_CLIENT_SECRET`

See `src/Properties/launchSettings.example.json` for development defaults.

### Rate Limiting

The application uses ASP.NET Core rate limiting with policies:
- `SignUp` - 4 requests per hour per IP
- `Stats` - 1000 requests per hour per IP
- `EventIngestion` - 20 requests per second per IP
- `FeatureFlags` - 10 requests per second per IP

### Background Jobs

Several `IHostedService` implementations handle periodic tasks:
- `EventBackgroundWritter` - Flushes buffered events to ClickHouse/Tinybird
- `PurgeDailySaltsCronJob` - Removes old privacy salts
- `OveruseNotificationCronJob` - Sends billing alerts
- `TrialExpiredCronJob` - Handles trial expirations
- `TrialNotificationCronJob` - Sends trial reminders
- `ResetOveruseCronJob` - Resets monthly usage counters

## Development Notes

### Running Migrations

Migrations run automatically on startup via `Program.RunMigrations()`. To create new migrations:

```bash
# PostgreSQL migration
dotnet tool install -g FluentMigrator.DotNet.Cli
dotnet fm migrate -p postgres -c "YourConnectionString"

# Add new migration class in src/Data/Migrations/
```

### Mailcatcher

In development, emails are caught by Mailcatcher. Access the UI at http://localhost:1080.

### HTTPS in Development

Vite uses mkcert plugin to generate local HTTPS certificates. Both frontend (port 3000) and backend (port 5251) run on HTTPS in development.

### Frontend Build Output

When building for production, frontend assets are output to `src/wwwroot/` and served by ASP.NET Core as static files.

### Testing Strategy

- **UnitTests** - Test business logic, utilities, isolated features
- **IntegrationTests** - Test API endpoints with `Microsoft.AspNetCore.Mvc.Testing`

Uses xUnit, Moq, and FluentAssertions.

## Common Patterns

### Adding a New Feature

1. Create feature folder in `src/Features/YourFeature/`
2. Add controller if API endpoint needed
3. Add service interface and implementation
4. Register services in `Program.cs` dependency injection
5. Create corresponding React components in `src/webapp/features/yourfeature/`
6. Add routes in `src/webapp/router.tsx` if needed

### Adding a Database Migration

1. Create new migration class in `src/Data/Migrations/` inheriting from `Migration`
2. Implement `Up()` and `Down()` methods
3. Add `[Migration(timestamp)]` attribute with timestamp
4. Migration runs automatically on next startup

### Adding an API Endpoint

Controllers use attribute routing. Place in appropriate feature folder and register automatically via `builder.Services.AddControllers()`.
