# Claude.md - Amare.Wedding API Context

This document provides AI assistants with structured context about the Amare.Wedding API project so they don't need to re-read source files.

## Project Overview

**Name:** Amare.Wedding
**Type:** Wedding management SaaS platform API
**Live URL:** https://amare.wedding
**Repo:** https://github.com/Sillves/WeddingManagerApi

Amare (Ah-mah-re) is Italian for "to love" - chosen because wedding planning is about celebrating love.

## Tech Stack

- **Runtime:** .NET 10
- **Framework:** ASP.NET Core 10 (Minimal APIs, not Controllers)
- **Database:** PostgreSQL 17+
- **ORM:** Entity Framework Core
- **Auth:** ASP.NET Identity + JWT Bearer
- **Payments:** Stripe.net
- **Object Storage:** Scaleway S3-compatible
- **Email:** SMTP
- **Mapping:** Riok.Mapperly (source-generated, compile-time)
- **Cloud:** Scaleway (Container Registry, VPS)
- **CI/CD:** GitHub Actions (auto-deploy on push to main)

## Architecture

Clean architecture with four layers:

```
WeddingManager.Web/            # Minimal API endpoints, authorization, middleware
WeddingManager.Application/    # Business services, validation, mappings
WeddingManager.Infrastructure/ # EF Core repos, SMTP, Scaleway, Identity
WeddingManager.Domain/         # Entities, DTOs, interfaces, enums, models
WeddingManager.Tests/          # xUnit + Moq unit tests
```

---

## Domain Layer

### Entities

**User** (extends IdentityUser<Guid>):
- `FirstName`, `LastName` (string)
- `SubscriptionTier` (enum: Free=0, Starter=1, Pro=2)
- `StripeCustomerId`, `StripeSubscriptionId` (string?)

**Wedding**:
- `Id` (Guid), `Title`, `Slug`, `Location` (string), `Date` (DateTime)
- `UserId` (Guid), `User` (nav)
- Collections: `Guests`, `Events`, `Expenses`, `WeddingUsers`, `Pages`, `Media`
- `Website` (WeddingWebsite?), `Budget` (WeddingBudget?)

**Guest**:
- `Id` (Guid), `Name`, `Email` (string), `PreferredLanguage` (string, default "en")
- `RsvpStatus` (enum: Pending=0, Accepted=1, Declined=2, Maybe=3)
- `InvitationToken` (string?), `InvitationTokenExpiresAt` (DateTime?), `InvitationSentAt` (DateTime?)
- `WeddingId` (Guid), `Wedding` (nav), `Events` (ICollection<Event>)

**Event**:
- `Id` (Guid), `Name`, `Description`, `Location` (string)
- `StartDate` (DateTime), `EndDate` (DateTime?)
- `WeddingId` (Guid), `Wedding` (nav), `Guests` (ICollection<Guest>)

**WeddingExpense**:
- `Id` (Guid), `WeddingId` (Guid), `Amount` (decimal)
- `Category` (enum: Venue=0, Catering=1, Photography=2, Decoration=3, Attire=4, Transport=5, Other=6)
- `Description` (string), `Date` (DateTime), `Notes` (string?)
- `CreatedAt`, `UpdatedAt` (DateTime), `Wedding` (nav)

**WeddingWebsite**:
- `Id` (Guid), `WeddingId` (Guid), `Wedding` (nav)
- `Template` (enum: ElegantClassic=0, ModernMinimal=1, RomanticGarden=2)
- `Settings`, `Content` (string, stored as JSON)
- `IsPublished` (bool), `PublishedAt` (DateTime?), `MetaDescription` (string?)
- `CreatedAt`, `UpdatedAt` (DateTime)

**WeddingBudget**:
- `Id` (Guid), `WeddingId` (Guid), `TotalBudget` (decimal)
- `Allocations` (ICollection<BudgetAllocation>)

**BudgetAllocation**:
- `Id` (Guid), `WeddingBudgetId` (Guid), `Category` (ExpenseCategory), `Amount` (decimal)

**Media**:
- `Id` (Guid), `WeddingId` (Guid), `FileName`, `S3Url`, `ContentType` (string), `Size` (long)

**SubscriptionUsage**:
- `Id` (Guid), `UserId` (Guid), `Year` (int), `Month` (int), `EmailsSent` (int)

### Result Types

