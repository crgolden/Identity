# Admin UI — E2E Test Guide

Captures what was manually spot-checked during browser verification and what a proper E2E test suite should cover for each admin section. Use this as the specification when adding `Category=E2E` tests to `Identity.Tests.E2E/Pages/Admin/`.

---

## Infrastructure Requirements

### Test database seeding

Unlike the existing E2E tests (which use real user flows to create data), admin tests need pre-seeded IdentityServer configuration entities. The `IdentityWebApplicationFactory` should expose a `SeedAdminDataAsync()` method that inserts known records via `IConfigurationDbContext` and `IPersistedGrantDbContext` before the test suite runs, and removes them in `DisposeAsync`.

Suggested seed entities:
```csharp
// Client
new Client { ClientId = "e2e-test-client", ClientName = "E2E Test Client", AllowedScopes = [...] }

// ApiResource
new ApiResource { Name = "e2e-api", DisplayName = "E2E API" }

// ApiScope
new ApiScope { Name = "e2e-scope", DisplayName = "E2E Scope" }

// IdentityResource
new IdentityResource { Name = "e2e-profile", DisplayName = "E2E Profile" }
```

### Admin user requirement

Tests that exercise admin pages must run as a user with the `Admin` role. Options:
1. Seed an admin-role user in `PlaywrightFixture` (separate from the smoke-test user).
2. Assign the `Admin` role to the existing E2E test user (`TestEmail` from User Secrets) for the duration of the suite and remove it in `DisposeAsync`.

### Page Object Model

Each admin section should have a Page Object class under `Identity.Tests.E2E/Pages/Admin/PageObjects/` with typed helpers for Index, Create, Delete, Details, and Edit flows. Pattern from the existing E2E tests (`LoginPage`, `ManagePage`, etc.) applies directly.

---

## Verification Scenarios

### Auth / Access Control

| # | Scenario | How to verify | Proper E2E approach |
|---|---|---|---|
| A1 | Unauthenticated request to `/Admin` redirects to login | Navigate `/Admin` with no session; assert URL contains `/Account/Login` | `new BrowserContext()` (no cookies) → `page.GotoAsync("/Admin")` → `Expect(page).ToHaveURLAsync(*/Login*)` |
| A2 | Authenticated non-Admin user gets 403 at `/Admin` | Log in as a user without Admin role; navigate `/Admin` | Seed a non-Admin user; login; `GotoAsync("/Admin")` → `Expect(page).ToHaveURLAsync(*/AccessDenied*)` or status 403 |
| A3 | Admin nav link visible to Admin role | Log in as Admin; assert `a[href="/Admin"]` present in nav | `Expect(page.Locator("a[href='/Admin']")).ToBeVisibleAsync()` |
| A4 | Admin nav link absent for non-Admin | Log in as non-Admin; assert link absent | `Expect(page.Locator("a[href='/Admin']")).Not.ToBeVisibleAsync()` |

---

### Admin Landing Page (`/Admin`)

| # | Scenario | Assertions | Notes |
|---|---|---|---|
| L1 | Page renders with all section cards | Cards for: Clients, API Resources, API Scopes, Identity Resources, Identity Providers, SAML Service Providers, Persisted Grants, Device Flow Codes, Server-Side Sessions, Keys, Pushed Authorization Requests, SAML Sign-In States, SAML Logout Sessions, SAML Logout Session Request Indices, Users, Roles | 16 sections total |
| L2 | Each card "Manage" link navigates to correct Index | Click each card link; assert heading on destination page | Can be done as a single parameterized test |

---

### Clients (`/Admin/Clients`)

| # | Scenario | Assertions | Notes |
|---|---|---|---|
| C1 | Index lists seeded client | Table row contains `e2e-test-client` | Requires seeded data |
| C2 | Create — valid data → Details | Fill ClientId + ClientName; submit; assert redirect to Details showing the new client | Assert `ClientId` appears in Details heading or dl |
| C3 | Create — empty ClientId → stays on page | Submit empty form; assert validation error | |
| C4 | Details/Index shows client fields | Navigate to Details; assert ClientId, ClientName dl rows | |
| C5 | Edit/Index — update ClientName → persists | Change ClientName; save; revisit Details; assert updated value | |
| C6 | Edit/Scopes — add scope → persists | Add a scope row; save; revisit Details/Scopes; assert new scope in table | Tests JS row add + POST |
| C7 | Edit/Scopes — remove scope → persists | Post with no rows; revisit Details/Scopes; assert table empty | Tests JS row remove + POST |
| C8 | Edit/Scopes — update scope → persists | Change scope value on existing row; save; revisit; assert updated value | Tests update-existing diff path |
| C9 | Edit/Secrets — add secret → persists | Add secret row (Value + Type); save; revisit Details/Secrets; assert row appears (Value is hashed, not shown) | |
| C10 | Delete — removes client | Navigate to Delete; confirm; assert client no longer in Index | Cleanup test — should run last |
| *Repeat C6–C8 for:* | Claims, CorsOrigins, GrantTypes, IdPRestrictions, PostLogoutRedirectUris, Properties, RedirectUris | Same pattern | 7 collection sub-properties × 3 scenarios = 21 additional tests (Scopes + Secrets, covered above, bring the Client child-collection total to 9) |

