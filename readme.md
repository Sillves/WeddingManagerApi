# Amare.Wedding API

Wedding management SaaS platform API built with .NET 10, ASP.NET Core, and PostgreSQL.

**"Amare"** (Ah-mah-re) — *The Italian verb "to love."* We chose this name because wedding planning is ultimately about celebrating love.

**Live:** https://amare.wedding

---

## Tech Stack

- **Runtime:** .NET 10
- **Framework:** ASP.NET Core 10 (Minimal APIs)
- **Database:** PostgreSQL 17
- **ORM:** Entity Framework Core
- **Authentication:** JWT Bearer tokens
- **Payments:** Stripe.net
- **Object Storage:** Scaleway
- **Email:** SMTP
- **Cloud:** Scaleway (Container Registry, VPS)
- **CI/CD:** GitHub Actions

---

## Project Structure

```
WeddingManagerApi/
├── WeddingManager.Domain/          # Entities, DTOs, interfaces, enums
├── WeddingManager.Application/     # Business services, validation
├── WeddingManager.Infrastructure/  # Repositories, EF Core, SMTP, external services
├── WeddingManager.Web/             # API endpoints, authorization, middleware
├── Scripts/                        # Migration & deployment scripts
├── Dockerfile                      # Multi-stage Docker build
└── .github/workflows/              # CI/CD pipelines
```

---

## Complete Feature List

### Authentication & User Management
- User registration with email/password
- User login with JWT token generation
- Current user profile retrieval (includes subscription tier)
- Password change functionality
- Forgot password flow (email with reset link)
- Password reset with token validation
- JWT claims: Sub, Email, GivenName, FamilyName, Jti

### Wedding Management
- Create, read, update, delete weddings
- Wedding details: Title, Slug, Date, Location
- Get weddings by authenticated user
- Public wedding info endpoint (for unauthenticated RSVP pages)
- Wedding ownership validation

### Guest Management
- Full CRUD operations for guests
- Guest details: Name, Email, RSVP Status, Preferred Language
- RSVP status options: Pending, Accepted, Declined, Maybe
- Invitation token system (32 bytes, base64-encoded, 30-day expiration)
- Single invitation sending via email
- Batch invitation sending (all pending or specific guest IDs)
- Email limit checking before sending
- Invitation sent timestamp tracking

### Event Management
- Full CRUD operations for events
- Event details: Name, Description, StartDate, EndDate, Location
- Many-to-many relationship with guests
- Add/remove single guests to/from events
- Batch add/remove guests with detailed result tracking
- Event limit enforcement per subscription tier
- Change result tracking (Added, Removed, AlreadyExists, NotInEvent)

### Expense Tracking
- Full CRUD operations for expenses
- Expense details: Amount, Category, Description, Date, Notes
- Expense categories:
  - Venue
  - Catering
  - Photography
  - Decoration
  - Attire
  - Transport
  - Other
- Expense summary with category totals
- Total amount calculation

### Wedding Website Builder
- Create, read, update, delete websites
- 3 templates: ElegantClassic, ModernMinimal, RomanticGarden
- Multi-section content:
  - Hero section (couple names, wedding date/location)
  - Story section (couple narrative)
  - Details section (venue, date, dress code)
  - Events section (can pull from wedding events)
  - Gallery section (photo uploads)
  - RSVP section (customizable deadline)
  - Footer section (contact, social links)
- Publish/unpublish functionality with timestamp
- Public website retrieval by slug
- Meta description for SEO
- Subscription tier restriction (Starter/Pro only)
- Wedding date validation before creation

### Media Management
- Upload images to Scaleway object storage
- Allowed file types: JPEG, JPG, PNG, GIF, WebP
- Max file size: 5 MB
- Get media by wedding
- Stream media file retrieval
- Delete media with storage cleanup
- Media proxy URLs for serving through API

### RSVP System
- Public RSVP endpoint (no authentication required)
- Guest lookup by email and name
- RSVP status update
- RSVP confirmation email sending
- Invitation token validation

### Billing & Subscriptions
- 3 subscription tiers:
  - **Free**: 50 guests, 5 events, 0 emails/month
  - **Starter**: 200 guests, 20 events, 300 emails/month, Website builder
  - **Pro**: Unlimited (-1), All features
- Billing intervals: Monthly, Annual, Lifetime
- Stripe Checkout session creation
- Stripe Billing Portal session creation
- Plan upgrades/downgrades with prorations
- Webhook handling for 6 event types:
  - `checkout.session.completed`
  - `invoice.paid`
  - `invoice.payment_failed`
  - `customer.subscription.updated`
  - `customer.subscription.deleted`
- Automatic downgrade to Free on payment failure
- Promotion codes support in checkout

### Collaboration
- WeddingUser entity for collaboration management
- Roles: Owner, Planner, Guest
- Add/remove collaborators to weddings
- AddedAt timestamp tracking