```csharp
public class Result {
    bool IsSuccess;
    IReadOnlyList<Error> Errors;
    static Result Ok();
    static Result Fail(Error error);
    static Result Fail(IEnumerable<Error> errors);
}
public class Result<T> {
    bool IsSuccess;
    T? Value;
    IReadOnlyList<Error> Errors;
    static Result<T> Ok(T value);
    static Result<T> Fail(Error error);
    static Result<T> Fail(IEnumerable<Error> errors);
}
public sealed record Error(string Code, string Message);
```

### Error Codes

```csharp
ErrorCodes.Validation       // "validation"
ErrorCodes.NotFound         // "not_found"
ErrorCodes.Unauthorized     // "unauthorized"
ErrorCodes.Forbidden        // "forbidden"
ErrorCodes.Conflict         // "conflict"
ErrorCodes.LimitExceeded    // "limit_exceeded"
ErrorCodes.ExternalFailure  // "external_failure"
ErrorCodes.AccountLocked    // "account_locked"
ErrorCodes.Unexpected       // "unexpected"
```

### Key DTOs

**Guest DTOs:** `CreateGuestRequestDto` (Name, Email, RsvpStatus, PreferredLanguage?), `UpdateGuestRequestDto` (same), `RsvpSubmitRequestDto` (Name, Email, RsvpStatus), `GuestDto` (Id, Name, Email, RsvpStatus, PreferredLanguage, InvitationSentAt?, WeddingId)

**Expense DTOs:** `CreateWeddingExpenseRequestDto` (Amount, Category, Description, Date, Notes?), `UpdateWeddingExpenseRequestDto` (same), `WeddingExpenseDto` (adds Id, WeddingId, CreatedAt, UpdatedAt), `WeddingExpenseSummaryDto` (TotalAmount, CategoryTotals dict, Expenses list)

**Website DTOs:** `CreateWeddingWebsiteRequestDto` (Template), `UpdateWeddingWebsiteRequestDto` (Template?, Settings? JsonDocument, Content? JsonDocument, MetaDescription?), `WeddingWebsiteDto` (Id, WeddingId, WeddingSlug, Template, Settings JsonDocument, Content JsonDocument, IsPublished, PublishedAt?, MetaDescription?, CreatedAt, UpdatedAt), `PublicWeddingWebsiteDto` (WeddingSlug, CoupleNames, WeddingDate, WeddingLocation, Template, Settings, Content, Events? list)

**Event DTOs:** `CreateEventRequestDto` (Name, Description, Location, StartDate, EndDate?), `UpdateEventRequestDto` (same), `EventDto` (Id, WeddingId, Name, Description, StartDate, EndDate?, Location, GuestDtos list)

**Auth:** `AuthResult` (Success bool, Message, Token string)

**Billing:** `BillingPlanDto`, `InvitationSendResultDto` (SentCount, FailedCount, FailedGuestIds list), `BulkImportGuestResultDto` (CreatedCount, SkippedCount, ErrorCount, Errors list, CreatedGuests list)

### Repository Interfaces

**IGuestRepository**: `GetByIdAsync(Guid)`, `GetByWeddingIdAsync(Guid)`, `GetByIdsAsync(Guid weddingId, IEnumerable<Guid> guestIds)`, `GetByEmailAsync(Guid weddingId, string email)`, `CountByWeddingIdAsync(Guid)`, `AddAsync(Guest)`, `AddRangeAsync(IEnumerable<Guest>)`, `GetEmailsByWeddingIdAsync(Guid)`, `GetExistingEmailsAsync(Guid, IEnumerable<string>)`, `UpdateAsync(Guest)`, `DeleteAsync(Guid)`

**IWeddingRepository**: `GetAllAsync(Guid userId)`, `GetByIdAsync(Guid)`, `GetByIdOrSlugAsync(string)`, `GetWeddingsWithMediaOlderThanAsync(DateTime)`, `AddAsync`, `UpdateAsync`, `DeleteAsync`

**IWeddingExpenseRepository**: `GetByIdAsync(Guid)`, `GetByWeddingIdAsync(Guid)`, `GetByWeddingIdAndCategoryAsync(Guid, ExpenseCategory)`, `GetCategoryTotalsAsync(Guid)` → Dict, `GetTotalAmountAsync(Guid)` → decimal, `AddAsync`, `UpdateAsync`, `DeleteAsync`

**IWeddingWebsiteRepository**: `GetByIdAsync(Guid)`, `GetByWeddingIdAsync(Guid)`, `GetPublishedBySlugAsync(string)`, `AddAsync`, `UpdateAsync`, `DeleteAsync`

**IEventRepository**: `GetByIdAsync(Guid)`, `GetByWeddingIdAsync(Guid)`, `AddAsync`, `UpdateAsync`, `DeleteAsync`

