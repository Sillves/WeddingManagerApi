# Claude.md - Amare.Wedding API Context

This document provides AI assistants like Claude with structured context about the Amare.Wedding API project.

## Project Overview

**Name:** Amare.Wedding (Vowly)
**Type:** Wedding management SaaS platform API
**Live URL:** https://amare.wedding

Amare (Ah-mah-re) is Italian for "to love" - chosen because wedding planning is about celebrating love.

## Tech Stack

- **Runtime:** .NET 10
- **Framework:** ASP.NET Core 10 (Minimal APIs)
- **Database:** PostgreSQL 17+
- **ORM:** Entity Framework Core
- **Cloud:** Scaleway (Container Registry, VPS)
- **CI/CD:** GitHub Actions
- **Infrastructure:** Docker, Docker Compose, Nginx
- **Reverse Proxy:** Nginx with Let's Encrypt SSL

## Architecture

Clean architecture with clear separation of concerns:

```
WeddingManager.Web/          # API endpoints (Minimal APIs) - Entry point
WeddingManager.Application/  # Business logic & services - Use cases
WeddingManager.Infrastructure/ # Database & external services - Data access
WeddingManager.Domain/       # Domain models & entities - Core models
```

**Key Patterns:**
- Minimal APIs (not Controllers)
- Dependency injection via ASP.NET Core DI
- Repository pattern (Infrastructure layer)
- Domain-driven design principles

## API Endpoints

### Health
- `GET /health` - Returns "Healthy"

### Weddings
- `GET /api/weddings` - Get all weddings for user
- `POST /api/weddings` - Create new wedding
- `DELETE /api/weddings/{id}` - Delete wedding
- `POST /api/weddings/{id}/rsvp` - Submit RSVP

### Guests
- `GET /api/weddings/{weddingId}/guests` - Get guests for wedding
- `POST /api/weddings/{weddingId}/guests` - Add guest
- `GET /api/guests/{guestId}` - Get guest by ID
- `PUT /api/guests/{guestId}` - Update guest
- `DELETE /api/guests/{guestId}` - Remove guest

### Events
- `GET /api/events` - Get all events
- `GET /api/weddings/{weddingId}/events` - Get events for wedding
- `POST /api/weddings/{weddingId}/events` - Create event
- `GET /api/events/{eventId}` - Get event by ID
- `PUT /api/events/{eventId}` - Update event
- `DELETE /api/events/{eventId}` - Delete event
- `POST /api/events/{eventId}/guests/{guestId}` - Add guest to event
- `DELETE /api/events/{eventId}/guests/{guestId}` - Remove guest from event
- `POST /api/events/{eventId}/guests` - Add multiple guests to event (body: guestIds)
- `DELETE /api/events/{eventId}/guests` - Remove multiple guests from event (body: guestIds)

### Wedding Users
- `GET /api/weddings/{weddingId}/users` - Get wedding users
- `POST /api/weddings/{weddingId}/users` - Add user to wedding
- `DELETE /api/weddings/{weddingId}/users/{userId}` - Remove user

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user

## Development Workflow

### Local Development

**Requirements:**
- .NET 10 SDK
- Docker & Docker Compose
- PostgreSQL 15+

**Start locally:**
```bash
# With Docker
docker-compose up -d

# Or run .NET directly
dotnet restore
dotnet build
dotnet run --project WeddingManager.Web
```

**API available at:** http://localhost:8080

### Testing
```bash
dotnet test
dotnet test WeddingManager.Application.Tests
dotnet test /p:CollectCoverage=true
```

### Deployment

**Automatic:** Push to `main` triggers GitHub Actions workflow:
1. Build Docker image (multi-stage)
2. Push to Scaleway Container Registry
3. SSH to VPS
4. Pull and restart containers
5. Health check verification

**Manual deployment on VPS:**
```bash
cd /app
docker-compose pull
docker-compose up -d api
docker-compose logs api
```

## Environment Variables

### Required for Production
```
SCW_SECRET_KEY                  # Scaleway API secret
SCW_DEFAULT_ORGANIZATION_ID     # Scaleway organization ID
SCW_DEFAULT_PROJECT_ID          # Scaleway project ID
SCW_REGION                      # Default: fr-par
```

### ASP.NET Core
```
ASPNETCORE_ENVIRONMENT          # Development, Staging, or Production
ASPNETCORE_URLS                 # Default: http://+:8080
```