### Email System
- SMTP integration with authentication
- Multilingual support (Dutch, English, French)
- HTML email templating
- Invitation emails with RSVP links
- RSVP confirmation emails
- Password reset emails
- Email quota tracking per user per month

### Subscription Limits
- Guest limit enforcement per tier
- Event limit enforcement per tier
- Monthly email limit enforcement per tier
- Email usage tracking and recording
- Unlimited limits support (when limit is -1)

---

## API Endpoints

### Health Check
```
GET /health                         # Returns "Healthy"
```

### Authentication (6 endpoints)
```
POST /api/auth/register             # Register new user
POST /api/auth/login                # Login, returns JWT token
GET  /api/auth/me                   # Get current user profile
POST /api/auth/forgot-password      # Request password reset email
POST /api/auth/reset-password       # Reset password with token
POST /api/auth/change-password      # Change password (requires current)
```

### Weddings (6 endpoints)
```
GET    /api/weddings                # Get all weddings for user
POST   /api/weddings                # Create new wedding
GET    /api/weddings/{id}           # Get specific wedding
PUT    /api/weddings/{id}           # Update wedding
DELETE /api/weddings/{id}           # Delete wedding
GET    /api/weddings/{id}/public    # Get public wedding info
```

### Guests (7 endpoints)
```
GET    /api/weddings/{weddingId}/guests              # Get all guests
POST   /api/weddings/{weddingId}/guests              # Create guest
GET    /api/guests/{guestId}                         # Get specific guest
PUT    /api/guests/{guestId}                         # Update guest
DELETE /api/guests/{guestId}                         # Delete guest
POST   /api/weddings/{weddingId}/guests/send-invitation   # Send single invitation
POST   /api/weddings/{weddingId}/guests/send-invitations  # Batch send invitations
```

### RSVP (1 endpoint)
```
POST /api/weddings/{weddingId}/rsvp                  # Submit RSVP (public)
```

### Events (8 endpoints)
```
GET    /api/events                                   # Get all events
GET    /api/weddings/{weddingId}/events              # Get events for wedding
POST   /api/weddings/{weddingId}/events              # Create event
GET    /api/events/{eventId}                         # Get specific event
PUT    /api/events/{eventId}                         # Update event
DELETE /api/events/{eventId}                         # Delete event
POST   /api/events/{eventId}/guests/{guestId}        # Add guest to event
DELETE /api/events/{eventId}/guests/{guestId}        # Remove guest from event
POST   /api/events/{eventId}/guests                  # Batch add guests
DELETE /api/events/{eventId}/guests                  # Batch remove guests
```

### Expenses (6 endpoints)
```
GET    /api/weddings/{weddingId}/expenses            # Get all expenses
GET    /api/weddings/{weddingId}/expenses/summary    # Get expense summary
GET    /api/expenses/{expenseId}                     # Get specific expense
POST   /api/weddings/{weddingId}/expenses            # Create expense
PUT    /api/expenses/{expenseId}                     # Update expense
DELETE /api/expenses/{expenseId}                     # Delete expense
```

### Wedding Users (3 endpoints)
```
GET    /api/weddings/{weddingId}/users               # Get collaborators
POST   /api/weddings/{weddingId}/users               # Add collaborator
DELETE /api/weddings/{weddingId}/users/{userId}      # Remove collaborator
```

### Website Builder (7 endpoints)
```
GET    /api/weddings/{weddingId}/website             # Get website
POST   /api/weddings/{weddingId}/website             # Create website
PUT    /api/weddings/{weddingId}/website             # Update website
DELETE /api/weddings/{weddingId}/website             # Delete website
POST   /api/weddings/{weddingId}/website/publish     # Publish website
POST   /api/weddings/{weddingId}/website/unpublish   # Unpublish website
GET    /api/website/{slug}                           # Get published website (public)
```

### Media (4 endpoints)
```
POST   /api/weddings/{weddingId}/media               # Upload media file
GET    /api/weddings/{weddingId}/media               # Get all media
GET    /api/media/{mediaId}                          # Get/stream media file
DELETE /api/weddings/{weddingId}/media/{mediaId}     # Delete media
```

### Billing (5 endpoints)
```
GET  /api/billing/plans                              # Get subscription plans
POST /api/billing/checkout-session                   # Create Stripe Checkout
POST /api/billing/portal-session                     # Create Billing Portal
POST /api/billing/change-plan                        # Change subscription plan
POST /api/billing/webhook                            # Stripe webhook receiver
```

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- Docker & Docker Compose
- PostgreSQL 17+

### Local Development

1. **Clone the repository**
```bash
git clone https://github.com/Sillves/WeddingManagerApi.git
cd WeddingManagerApi
```

2. **Start services with Docker Compose**
```bash
docker-compose up -d
```

This starts:
- PostgreSQL database
- SMTP4dev (for local email testing)