**IEmailService**: `SendRsvpConfirmationAsync(Guest, Wedding)`, `SendInvitationAsync(Guest, Wedding)`, `SendPasswordResetAsync(string email, string resetLink, string language)`

**ISubscriptionLimitService**: `EnsureGuestLimitAsync(Guid weddingId, int count = 1)`, `EnsureEmailLimitAsync(Guid weddingId, int count)`, `RecordEmailsSentAsync(Guid userId, int count)`

---

## Application Layer Services

### AuthService
**Constructor:** `UserManager<User>`, `IOptions<JwtSettings>`, `IOptions<FrontendSettings>`, `IEmailService`, `ILogger<AuthService>`

- `RegisterAsync(email, firstName, lastName, password)` → `Result<AuthResult>`
- `LoginAsync(email, password)` → `Result<AuthResult>` — checks lockout, wrong password calls `AccessFailedAsync`, success calls `ResetAccessFailedCountAsync`
- `RequestPasswordResetAsync(email, language)` → `Result` — returns Ok even if user not found (no leak), sends email via `IEmailService`
- `ResetPasswordAsync(email, token, newPassword)` → `Result`
- `ChangePasswordAsync(userId, currentPassword, newPassword)` → `Result`

### GuestService
**Constructor:** `IGuestRepository`, `IWeddingRepository`, `IEmailService`, `ISubscriptionLimitService`, `ApplicationMapper`, `ILogger<GuestService>`

- `GetByIdAsync(guestId)` → `Result<GuestDto>`
- `GetByWeddingIdAsync(weddingId)` → `Result<IEnumerable<GuestDto>>`
- `CreateGuestAsync(weddingId, dto)` → validates → checks limit → checks duplicate email → adds
- `UpdateGuestAsync(guestId, dto)` → finds → validates → checks duplicate email if changed → updates
- `DeleteGuestAsync(guestId)` → finds → deletes
- `SubmitRsvpAsync(weddingId, dto)` → validates → finds by email → updates name + status
- `SendInvitationAsync(weddingId, guestId)` → checks email limit → ensures token → sends email → records usage
- `SendInvitationsAsync(weddingId, guestIds?)` → batch send, tracks sent/failed counts
- `ImportGuestsAsync(weddingId, dto)` → bulk import up to 500, validates each, skips duplicates
- `CheckExistingEmailsAsync(weddingId, emails)` → returns matching emails

### WeddingService
**Constructor:** `IWeddingRepository`, `IUserContextService`

- `GetByIdAsync(id)` → `Result<Wedding>` (returns entity, not DTO)
- `GetByIdOrSlugAsync(idOrSlug)` → `Result<Wedding>`
- `GetAllAsync()` → uses `userContextService.GetUserId()` to scope
- `AddAsync(wedding)` → generates new Id, Slug from title, sets UserId if empty
- `UpdateAsync(wedding)` / `DeleteAsync(id)` → delegates to repo

### WeddingExpenseService
**Constructor:** `IWeddingExpenseRepository`, `IWeddingRepository`, `ApplicationMapper`, `ILogger<WeddingExpenseService>`

- `GetByIdAsync(expenseId)` → `Result<WeddingExpenseDto>`
- `GetByWeddingIdAsync(weddingId)` / `GetByWeddingIdAndCategoryAsync(weddingId, category)`
- `GetSummaryAsync(weddingId)` → returns totals, category breakdown, expense list
- `CreateExpenseAsync(weddingId, dto)` → validates → checks wedding exists → adds
- `UpdateExpenseAsync(expenseId, dto)` → validates → finds → updates
- `DeleteExpenseAsync(expenseId)` → finds → deletes

### WeddingWebsiteService
**Constructor:** `IWeddingWebsiteRepository`, `IWeddingRepository`, `IEventRepository`, `ApplicationMapper`

- `GetByWeddingIdAsync(weddingId)` → `Result<WeddingWebsiteDto>`
- `CreateAsync(userId, weddingId, dto)` → checks wedding exists → checks date set → checks subscription tier (Free forbidden) → checks no existing → creates with default content/settings
- `UpdateAsync(weddingId, dto)` → finds → updates template/settings/content/metaDescription if provided
- `PublishAsync(weddingId)` / `UnpublishAsync(weddingId)` → sets IsPublished flag
- `GetPublicBySlugAsync(slug)` → `Result<PublicWeddingWebsiteDto>` — includes events if content JSON has `events.enabled=true` and `events.showFromWeddingEvents=true`
- `DeleteAsync(weddingId)` → finds → deletes

