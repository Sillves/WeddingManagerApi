# Plan: Unify events + passcode-gate the public website

Real wedding data (user's brother). Careful, reversible-where-possible steps.

## Decision summary
- **One source of truth for events.** Drop flow-local `CustomEvents` (jsonb) + `CustomEventDefinition`. A flow only holds `EventIds` referencing real wedding `Event` rows. "Add a custom event on an invitation" = create a real wedding event and reference it (frontend does this via the existing events API, then selects it).
- **Whole website gated by passcode.** No valid session -> gate (see nothing). Correct passcode -> 1h cookie -> site shows only that flow's events. Reuses `RsvpFlowCookie` + existing unlock endpoint.
- **Gate only applies to weddings that have passcoded flows.** Weddings with no flows stay fully public (multi-tenant SaaS safety).

## Backend (WeddingManagerApi)

### 1. Model / data
- `InvitationFlow.cs`: remove `CustomEvents`.
- Delete `WeddingManager.Domain/Models/CustomEventDefinition.cs`.
- `RsvpResponse.cs`: update `AttendingEventIds` comment (real event ids only).
- `InvitationFlowConfiguration.cs`: remove the `CustomEvents` jsonb mapping.
- DTOs `CreateInvitationFlowRequestDto` / `UpdateInvitationFlowRequestDto`: remove `CustomEvents`.

### 2. Services
- `InvitationFlowService`: drop `NormalizeCustomEvents` + all CustomEvents handling in Create/Update; keep EventIds validation.
- `RsvpService.BuildPublicFlowAsync` / `GetValidEventIdsAsync`: remove custom-event merge; just filter real events by `flow.EventIds`.

### 3. Migration `UnifyFlowEvents_DropCustomEvents`
Generate via `dotnet ef` AFTER code compiles, then hand-edit `Up()` to run BEFORE the DropColumn:
- For each flow with non-empty `CustomEvents`, INSERT an `Event` reusing `ce.id` as `Event.Id`
  (StartDate = coalesce(ce.startDate, wedding.Date); Location = coalesce(ce.location, '')).
- Append those ids into the flow's `EventIds` (dedup).
- `AttendingEventIds` need no change (ids reused).
- Then DropColumn `CustomEvents`.
- `Down()`: re-add empty `CustomEvents` jsonb column (data not restored — acceptable).
- **Before running on prod: back up DB, test on a copy.** Prod applies migrations explicitly (Program.cs), dev auto-migrates.

### 4. Website gate
- `WeddingWebsiteService.GetPublicBySlugAsync(slug, Guid? flowId)`:
  - no flows -> full site, all events (unchanged public behavior).
  - open flow (passcode null) -> events filtered to that flow.
  - only passcoded flows + no/invalid flowId -> return locked (`RequiresPasscode = true`, no website).
  - valid flowId belonging to this wedding -> events filtered to that flow's EventIds.
- Response DTO wrapper: `{ requiresPasscode: bool, website: PublicWeddingWebsiteDto? }`, always HTTP 200 (avoid the 401->/login interceptor).
- `GetPublicWebsiteEndpoint`: inject `HttpContext` + `IDataProtectionProvider`, resolve flowId via `RsvpFlowCookie.ResolveFlowId`, pass to service.
- Website unlock reuses `POST /public/weddings/{slug}/rsvp/unlock` (sets the shared cookie).

## Frontend (amare-wedding-frontend)
- `websiteApi.getPublicBySlug`: switch from authed `apiClient` to a `withCredentials` public client; return the new wrapper shape.
- `PublicWebsitePage`: if `requiresPasscode` -> render passcode gate (reuse RsvpPage gate UI); on unlock success, refetch.
- Flow editor: replace the custom-events UI with multi-select of existing wedding events + "create new event" (calls events API, then selects it).
- Details section builder: "Wedding Events" option now = the unlocked invitation's events; keep "Static Details" as-is.

## Order of work
1. Backend code changes (compile-clean). 2. Generate + hand-edit migration. 3. Backend website gate. 4. Frontend. 5. Test on DB copy, back up, then deploy.

## Status (2026-07-02)
- [x] Backend unify (entity/config/DTOs/mapper/validation/services)
- [x] Migration `20260702110810_UnifyFlowEvents_PromoteCustomEventsToRealEvents` — data promotion tested on a throwaway Postgres 17 (promote, name-collision merge, cross-flow dedupe, response remap all correct)
- [x] Website gate (service + endpoint + `PublicWebsiteResponseDto`, reuses `RsvpFlowCookie`)
- [x] Backend build clean + 238/238 tests pass (added 3 gating tests)
- [x] Frontend: `getPublicBySlug` on credentialed client + wrapper shape; `PublicWebsitePage` passcode gate; flow editor now creates real wedding events; i18n keys added (en/nl/fr); `vite build` passes
- [ ] DEPLOY: back up prod DB, dry-run migration on a copy, then deploy. Run `npm run sync-types` after the API is live to regenerate `src/types/api.ts` (still lists the removed `customEvents`, currently harmless/optional).