3. **Run the API**
```bash
dotnet run --project WeddingManager.Web
```

4. **Access the API**
- API: http://localhost:5072
- Health Check: http://localhost:5072/health
- Swagger: http://localhost:5072/swagger

---

## Environment Variables

### Database
```
DatabaseSettings__ConnectionString=Host=localhost;Database=WeddingDb;Username=postgres;Password=postgres
```

### JWT Authentication
```
JwtSettings__Key=your-secret-key-min-32-chars
JwtSettings__Issuer=amare.wedding
JwtSettings__Audience=amare.wedding
JwtSettings__ExpirationInDays=30
```

### SMTP (Email)
```
SmtpSettings__Host=localhost
SmtpSettings__Port=1025
SmtpSettings__Username=
SmtpSettings__Password=
SmtpSettings__FromEmail=noreply@amare.wedding
SmtpSettings__FromName=Amare Wedding
```

### Stripe
```
StripeSettings__SecretKey=sk_test_...
StripeSettings__WebhookSecret=whsec_...
StripeSettings__SuccessUrl=https://amare.wedding/billing/success
StripeSettings__CancelUrl=https://amare.wedding/billing/cancel
StripeSettings__PortalReturnUrl=https://amare.wedding/profile
```

### Scaleway (Object Storage)
```
ScalewaySettings__AccessKey=...
ScalewaySettings__SecretKey=...
ScalewaySettings__BucketName=amare-wedding-media
ScalewaySettings__Region=fr-par
```

### ASP.NET Core
```
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5072
```

---

## Database Migrations

```bash
# Add migration (PowerShell)
./Scripts/add-migration.ps1 MigrationName

# Update database (PowerShell)
./Scripts/update-db.ps1

# Or on macOS/Linux
./Scripts/add-migration.sh MigrationName
./Scripts/update-db.sh
```

### Migration History
1. `InitialCreate` - Base schema
2. `AddGuestInvitations` - Invitation token system
3. `AddEventGuestManyToMany` - Event-guest relationship
4. `AddSubscriptionLimits` - Subscription tier tracking
5. `AddStripeBillingFields` - Stripe customer/subscription IDs
6. `AddWeddingExpenses` - Expense tracking
7. `AddWeddingWebsite` - Website builder

---

## Subscription Configuration

Configure in `appsettings.json`:

```json
"SubscriptionPlans": {
  "Plans": {
    "Free": {
      "MaxGuests": 50,
      "MaxEvents": 5,
      "MaxEmailsPerMonth": 0,
      "Features": []
    },
    "Starter": {
      "MaxGuests": 200,
      "MaxEvents": 20,
      "MaxEmailsPerMonth": 300,
      "Features": ["website_builder"]
    },
    "Pro": {
      "MaxGuests": -1,
      "MaxEvents": -1,
      "MaxEmailsPerMonth": -1,
      "Features": ["website_builder", "priority_support"]
    }
  }
}
```

Notes:
- `-1` means unlimited
- Email usage tracked per user per month in `SubscriptionUsages` table
- Limits enforced when creating guests/events and sending invitations

---

## Deployment

### Production Deployment

Automatic deployment on push to `main` branch via GitHub Actions.

**Pipeline:**
1. GitHub Actions checks out code
2. Builds Docker image with multi-stage build
3. Pushes to Scaleway Container Registry
4. SSHs into VPS
5. Pulls latest image
6. Restarts containers with `docker-compose up -d`
7. Verifies health check

### Manual Deployment

```bash
# Build and push
docker build -t rg.fr-par.scw.cloud/amare-wedding/api:latest .
docker push rg.fr-par.scw.cloud/amare-wedding/api:latest

# On VPS
cd /app
docker-compose pull
docker-compose up -d api
```

---

## Error Codes

Standardized error responses:
- `Validation` - Input validation failures
- `NotFound` - Resource not found
- `Unauthorized` - Authentication failed
- `Forbidden` - Authorization failed
- `Conflict` - Resource conflict (e.g., duplicate email)
- `LimitExceeded` - Subscription limit exceeded
- `ExternalFailure` - External service failures (Stripe, email, storage)
- `Unexpected` - Unexpected errors

---

## Security

- JWT Bearer token authentication on protected endpoints
- Wedding access filter for resource-level authorization
- Wedding ownership validation
- Invitation token-based RSVP access
- Subscription tier validation for features
- Secrets via environment variables (never in code)
- HTTPS enforced (Let's Encrypt)
- Database on private network

---

## Monitoring

### Docker Logs
```bash
docker-compose logs -f api          # View API logs
docker-compose logs --tail=100 api  # Last 100 lines
```

### Health Check
```bash
curl https://amare.wedding/api/health
```

---

## License

Proprietary - Amare.Wedding

---

**Last Updated:** February 2026
**Status:** Production Ready
