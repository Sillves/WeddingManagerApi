# Amare.Wedding API (Vowly)

Wedding management SaaS platform API built with .NET 10, ASP.NET Core, and PostgreSQL.

## What's in the Name?

**Amare** (Ah-mah-re) — *The Italian verb "to love."*

It is globally recognized and deeply personal. We chose this name because wedding planning is ultimately about celebrating love. From the moment couples start planning to the final vow, everything centers on expressing and honoring their love.

*Typography: Brittany signature, Allura, Sophia Script*

## Overview

Amare.Wedding is a comprehensive wedding planning and management platform that enables couples to create beautiful wedding websites, manage guest lists, collect RSVPs, and coordinate with vendors.

**Live:** https://amare.wedding

## Tech Stack

- **Runtime:** .NET 10
- **Framework:** ASP.NET Core 10
- **Database:** PostgreSQL
- **ORM:** Entity Framework Core
- **Cloud:** Scaleway (Container Registry, VPS)
- **CI/CD:** GitHub Actions
- **Infrastructure:** Docker, Docker Compose, Nginx

## Project Structure

```
WeddingManagerApi/
├── WeddingManager.Web/          # API endpoints (Minimal APIs)
├── WeddingManager.Application/  # Business logic & services
├── WeddingManager.Infrastructure/ # Database & external services
├── WeddingManager.Domain/       # Domain models & entities
├── Dockerfile                   # Multi-stage Docker build
└── .github/workflows/           # CI/CD pipelines
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Docker & Docker Compose
- PostgreSQL 15+
- Node.js 20+ (for frontend development)

### Local Development

1. **Clone the repository**
```bash
git clone https://github.com/Sillves/WeddingManagerApi.git
cd WeddingManagerApi
```

2. **Setup environment variables**
```bash
# Create .env file
cat > .env << 'EOF'
# Database
DatabaseSettings__ConnectionString=Host=localhost;Database=WeddingDb;Username=postgres;Password=postgres

# Scaleway
SCW_SECRET_KEY=your_scaleway_secret_key
SCW_DEFAULT_ORGANIZATION_ID=your_org_id
SCW_DEFAULT_PROJECT_ID=your_project_id
SCW_REGION=fr-par

# ASP.NET
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
EOF
```

3. **Build and run**
```bash
# With Docker
docker-compose up -d

# Or locally
dotnet restore
dotnet build
dotnet run --project WeddingManager.Web
```

4. **Access the API**
- API: http://localhost:8080
- Health Check: http://localhost:8080/health
- OpenAPI/Swagger: http://localhost:8080/swagger (if enabled)

## API Endpoints

### Health Check
```bash
GET /health
# Returns: "Healthy"
```

### Weddings
```bash
GET    /api/weddings              # Get all weddings for user
POST   /api/weddings              # Create new wedding
DELETE /api/weddings/{id}         # Delete wedding
POST   /api/weddings/{id}/rsvp    # Submit RSVP
```

### Guests
```bash
GET    /api/weddings/{weddingId}/guests # Get guests for wedding
POST   /api/weddings/{weddingId}/guests # Add guest to wedding
GET    /api/guests/{guestId}            # Get guest by id
PUT    /api/guests/{guestId}            # Update guest
DELETE /api/guests/{guestId}            # Remove guest
```

### Events
```bash
GET    /api/events                       # Get all events
GET    /api/weddings/{weddingId}/events  # Get events for wedding
POST   /api/weddings/{weddingId}/events  # Create event
GET    /api/events/{eventId}             # Get event by id
PUT    /api/events/{eventId}             # Update event
DELETE /api/events/{eventId}             # Delete event
POST   /api/events/{eventId}/guests/{guestId} # Add guest to event
DELETE /api/events/{eventId}/guests/{guestId} # Remove guest from event
POST   /api/events/{eventId}/guests      # Add multiple guests to event (body: guestIds)
DELETE /api/events/{eventId}/guests      # Remove multiple guests from event (body: guestIds)
```

### Wedding Users
```bash
GET    /api/weddings/{weddingId}/users   # Get wedding users
POST   /api/weddings/{weddingId}/users   # Add user to wedding
DELETE /api/weddings/{weddingId}/users/{userId} # Remove user from wedding
```

### Authentication
```bash
POST   /api/auth/register         # Register new user
POST   /api/auth/login            # Login user
```

## Deployment

### Production Deployment

The API automatically deploys to production when you push to the `main` branch.

**CI/CD Pipeline:**
1. GitHub Actions checks out code
2. Builds Docker image with multi-stage build
3. Pushes to Scaleway Container Registry
4. SSHs into VPS
5. Pulls latest image
6. Restarts containers with `docker-compose up -d`
7. Health checks verify API is running

**Deployment Flow:**
```
git push origin main
  ↓
GitHub Actions triggered
  ↓
Build Docker image
  ↓
Push to rg.fr-par.scw.cloud/amare-wedding/api:latest
  ↓
Wait 15 seconds (registry propagation)
  ↓
SSH to VPS: docker-compose pull && docker-compose up -d
  ↓
Verify health check: curl /health
  ↓
✅ Live!
```

### Manual Deployment

If you need to deploy manually:

```bash
# On your local machine
docker build -t rg.fr-par.scw.cloud/amare-wedding/api:latest .
docker push rg.fr-par.scw.cloud/amare-wedding/api:latest