---

### API Resources (`/Admin/ApiResources`)

| # | Scenario | Assertions |
|---|---|---|
| AR1 | Index lists seeded resource | Row contains `e2e-api` |
| AR2 | Create → Details | |
| AR3 | Edit/Scopes add/remove/update | Same 3-case pattern as Clients |
| AR4 | Edit/Secrets add → Details/Secrets shows row | |
| AR5 | Delete removes resource | |

---

### API Scopes (`/Admin/ApiScopes`)

| # | Scenario | Assertions |
|---|---|---|
| AS1 | Index, Create, Delete | Standard CRUD |
| AS2 | Edit/ClaimTypes add/remove/update | |
| AS3 | Edit/Properties add/remove/update | |

---

### Identity Resources (`/Admin/IdentityResources`)

Same shape as API Scopes: Index, Create, Delete, Edit/ClaimTypes, Edit/Properties.

---

### Identity Providers (`/Admin/IdentityProviders`)

| # | Scenario | Assertions |
|---|---|---|
| IP1 | Index loads (empty table OK) | Table visible, no JS error |
| IP2 | Create → Details | Flat form — Scheme, Type, DisplayName, Enabled |
| IP3 | Edit → updated values in Details | |
| IP4 | Delete removes entry | |

---

### SAML Service Providers (`/Admin/SamlServiceProviders`)

Same shape as Identity Providers.

---

### Read-Only / View+Delete Sections

Applies to: PersistedGrants, DeviceFlowCodes, ServerSideSessions, PushedAuthorizationRequests, SamlSigninStates, SamlLogoutSessions.

| # | Scenario | Assertions |
|---|---|---|
| RO1 | Index loads with table | Table visible |
| RO2 | Details shows record fields | dl rows present |
| RO3 | Delete removes record | Record absent from Index after confirm |

For **Keys** and **SamlLogoutSessionRequestIndices** (read-only, no Delete): Index + Details only.

---

### Users (`/Admin/Users`)

| # | Scenario | Assertions |
|---|---|---|
| U1 | Index lists the admin user | Row with `crgolden@msn.com` |
| U2 | Details/Index shows user fields | UserName, Email, EmailConfirmed, LockoutEnabled |
| U3 | Details/Claims shows claims | Table; no JS error |
| U4 | Details/Roles shows `Admin` role | Row contains "Admin" |
| U5 | Details/Logins shows external logins | Table (may be empty) |
| U6 | Details/Passkeys shows passkeys | Table (may be empty; depends on test user) |
| U7 | Edit/Index — update PhoneNumber → persists | Change PhoneNumber; save; revisit Details/Index; assert updated |
| U8 | Edit/Claims add/remove | Add a claim; save; revisit Details/Claims; assert row present. Remove; assert gone |
| U9 | Edit/Roles — add/remove role | Add a role; save; revisit Details/Roles |
| U10 | Edit/Logins — shows list; Remove button present | |
| U11 | Edit/Passkeys — shows list; Remove button present | |

---

### Roles (`/Admin/Roles`)

| # | Scenario | Assertions |
|---|---|---|
| R1 | Index lists `Admin` role | Row present |
| R2 | Create role → Details | New role name in Details |
| R3 | Details/Claims shows claims | Table |
| R4 | Details/Users shows users in role | Admin user row |
| R5 | Edit/Index — rename role → persists | |
| R6 | Edit/Claims add/remove | |
| R7 | Delete role → gone from Index | Use created-in-R2 role, not Admin |

---

## Gaps / Caveats

- **Collection JS (admin-collection.js)**: The add/remove row interactions depend on JavaScript. Playwright tests must `WaitForResponseAsync` or `WaitForSelectorAsync` after clicking Add/Remove before submitting, since rows are DOM-manipulated client-side.
- **Passkey tests**: Cannot automate WebAuthn registration (hardware authenticator). Tests for Details/Passkeys and Edit/Passkeys should assert the UI renders correctly for a user with no passkeys (empty table, "no passkeys" message) and skip the remove path.
- **Secret values**: Edit/Secrets never shows the raw value — only Description, Type, Expiration. Tests must not assert the `Value` field after save; assert Description instead.
- **SAML tables**: PersistedGrantDb SAML tables (SamlSigninStates, SamlLogoutSessions, etc.) are empty in development. Tests for these should assert the table is present even when empty, and skip the Delete/Details test unless data is seeded via the grant store directly.
- **Non-Admin 403**: Requires a second E2E user without the Admin role. Add `E2E_NON_ADMIN_EMAIL` + `E2E_NON_ADMIN_PASSWORD` to CI variables, or seed the user in the factory.

---

## Total Estimated E2E Test Methods

| Section | Approx. tests |
|---|---|
| Auth / Access Control | 4 |
| Landing Page | 2 |
| Clients (CRUD + 9 collections × 3) | ~35 |
| ApiResources | ~15 |
| ApiScopes | ~8 |
| IdentityResources | ~8 |
| IdentityProviders | ~6 |
| SamlServiceProviders | ~6 |
| Read-only sections (6 × 3 + 2 × 2) | ~22 |
| Users | ~12 |
| Roles | ~8 |
| **Total** | **~126** |