### EventService
**Constructor:** `IEventRepository`, `IGuestRepository`, `ISubscriptionLimitService`, `ApplicationMapper`, `ILogger<EventService>`

- CRUD operations for events
- `AddGuestToEventAsync` / `RemoveGuestFromEventAsync` — single guest
- `AddGuestsToEventAsync` / `RemoveGuestsFromEventAsync` — batch operations

### BillingService
**Constructor:** heavy Stripe dependency — `IOptions<StripeSettings>`, `IOptions<SubscriptionPlanOptions>`, `UserManager<User>`, `ILogger<BillingService>`

- `GetPlansAsync()`, `CreateCheckoutSessionAsync()`, `CreatePortalSessionAsync()`, `HandleWebhookAsync()`, `ChangePlanAsync()`

### Validation Classes (static)

**GuestValidation**: `ValidateInput(CreateGuestRequestDto)`, `ValidateInput(UpdateGuestRequestDto)`, `ValidateInput(RsvpSubmitRequestDto)`, `ValidateImportItem(BulkImportGuestItemDto)`
- Name: required (not whitespace)
- Email: required + valid format (System.Net.Mail.MailAddress)
- PreferredLanguage: must be "en", "nl", or "fr" (null/empty = ok, defaults to "en")
- `IsValidEmail(string)` and `IsSupportedLanguage(string?)` are `internal` helpers

**ExpenseValidation**: `ValidateInput(CreateWeddingExpenseRequestDto)`, `ValidateInput(UpdateWeddingExpenseRequestDto)`
- Amount: must be > 0
- Description: required, max 500 chars
- Notes: optional, max 1000 chars

**EventValidation**: `ValidateInput(CreateEventRequestDto)`, `ValidateInput(UpdateEventRequestDto)`
- Name: required, Location: required, StartDate: not default
- EndDate: if provided, must be >= StartDate

**BudgetValidation**: `ValidateInput(UpsertWeddingBudgetRequestDto)`
- TotalBudget > 0, allocations not negative, no duplicate categories

### ApplicationMapper (Mapperly)

Source-generated at compile time. Use `new ApplicationMapper()` directly (no DI needed).

Key methods: `GuestToDto`, `GuestsToDto`, `ExpenseToDto`, `ExpensesToDto`, `ExpensesToListDto`, `EventToDto` (manual, maps Guests to GuestDtos), `WebsiteToDto` (manual, parses JSON strings to JsonDocument), `WeddingToDto`, `WeddingToPublicDto`, `BudgetToDto`

---

## Testing

### Framework & Conventions
- **xUnit** with `[Fact]` and `[Theory]`/`[InlineData]`
- **Moq** for mocking interfaces
- **AAA pattern** (Arrange-Act-Assert)
- **Naming:** `MethodName_Scenario` (e.g., `LoginAsync_ReturnsAccountLockedWhenLockedOut`)
- **Mapper:** use `new ApplicationMapper()` directly (Mapperly has no dependencies)
- **InternalsVisibleTo:** Application project exposes internals to Tests project

### Test Project Structure
```
WeddingManager.Tests/
├── AuthServiceTests.cs           # 16 tests - 100% coverage
├── GuestServiceTests.cs          # 21 tests - 57.9% coverage
├── GuestValidationTests.cs       # 15 tests - 77.4% coverage
├── WeddingServiceTests.cs        # 9 tests  - 100% coverage
├── WeddingExpenseServiceTests.cs # 19 tests - 100% coverage
├── ExpenseValidationTests.cs     # 12 tests - 100% coverage
├── WeddingWebsiteServiceTests.cs # 22 tests - 93.9% coverage
├── EventServiceTests.cs          # existing tests
├── EventValidationTests.cs       # existing tests
├── BillingServiceTests.cs        # existing tests
├── SubscriptionLimitServiceTests.cs
├── WeddingBudgetServiceTests.cs
├── BudgetValidationTests.cs
├── WeddingUserServiceTests.cs
├── EventRepositoryTests.cs
└── MappingProfileTests.cs
```

### CreateService Helper Pattern
Each test class has a private `CreateService` method with optional mock parameters:
```csharp
private GuestService CreateService(
    Mock<IGuestRepository>? guestRepositoryMock = null,
    Mock<IWeddingRepository>? weddingRepositoryMock = null, ...)
{
    guestRepositoryMock ??= new Mock<IGuestRepository>();
    ...
    return new GuestService(guestRepositoryMock.Object, ..., Mock.Of<ILogger<GuestService>>());
}
```