# On VPS
cd /app
docker-compose pull
docker-compose up -d api
docker-compose ps
docker-compose logs api
```

## Docker

### Dockerfile

Multi-stage build for optimal image size:
```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WeddingManager.Web.dll"]
```

### Docker Compose

Running locally with PostgreSQL:
```bash
docker-compose up -d
```

Services:
- **api**: .NET application on port 8080
- **frontend**: React app on port 80
- **postgres**: Database on port 5432
- **nginx**: Reverse proxy on ports 80/443

### Health Checks

All containers have health checks configured:
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

## Environment Variables

### Required for Production

```
SCW_SECRET_KEY              # Scaleway API secret
SCW_DEFAULT_ORGANIZATION_ID # Scaleway organization ID
SCW_DEFAULT_PROJECT_ID      # Scaleway project ID
SCW_REGION                  # Scaleway region (default: fr-par)
```

### ASP.NET Core

```
ASPNETCORE_ENVIRONMENT      # Development, Staging, or Production
ASPNETCORE_URLS             # Binding URL (default: http://+:8080)
```

### Database

```
DatabaseSettings__ConnectionString # PostgreSQL connection string
```

## Monitoring & Logging

### Docker Logs

```bash
# View API logs
docker-compose logs -f api

# View all logs
docker-compose logs -f

# View last 100 lines
docker-compose logs --tail=100 api
```

### Log Rotation

Logs are automatically rotated to prevent disk fill:
- Max file size: 10MB
- Max files: 3
- Driver: json-file

### Health Monitoring

Check application health:
```bash
# Local
curl http://localhost:8080/health

# Production
curl https://amare.wedding/api/health

# With retry logic (via nginx)
curl -v https://amare.wedding/health
```

## Troubleshooting

### API won't start

1. Check if port 8080 is available
2. Verify environment variables are set
3. Check database connection string
4. View logs: `docker-compose logs api`

### Database connection errors

```bash
# Update database schema locally
./Scripts/update-db.ps1
# Or on macOS/Linux
./Scripts/update-db.sh

# Test PostgreSQL connection
docker-compose exec api dotnet

# Or check connection from VPS
psql postgresql://user:password@postgres:5432/wedding_manager -c "SELECT 1"
```

### Deployment failed

```bash
# SSH to VPS and check
ssh root@amare-wedding
cd /app

# Check containers
docker-compose ps

# Check logs
docker-compose logs api

# Restart manually
docker-compose down
docker-compose pull
docker-compose up -d
```

### High disk usage

```bash
# Check disk usage
df -h

# Clean up Docker
docker system prune -a

# View log sizes
docker exec amare-api du -sh /var/log
```

## Security

### Best Practices

- ✅ Secrets never stored in code (GitHub Secrets)
- ✅ Secrets injected via environment variables
- ✅ HTTPS enforced (Let's Encrypt)
- ✅ API only accessible through nginx proxy
- ✅ Database on private network
- ✅ SSH key-based authentication for deployments

### Rotating Secrets

1. Update secret in GitHub repo Settings
2. Re-run workflow or push new commit
3. New containers start with new secret

## Scaling

### Horizontal Scaling

To run multiple API instances:

1. Update docker-compose.yml to run multiple api services
2. Load balance with nginx upstream

```yaml
upstream api_backend {
    server api-1:8080;
    server api-2:8080;
    server api-3:8080;
}
```

### Vertical Scaling

Upgrade VPS resources:
1. Resize instance on Scaleway
2. Restart containers
3. Automatic recovery

## CI/CD Pipeline Details

### Build Job

- Checks out code
- Sets up Docker Buildx
- Logs into Scaleway registry
- Builds image with metadata
- Pushes to registry with tags:
    - `main` (branch name)
    - `latest` (if main branch)
    - `main-abc1234` (commit SHA)

### Deploy Job

- Waits 15 seconds for registry propagation
- SSHes to VPS with retry logic
- Pulls latest image (3 retry attempts, 10 sec delay)
- Restarts container with `docker-compose up -d`
- Verifies health check
- Shows logs

**Triggered:** On push to `main` or `develop` branches

## Dependencies

### NuGet Packages

Key dependencies (see .csproj files):

- **Microsoft.EntityFrameworkCore**: ORM for database access
- **Scaleway SDK**: Cloud service integration
- **Serilog**: Structured logging
- **FluentValidation**: Input validation
- **MediatR**: CQRS pattern (optional)

### Docker Images

- `mcr.microsoft.com/dotnet/sdk:10.0` - Build stage
- `mcr.microsoft.com/dotnet/aspnet:10.0` - Runtime stage
- `nginx:alpine` - Reverse proxy
- `PostgreSQL-17` - Database

## Contributing

1. Create feature branch: `git checkout -b feature/amazing-feature`
2. Commit changes: `git commit -m 'Add amazing feature'`
3. Push to branch: `git push origin feature/amazing-feature`
4. Open Pull Request

**Branch naming:**
- `feature/*` - New features
- `fix/*` - Bug fixes
- `chore/*` - Maintenance tasks

## Testing

```bash
# Run tests
dotnet test

# Run specific test project
dotnet test WeddingManager.Application.Tests

# With coverage
dotnet test /p:CollectCoverage=true
```

## License

Proprietary - Amare.Wedding

## Support

For issues and questions:
- Create GitHub issue
- Check CI/CD logs in GitHub Actions
- SSH to VPS and inspect logs

---

**Last Updated:** January 24, 2026  
**Current Version:** 1.0.0  
**Status:** Production Ready ✅