### Database
```
DatabaseSettings__ConnectionString # PostgreSQL connection string
```

## Subscription Limits

Subscription limits are configured in appsettings under `SubscriptionPlans`. Tiers map to enum values:
- Free
- Starter
- Pro

Each tier has limits:
- MaxGuests
- MaxEvents
- MaxEmailsPerMonth

Use `-1` for unlimited. Email usage is tracked per user per month in `SubscriptionUsages`.

## Docker Setup

### Multi-stage Dockerfile
- **Stage 1:** Build with mcr.microsoft.com/dotnet/sdk:10.0
- **Stage 2:** Runtime with mcr.microsoft.com/dotnet/aspnet:10.0

### Docker Compose Services
- `api` - .NET app on port 8080
- `frontend` - React app on port 80
- `postgres` - Database on port 5432
- `nginx` - Reverse proxy on ports 80/443

**Health checks configured:**
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

## Key Dependencies

- Microsoft.EntityFrameworkCore - ORM
- Scaleway SDK - Cloud integration
- Serilog - Structured logging
- FluentValidation - Input validation
- MediatR - CQRS pattern (if used)

## Security Practices

- ✅ Secrets stored in GitHub Secrets (never in code)
- ✅ Environment variable injection
- ✅ HTTPS enforced (Let's Encrypt)
- ✅ API behind nginx reverse proxy
- ✅ Database on private network
- ✅ SSH key-based deployment

## Troubleshooting

### View logs
```bash
docker-compose logs -f api
docker-compose logs --tail=100 api
```

### Check health
```bash
curl http://localhost:8080/health
curl https://amare.wedding/api/health
```

### Database connection test
```bash
./Scripts/update-db.ps1
# Or on macOS/Linux
./Scripts/update-db.sh

psql postgresql://user:password@postgres:5432/wedding_manager -c "SELECT 1"
```

### Restart services
```bash
docker-compose down
docker-compose pull
docker-compose up -d
```

## Git Workflow

**Branches:**
- `main` - Production (auto-deploys)
- `develop` - Staging (auto-deploys)
- `feature/*` - New features
- `fix/*` - Bug fixes
- `chore/*` - Maintenance

**Recent commits:**
- cfbd89d - Update README API endpoints
- 1f4bd83 - Optimize GitHub Actions triggers
- 3f5b307 - Expand README documentation
- 5a975ce - Inject Scaleway credentials in workflow

## Conventions

### Commit Messages
- Clear, imperative mood
- Reference issue numbers when applicable

### Code Style
- Follow .NET conventions
- Use async/await for I/O operations
- Keep endpoints focused and minimal
- Validate input with FluentValidation

### File Organization
- Endpoints in WeddingManager.Web
- Business logic in Application layer
- Data access in Infrastructure layer
- Models/entities in Domain layer

## Important Notes for AI

1. **This is a .NET 10 project** - Use latest C# features and .NET APIs
2. **Minimal APIs** - Not MVC Controllers
3. **Clean Architecture** - Respect layer boundaries
4. **Docker-first** - Everything runs in containers
5. **Production system** - Changes deploy automatically to main branch
6. **PostgreSQL** - Not SQL Server
7. **Scaleway** - Not AWS/Azure/GCP

## Helpful Commands

```bash
# Build
dotnet build

# Run locally
dotnet run --project WeddingManager.Web

# Run tests
dotnet test

# Database migrations (if using EF migrations)
dotnet ef migrations add MigrationName --project WeddingManager.Infrastructure
dotnet ef database update --project WeddingManager.Infrastructure

# Scripts
./Scripts/add-migration.ps1 -Name MigrationName
./Scripts/update-db.ps1

# Docker
docker-compose up -d
docker-compose logs -f api
docker-compose ps

# View running containers
docker ps

# Clean Docker
docker system prune -a
```

## Project Status

**Version:** 1.0.0
**Status:** Production Ready ✅
**Last Updated:** January 24, 2026

## Next Steps

1. Stop any running `WeddingManager.Web` process to avoid file locks.
2. Add and apply the subscription limits migration:
   - `./Scripts/add-migration.ps1 -Name AddSubscriptionLimits`
   - `./Scripts/update-db.ps1`
3. Decide Stripe plan mapping and update `User.SubscriptionTier` on upgrade.
4. Add Stripe checkout + webhook endpoints to manage tier upgrades.

---

*This file helps AI assistants understand the project context, architecture, and conventions. Keep it updated as the project evolves.*
