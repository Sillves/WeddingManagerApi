# Website-toegangscode & gepersonaliseerde content

**Datum:** 2026-07-01
**Status:** Goedgekeurd ontwerp
**Repos:** `WeddingManagerApi` (backend), `amare-wedding-frontend` (frontend)

## Probleem

Koppels willen dat een gast eerst een code invult vóór de gepubliceerde website
zichtbaar wordt. Op basis van die code wordt de content gepersonaliseerd (o.a. op
welke events de gast is uitgenodigd). Omdat de code dan al is ingegeven, mag hij niet
opnieuw gevraagd worden op de RSVP.

## Uitgangspunt: bestaande bouwstenen

De feature hergebruikt wat er al is; er komt géén nieuw code/beveiligingsconcept bij.

- **`InvitationFlow.Passcode`** — de code. `ValidateExclusivity`
  (`InvitationFlowService.cs:113`) dwingt al af: **ofwel** N flows die allemaal een
  passcode hebben, **ofwel** exact één open flow zonder passcode. Een mix is onmogelijk.
- **`RsvpFlowCookie`** (`RsvpFlowCookie.cs`) — gesigneerde, versleutelde, HttpOnly cookie
  (`weddingId | flowId | expiry`, 1 uur) via `IDataProtectionProvider`. Bewijst dat deze
  browser een geldige code heeft ingevoerd en bindt de gekozen flow. Er is geen
  server-side sessie; de cookie is de gedeelde sleutel tussen website en RSVP.
- **Unlock-endpoint** `POST /public/weddings/{slug}/rsvp/unlock` — valideert de passcode
  en zet de cookie. Blijft ongewijzigd en wordt hergebruikt vanuit de website-gate.

## Gedrag: poort volgt automatisch uit de flow-opzet

| Flow-situatie | Website | Events getoond |
|---|---|---|
| Geen flows | Publiek (ongewijzigd) | Alle events |
| Eén open flow (geen passcode) | Publiek | Gefilterd op die flow |
| Passcode-flows | **Code vereist vóór content** | Gefilterd op de ontgrendelde flow |

Er is dus **geen aparte toggle**: de poort staat aan zodra de bruiloft passcode-flows heeft.

## Backend

### 1. Website-endpoint wordt flow-aware

`GET /w/{slug}` (`GetPublicWebsiteEndpoint.cs`) injecteert `IDataProtectionProvider` en
haalt de flowId uit de cookie via `RsvpFlowCookie.ResolveFlowId`. Nieuwe respons-DTO:

```csharp
public class PublicWebsiteStateDto
{
    public bool RequiresPasscode { get; set; }
    public PublicWeddingWebsiteDto? Website { get; set; } // null wanneer vergrendeld
}
```

Nieuwe service-signatuur:
`GetPublicBySlugAsync(string slug, Guid? unlockedFlowId)` in `WeddingWebsiteService`.

Logica:
1. Website ophalen (`GetPublishedBySlugAsync`); niet gevonden → `NotFound` (ongewijzigd).
2. Flows van de wedding ophalen.
   - **Geen flows** → `{ RequiresPasscode = false, Website = <ongefilterd> }` (huidig gedrag).
   - **Open flow** → `{ RequiresPasscode = false, Website = <events gefilterd op open flow> }`.
   - **Passcode-flows** en `unlockedFlowId` is null of hoort niet bij deze wedding →
     `{ RequiresPasscode = true, Website = null }`. Content wordt niet geserialiseerd → geen lek.
   - **Passcode-flows** met geldige `unlockedFlowId` binnen de wedding →
     `{ RequiresPasscode = false, Website = <events gefilterd op die flow> }`.
3. **Event-filtering**: waar nu ongefilterd `GetByWeddingIdForPublicAsync` wordt getoond
   (events-sectie enabled), worden de events beperkt tot `flow.EventIds` van de relevante flow.

### 2. RSVP vult de code niet opnieuw

`GetRsvpFlowStateEndpoint.cs` resolvet óók de cookie en geeft `unlockedFlowId` door aan
`GetFlowStateAsync(string weddingSlugOrId, Guid? unlockedFlowId)`. Is die geldig en hoort
bij de wedding → bouw de flow direct (`RequiresPasscode = false, Flow = dto`), net als bij
een open flow. Een unlock via de website telt zo meteen voor de RSVP.

## Frontend

- **`usePublicWebsiteState`** — hook die de nieuwe state-respons ophaalt.
- **`WebsiteUnlockGate`** — component met code-invoer; hergebruikt `useUnlockRsvpFlow`.
- **`PublicWebsitePage`** — toont bij `requiresPasscode` de gate i.p.v. de template. Na
  geslaagde unlock: query invalideren en de site herladen (cookie zit er dan).
- **`RsvpPage`** — geen wijziging nodig: als er via de site al is ontgrendeld, geeft de
  flowstate nu direct het formulier terug.
- i18n: nieuwe strings voor de gate in NL/EN/FR (`website.json`).

## Edge cases

- Cookie verlopen/ongeldig → `ResolveFlowId` geeft `null` → poort verschijnt opnieuw.
- Cookie van flow A maar hoort bij wedding B → genegeerd (wedding-binding gecheckt) → poort dicht.
- Events verwijderd na unlock → bestaande filtering op geldige event-ids vangt dit.
- Ongepubliceerde site met flows → `/w/{slug}` geeft `NotFound` zoals nu (gate niet relevant).

## Tests (xUnit + Moq)

`WeddingWebsiteServiceTests`:
- `GetPublicBySlugAsync` geen flows → website ongefilterd, `RequiresPasscode=false`.
- open flow → events gefilterd op open flow, `RequiresPasscode=false`.
- passcode-flows + geen/ongeldige/andere-wedding flowId → `RequiresPasscode=true`, `Website=null`.
- passcode-flows + geldige flowId → events gefilterd op die flow, `RequiresPasscode=false`.

`RsvpServiceTests`:
- `GetFlowStateAsync` met geldige `unlockedFlowId` → directe flow, `RequiresPasscode=false`.
- met ongeldige/andere-wedding flowId → `RequiresPasscode=true`.

## Privacy-fix (meegenomen)

`EventToDto` nam `GuestDtos` mee op de publieke website (`GET /w/{slug}`) — een anoniem
endpoint. Dat lekte per event de volledige gastenlijst (naam, achternaam, e-mail,
dieetwensen, RSVP-status) in de JSON-respons. `BuildPublicWebsiteDtoAsync` leegt nu
`GuestDtos` voor elk publiek event; test `GetPublicBySlugAsync_NeverExposesGuestList`
dekt dit af. De publieke templates gebruiken enkel naam/datum/locatie/beschrijving.