### UserManager Mock Pattern
```csharp
private static Mock<UserManager<User>> CreateUserManager()
{
    var store = new Mock<IUserStore<User>>();
    return new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
}
```

### Running Tests
```bash
dotnet test WeddingManager.Tests/WeddingManager.Tests.csproj
dotnet test WeddingManager.Tests/WeddingManager.Tests.csproj --collect:"XPlat Code Coverage"
# Generate report (ReportGenerator installed as global tool)
"$HOME/.dotnet/tools/reportgenerator" "-reports:TestResults/**/coverage.cobertura.xml" "-targetdir:TestResults/Report" "-reporttypes:TextSummary"
```

### Coverage Status (February 2026, 183 tests)

| Layer | Coverage |
|-------|----------|
| **Application** | **73.2%** |
| **Domain** | **67.7%** |
| **Infrastructure** | **3.2%** (repos need integration tests) |
| **Overall line** | **16.5%** |
| **Overall method** | **57.7%** |
| **Overall branch** | **51%** |

**100% covered services:** AuthService, WeddingService, WeddingExpenseService, WeddingBudgetService, WeddingUserService, ExpenseValidation, EventValidation, BudgetValidation

**Gaps:** GuestService 57.9% (ImportGuestsAsync untested), EventService 60.7% (bulk guest ops), BillingService 38.4% (Stripe), GuestValidation 77.4% (ValidateImportItem), WeddingWebsiteService 93.9% (private helpers)

---

## API Endpoints

### Auth
- `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`
- `POST /api/auth/forgot-password`, `POST /api/auth/reset-password`, `POST /api/auth/change-password`

### Weddings
- `GET/POST /api/weddings`, `GET/PUT/DELETE /api/weddings/{id}`
- `GET /api/weddings/{id}/public`

### Guests
- `GET/POST /api/weddings/{weddingId}/guests`
- `GET/PUT/DELETE /api/guests/{id}`
- `POST /api/weddings/{weddingId}/guests/send-invitations`
- `POST /api/weddings/{weddingId}/guests/import`
- `POST /api/weddings/{weddingId}/guests/check-emails`

### Events
- `GET/POST /api/weddings/{weddingId}/events`
- `GET/PUT/DELETE /api/events/{id}`
- `POST/DELETE /api/events/{id}/guests/{guestId}` (single)
- `POST/DELETE /api/events/{id}/guests` (batch)

### Expenses
- `GET/POST /api/weddings/{weddingId}/expenses`
- `GET /api/weddings/{weddingId}/expenses/summary`
- `GET/PUT/DELETE /api/expenses/{id}`

### Website
- `GET/POST/PUT/DELETE /api/weddings/{weddingId}/website`
- `POST /api/weddings/{weddingId}/website/publish`
- `POST /api/weddings/{weddingId}/website/unpublish`
- `GET /api/website/{slug}` (public)

### Media
- `POST /api/weddings/{weddingId}/media`, `GET /api/weddings/{weddingId}/media`
- `GET /api/media/{id}`, `DELETE /api/weddings/{weddingId}/media/{id}`

### Billing
- `GET /api/billing/plans`, `POST /api/billing/checkout-session`
- `POST /api/billing/portal-session`, `POST /api/billing/change-plan`
- `POST /api/billing/webhook`

### RSVP
- `POST /api/weddings/{weddingId}/rsvp` (public)

### Health
- `GET /health`

---

## Environment & Deployment

### Local Development
```bash
docker compose up -d                           # Start PostgreSQL + MinIO
dotnet run --project WeddingManager.Web        # API on http://localhost:5072
```

### Environment Variables
- `DatabaseSettings__ConnectionString` — PostgreSQL
- `JwtSettings__Key/Issuer/Audience` — JWT config (Key must be 32+ chars)
- `FrontendSettings__BaseUrl` — for password reset links
- `SmtpSettings__Host/Port/FromEmail` — SMTP
- `StripeSettings__SecretKey/WebhookSecret` — Stripe
- `ScalewayStorageSettings__*` — object storage

### Database Migrations
```bash
./Scripts/add-migration.ps1 MigrationName
./Scripts/update-db.ps1
```

### Deployment
Push to `main` → GitHub Actions → Docker build → Scaleway Container Registry → SSH deploy to VPS → health check

---

## Subscription Tiers

| Feature | Free | Starter | Pro |
|---------|------|---------|-----|
| Guests | 50 | 200 | Unlimited (-1) |
| Events | 5 | 20 | Unlimited |
| Emails/month | 0 | 300 | Unlimited |
| Website Builder | No | Yes | Yes |

---

**Last Updated:** February 2026
