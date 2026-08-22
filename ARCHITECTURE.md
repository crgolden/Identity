# Architecture

## Purpose

This application is a standalone **OpenID Connect Identity Provider** (IdP). Client applications redirect users here to authenticate, and this app issues tokens that clients use to verify identity. It does not host any application logic beyond authentication and account management.

Built on:
- **Duende IdentityServer 8** — authorization server (OIDC/OAuth2 token issuance)
- **ASP.NET Core Identity** — user store, password hashing, 2FA, passkeys
- **SQL Server** — persistence via EF Core (`ApplicationDbContext`)

---

## Solution Structure

| Project | Type | Role |
|---|---|---|
| `Identity/` | ASP.NET Core 10 web app | The running application — Razor Pages, services, `Program.cs`, `ApplicationDbContext` |
| `Identity.Data/` | SQL Server Database Project (SSDT) | Authoritative schema source; builds to a `.dacpac` for production deployment |
| `Identity.Tests.Unit/` | xUnit v3 test project | Unit and property-based tests |
| `Identity.Tests.E2E/` | xUnit v3 test project | E2E (Playwright/Chromium), load, and smoke tests |

---

## URL & Routing Conventions

All Razor Pages require authentication by default (`MapRazorPages().RequireAuthorization()`). Pages that must be reachable before login explicitly opt out with `[AllowAnonymous]`.

`.AddDefaultUI()` is deliberately **not** called on the `AddIdentity<...>()` chain in `Program.cs` — this app has its own complete, branded replacement for the entire scaffolded Identity UI page set (everything under `Pages/Account/*` below, including `Manage/*`). Calling `.AddDefaultUI()` would register a second, unstyled `/Identity/Account/*` route tree and — because it also unconditionally repoints the `ApplicationScheme` cookie's `LoginPath`/`LogoutPath`/`AccessDeniedPath` to those scaffolded pages — silently hijack every framework-driven auth challenge and access-denied redirect away from the branded pages below. Framework-driven redirects (an unauthenticated hit on a protected page, or an authenticated-but-unauthorized hit on `/Admin/**`) land on this app's own `/Account/Login` and `/Account/AccessDenied` precisely because `CookieAuthenticationOptions`'s built-in defaults already match this app's page routes — no `ConfigureApplicationCookie()` override needed, as long as `.AddDefaultUI()` stays out of the chain.

### Tier 1 — Root: public, non-account pages

Pages here are informational or handle system-level errors. They carry no user context and are always `[AllowAnonymous]`.

| URL | Purpose |
|---|---|
| `/` | Home / landing page |
| `/Privacy` | Privacy policy |
| `/Error` | Error display (also the IdentityServer `ErrorUrl`) |

### Tier 2 — `/Account`: unauthenticated account flows

Everything a user does before they are logged in. All pages are `[AllowAnonymous]`.

**Registration & email confirmation**

| URL | Purpose |
|---|---|
| `/Account/Register` | Email + password registration |
| `/Account/RegisterConfirmation` | Post-registration instructions page |
| `/Account/ConfirmEmail` | Processes the email confirmation token |
| `/Account/ResendEmailConfirmation` | Resend confirmation link |
| `/Account/ConfirmEmailChange` | Processes an email-change confirmation token |

**Login**

| URL | Purpose |
|---|---|
| `/Account/Login` | Primary login: local credentials, Google OIDC button, passkey button |
| `/Account/LoginWith2fa` | TOTP second-factor step |
| `/Account/LoginWithRecoveryCode` | Recovery-code fallback for locked-out 2FA |
| `/Account/ExternalLogin` | Callback handler for external providers (Google) |
| `/Account/Lockout` | Displayed when the account is locked |
| `/Account/AccessDenied` | Displayed when an authenticated user lacks permission |
| `/Account/Logout` | Signs the user out (also the IdentityServer `LogoutUrl`) |

**Password recovery**

| URL | Purpose |
|---|---|
| `/Account/ForgotPassword` | Initiates the password-reset email |
| `/Account/ForgotPasswordConfirmation` | Confirms the reset email was sent |
| `/Account/ResetPassword` | Accepts the reset token and new password |
| `/Account/ResetPasswordConfirmation` | Confirms reset success |

**Minimal API endpoints (passkey WebAuthn, also under `/Account`)**

| Method + URL | Purpose |
|---|---|
| `POST /Account/PasskeyCreationOptions` | Returns WebAuthn credential-creation options JSON (antiforgery required; authenticated) |
| `POST /Account/PasskeyRequestOptions` | Returns WebAuthn assertion-request options JSON (antiforgery required; `?username=` optional) |

These are registered as minimal API endpoints in `EndpointRouteBuilderExtensions.cs`, not as Razor Pages.

### Tier 3 — `/Account/Manage`: authenticated account management

All pages here require an active session. There is no `[Authorize]` attribute on each page individually — global `RequireAuthorization()` applies, and none of these pages opt out.

**Profile & credentials**

| URL | Purpose |
|---|---|
| `/Account/Manage` | Dashboard: avatar, username, phone number |
| `/Account/Manage/Email` | Change or confirm a new email address |
| `/Account/Manage/ChangePassword` | Change password (when one exists) |
| `/Account/Manage/SetPassword` | Set a password for the first time (external-login users) |
| `/Account/Manage/ExternalLogins` | Link or unlink Google (or other) external login providers |

**Two-factor authentication (TOTP)**

| URL | Purpose |
|---|---|
| `/Account/Manage/TwoFactorAuthentication` | 2FA overview and status |
| `/Account/Manage/EnableAuthenticator` | QR-code setup for an authenticator app |
| `/Account/Manage/Disable2fa` | Remove TOTP 2FA |
| `/Account/Manage/ResetAuthenticator` | Invalidate the current TOTP secret and start over |
| `/Account/Manage/GenerateRecoveryCodes` | Generate a fresh set of one-time recovery codes |
| `/Account/Manage/ShowRecoveryCodes` | Display newly generated recovery codes |

**Passkeys (WebAuthn)**

| URL | Purpose |
|---|---|
| `/Account/Manage/Passkeys` | List, add, and delete passkeys |
| `/Account/Manage/RenamePasskey` | Rename an existing passkey |

**Personal data (GDPR)**

| URL | Purpose |
|---|---|
| `/Account/Manage/PersonalData` | Overview page linking to download and delete |
| `/Account/Manage/DownloadPersonalData` | Export all stored personal data as JSON |
| `/Account/Manage/DeletePersonalData` | Irreversibly delete the account and all its data |

### Tier 4 — `/Admin`: role-gated admin UI

All pages under `/Admin` require the `"Admin"` role. Authorization is applied at the folder level via `AuthorizeFolder("/Admin", "Admin")` — no per-page `[Authorize]` attribute. Non-admin users receive 403.

| Section | URL prefix | Backing interface | Notes |
|---|---|---|---|
| Landing | `/Admin` | — | Card grid linking to all sections |
| Clients | `/Admin/Clients` | `IConfigurationDbContext` | 9 collection sub-properties |
| API Resources | `/Admin/ApiResources` | `IConfigurationDbContext` | 4 collection sub-properties |
| API Scopes | `/Admin/ApiScopes` | `IConfigurationDbContext` | 2 collection sub-properties |
| Identity Resources | `/Admin/IdentityResources` | `IConfigurationDbContext` | 2 collection sub-properties |
| Identity Providers | `/Admin/IdentityProviders` | `IConfigurationDbContext` | Flat edit (dynamic OIDC providers) |
| SAML Service Providers | `/Admin/SamlServiceProviders` | `IConfigurationDbContext` | Flat edit — see SAML note below |
| Persisted Grants | `/Admin/PersistedGrants` | `IPersistedGrantDbContext` | View + delete only |
| Device Flow Codes | `/Admin/DeviceFlowCodes` | `IPersistedGrantDbContext` | View + delete only |
| Server-Side Sessions | `/Admin/ServerSideSessions` | `IPersistedGrantDbContext` | View + delete only |
| Keys | `/Admin/Keys` | `IPersistedGrantDbContext` | Read-only |
| Pushed Authorization Requests | `/Admin/PushedAuthorizationRequests` | `IPersistedGrantDbContext` | View + delete only |
| SAML Sign-In States | `/Admin/SamlSigninStates` | `IPersistedGrantDbContext` | View + delete only |
| SAML Logout Sessions | `/Admin/SamlLogoutSessions` | `IPersistedGrantDbContext` | View + delete only |
| SAML Logout Request Indices | `/Admin/SamlLogoutSessionRequestIndices` | `IPersistedGrantDbContext` | Read-only |
| Users | `/Admin/Users` | `UserManager<IdentityUser<Guid>>` | Claims, Roles, Logins, Passkeys sub-pages |
| Roles | `/Admin/Roles` | `RoleManager<IdentityRole<Guid>>` | Claims, Users sub-pages |

#### SAML is storage and administration only

**Identity does not serve the SAML protocol.** It is not a SAML identity provider, and no application signs in through it that way.

The four SAML entity types ship with `Duende.IdentityServer.EntityFramework`, so their tables exist in the schema and the admin UI can read and write them. The protocol handler is a separate licensed Duende plugin that is not referenced by `Identity.csproj` and not registered in `Program.cs`. Nothing consumes these rows at runtime.

The admin pages are therefore inert configuration editors. Records created there have no effect. Standing up SAML would mean adding the plugin package, registering it, and exposing its endpoints — none of which is in place.

### IdentityServer flow pages

These pages participate in the OAuth2/OIDC protocol flows. They use `[Authorize]` or `[AllowAnonymous]` explicitly. Security headers are applied globally by middleware, not per page — see [Security headers](#security-headers).

| URL | Auth | Purpose |
|---|---|---|
| `/Account/Manage/Consent` | `[Authorize]` | OAuth2 scope approval — allow/deny, optional remember |
| `/Account/Manage/Grants` | `[Authorize]` | View and revoke previously granted client permissions |
| `/Account/Manage/Device` | `[Authorize]` | Device authorization flow — user code entry + scope consent |
| `/Account/Manage/DeviceSuccess` | `[AllowAnonymous]` | Device flow success confirmation |
| `/Ciba` | `[AllowAnonymous]` | CIBA backchannel login request display |
| `/Account/Manage/ServerSideSessions` | `[Authorize]` | View and revoke active server-side sessions |
| `/Account/Manage/Diagnostics` | `[Authorize]` | Current user claims/tokens (loopback only, development nav link) |
| `/Redirect` | `[AllowAnonymous]` | Intermediate loading page for native-client protocol redirects |

### Infrastructure endpoints

| URL | Mechanism | Purpose |
|---|---|---|
| `/health` | `MapHealthChecks` | `ApplicationDbContext` connectivity check; HTTP metrics disabled |
| Static files | `MapStaticAssets` | CSS, JS, images |

---

## Authentication Flows

### Local username/password

1. User visits `/Account/Login`, submits email and password.
2. `SignInManager.PasswordSignInAsync` validates credentials.
3. If 2FA is enabled, redirect to `/Account/LoginWith2fa` (TOTP) or `/Account/LoginWithRecoveryCode`.
4. On success, IdentityServer issues the appropriate token and redirects back to the client.

Email confirmation is required (`RequireConfirmedAccount = true`). Unconfirmed users are redirected to `/Account/RegisterConfirmation`.

### Google OpenID Connect

1. User clicks the Google button on `/Account/Login`.
2. ASP.NET Core redirects to Google, using `GoogleOpenIdConnectDefaults.AuthenticationScheme` with `SignInScheme = IdentityConstants.ExternalScheme`. `AddGoogleOpenIdConnect` (`Google.Apis.Auth.AspNetCore3`) requests scopes `openid email profile` by default.
3. Google redirects back to `/Account/ExternalLogin`. The returned `ClaimsPrincipal` carries these claims (verified live against a real Google account):

   | Claim type | Example value |
   |---|---|
   | `ClaimTypes.NameIdentifier` (`.../identity/claims/nameidentifier`) | Google's numeric `sub` |
   | `ClaimTypes.Email` (`.../identity/claims/emailaddress`) | the account's primary email |
   | `email_verified` | `true` |
   | `name` | full display name |
   | `picture` | avatar URL |
   | `ClaimTypes.GivenName` (`.../identity/claims/givenname`) | first name |
   | `ClaimTypes.Surname` (`.../identity/claims/surname`) | last name |

4. Because the email claim is always present and verified for Google, `ExternalLoginModel.OnGetCallbackAsync` never shows an editable email field for this provider — it acts immediately:
   - **An Identity account already exists with that email** — registration is refused. `ErrorMessage` tells the user to log in to the existing account and link the provider from `/Account/Manage/ExternalLogins` instead. This prevents a second, disconnected account from being created for someone who forgot they already registered, and prevents a client-editable form field from being used to claim an email that isn't the caller's.
   - **No account exists** — a new `IdentityUser<Guid>` is created with the Google email as both username and email; no user interaction beyond the initial Google consent screen is required. If the `email_verified` claim is `true`, `EmailConfirmed` is set on the new user at creation time — Google already verified the address, so the app does not also send its own confirmation-token email or gate sign-in behind `/Account/RegisterConfirmation` (`RequireConfirmedAccount`) for that user.
   - The editable-email confirmation page (`Input.Email` + `OnPostConfirmationAsync`) is retained only as a fallback for a hypothetical external provider that doesn't supply an email claim at all; it applies the same existing-account check before creating a user.
5. On both the new-account path and the existing-account link path (`ExternalLoginsModel.OnGetLinkLoginCallbackAsync`), the provider's claims are added to the Identity user via `UserManagerExtensions.AddMissingClaimsAsync` — but only for claim types the user doesn't already have (existing claim values, e.g. ones an admin edited via `/Admin/Users/Edit/Claims`, are never overwritten), and **excluding `ClaimTypes.NameIdentifier`**. That claim type is what `UserClaimsPrincipalFactory` uses for the user's own ID on every principal it builds (added before, and never deduplicated against, stored `AspNetUserClaims` rows) — persisting Google's `sub` under the same type would give the user two colliding claims of that type. The provider's subject identifier already has a correct home: `AspNetUserLogins.ProviderKey`, populated by `AddLoginAsync`.

### Passkeys / WebAuthn

Registration (during account management):
1. The Passkeys page (`/Account/Manage/Passkeys`) calls `POST /Account/PasskeyCreationOptions` to retrieve WebAuthn creation options.
2. The browser prompts the user for a hardware authenticator or platform authenticator.
3. The credential is stored via `UserManager`.

Login:
1. The login page calls `POST /Account/PasskeyRequestOptions` (with optional `?username=` hint) to retrieve assertion options.
2. The browser performs the WebAuthn assertion.
3. `SignInManager` verifies and signs in the user.

Both endpoints validate antiforgery tokens. In development, origin validation is relaxed to `https://localhost:7261`.

### TOTP two-factor authentication

- Users enable TOTP via `/Account/Manage/EnableAuthenticator`, which displays a QR code (rendered client-side via `davidshimjs-qrcodejs`).
- At login, if 2FA is active, the user is redirected to `/Account/LoginWith2fa`. `LoginWith2faModel.OnGetAsync` calls `GetTwoFactorAuthenticationUserAsync()` to confirm the caller already passed the username/password step; a null result means the page was reached directly rather than via that redirect, and throws.
- Recovery codes (generated at `/Account/Manage/GenerateRecoveryCodes`) are the fallback path.

---

## Data Layer

### Single DbContext, three interfaces

`ApplicationDbContext` implements:

| Interface | Tables managed |
|---|---|
| `IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid, …>` | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`, `AspNetUserPasskeys` |
| `IConfigurationDbContext` | `Clients` (+ child tables), `IdentityResources`, `ApiResources`, `ApiScopes`, `IdentityProviders`, `ClientCorsOrigins` |
| `IPersistedGrantDbContext` | `PersistedGrants`, `DeviceFlowCodes`, `Keys`, `ServerSideSessions`, `PushedAuthorizationRequests` |

The identity key type is `Guid` throughout. `IdentitySchemaVersions.Version3` enables the passkey table (`AspNetUserPasskeys`).

### Schema management

EF Core migrations are not used. The `Identity.Data/` SQL Server Database Project is the authoritative schema source. Changes are applied by building to a `.dacpac` and publishing with `SqlPackage`.

- **Development:** apply changes directly to the local SQL Server instance.
- **Production/CI:** the CI pipeline deploys the `.dacpac` before deploying the application.

### Client/resource *rows* are configuration data, not schema — and they drift

Clients, API scopes and identity resources live in `Clients`, `ApiScopes` and `IdentityResources`, seeded
by `Tools/Identity/*.sql` and thereafter edited through the admin UI. Those scripts use their own
independent incremental numbering, so **the live ids have drifted from the seed files** — adding a row
means inserting it and reading back the generated id, never copying one. Adding `curator.roles` to LocalDB
produced id `5004` where the seed script says `6`.

### A claim only reaches a browser if it is on an *identity* resource

`ApiScopeClaims` put a claim in the **access token**; `IdentityResourceClaims` put it in the **ID token
and userinfo**. A BFF builds its client-visible session from the latter two, so an access-token-only claim
is invisible to the browser no matter how correct the authorization is.

That is why `curator.admin` — present in `ApiScopes.sql` but on no identity resource — was invisible to
Librarian's browser, which made it fetch `GET /me` per page load just to colour its own nav.

**The fix was *not* a new identity resource, and that matters.** Giving the claim its own resource was
implemented and then reverted, because adding a scope to a client's authorize request is a one-way door:
if the app deploys requesting a scope the live client has not been granted, **sign-in fails for every
user** — and the grant has to land in LocalDB *and* production first. Instead the BFF decodes the access
token it already holds at `/bff/callback` and copies a short allowlist of claims into the session
(`Librarian/src/bff/routes.ts`, `accessTokenClaims`). No Identity change, no new scope, no login risk.

Reach for a dedicated identity resource only when a claim genuinely must ride the ID token — and even
then, **never hang it off `profile`**, which every client requests, so a claim placed there is handed to
all of them (the standing `churches.mod` defect, `AGENTS/PARKING_LOT.md` §8b-i). Server-side enforcement
is unaffected either way: Curator authorizes `require_admin` from the access token, and anything the
browser reads is a UI affordance.

---

## External Services

| Service | Purpose | Registration |
|---|---|---|
| **Azure Service Bus** | Transactional email (confirmation, password reset) | Pages inject `IAzureClientFactory<ServiceBusClient>`; call `CreateClient("crgolden")` then `CreateSender("email")` per send; namespace from the `ServiceBusNamespace` configuration value (production) or a connection string (non-production) — not a Key Vault secret |
| **Gravatar** | User avatar images via SHA-256 email hash | `IAvatarService` → `GravatarService` (scoped). **No outbound call and no credential** — see below |
| **Google APIs** | External OpenID Connect login | `AddGoogleOpenIdConnect`; Client ID/Secret from Key Vault |
| **Google reCAPTCHA v3** | Bot scoring on sign-in and registration | `ICAPTCHAService` → `ReCAPTCHAService` (typed `HttpClient`); site/secret keys from Key Vault, verification endpoint from `ReCAPTCHAVerifyEndpoint` config |
| **Azure Key Vault** | Runtime secrets (DB credentials, API keys, OAuth secrets) | No SDK calls at app startup — `crgolden-identity`'s App Service settings hold `@Microsoft.KeyVault(SecretUri=...)` references that the platform resolves into `IConfiguration` before the app starts; `Program.cs` reads them via `IConfiguration.GetRequired<T>` (`Identity.Extensions`) like any other config value |
| **Azure Blob Storage** | Data Protection key persistence | `PersistKeysToAzureBlobStorage` |
| **Azure Key Vault** | Data Protection key encryption | `ProtectKeysWithAzureKeyVault` |

### Gravatar is resolved by construction, not by a call

`GravatarService` builds `https://gravatar.com/avatar/{sha256}?s=2048&d=identicon` and returns it. The
avatar *image* endpoint is public and unauthenticated — Gravatar documents it as usable straight from an
`img` tag — so the URL is a pure function of the email address and there is nothing to look up.

This replaced a round trip to the authenticated Profile API (`api.gravatar.com/v3`) whose only purpose was
to read back a URL we can derive. Deleting it removed the NSwag `OpenApiReference` and its generated
`IGravatar` client, the typed `HttpClient` registration, and the **`GravatarApiSecretKey` Key Vault
secret** — which `Program.cs` read with `GetRequired`, so it was a hard startup dependency for a call the
app no longer makes.

Two consequences worth knowing before changing this:

- **`d=identicon` means every user has an avatar.** Gravatar decides at image-fetch time: the real avatar
  when the address has one, a deterministic generated image when it does not. There is no
  "this address has no Gravatar" case for a caller to branch on and no 404 path to handle. Previously a
  404 from the Profile API meant no claim was written and the user had no avatar at all.
- **The hash input must be trimmed and lower-cased *before* hashing**, per Gravatar's documented
  algorithm. Registration stores `UserName` as the raw typed string, so without normalization a
  mixed-case or space-padded address hashes to a digest Gravatar has never seen and the user silently
  has no avatar. (The lower-casing applied to the resulting hex is a separate, unrelated step:
  `Convert.ToHexString` emits upper-case.)

Gravatar publishes no stability, versioning or deprecation statement for this scheme, and it has already
changed once (MD5 → SHA-256). The derivation is therefore confined to `GravatarService.HashIdentifier`
and one URL template, so a future change moves one method.

### A stored `picture` claim only outranks the computed URL when it is *not* ours

Precedence is "a stored `picture` claim wins", because that claim is how an external photo (Google's,
via `AddMissingClaimsAsync`) reaches a token. That rule rests on an assumption which was **false for
existing rows**: the deleted `PictureClaimWorker` also wrote `picture` claims, and its values were
computed Gravatar URLs from the old Profile API — `https://0.gravatar.com/avatar/{hash}`, with **no
`?s=2048` and no `?d=identicon`**.

Found by running the real service against the real database rather than by any test: `GET /avatar/{sub}`
for a seeded user 302'd to exactly that legacy URL. So for every user the worker had already touched,
none of the improvements above applied — no generated-identicon fallback (so a user without a Gravatar
account got nothing, the one case `d=identicon` exists to remove), no 2048px size, and a hash that may
have been computed from an un-normalized address and therefore point at no avatar at all. The feature
would have shipped looking correct and changing nothing for existing accounts.

`IAvatarService.IsOwnComputedUrl` closes it: the provider recognizes its own URLs, and a stored claim
matching them is ignored and recomputed. It lives on the interface rather than in the two call sites
because the shape of a computed URL is the provider's own knowledge, and both `AvatarProfileService` and
`AvatarEndpoints` must apply the identical rule. `AvatarProfileService` additionally *removes* the stale
claim before adding the fresh one — otherwise the token carries two `picture` claims rather than a
replaced one.

This is self-healing and needs no data migration; it also survives a stale deployment writing another
legacy claim mid-rollout. Host matching is exact-or-subdomain (`gravatar.com`, `*.gravatar.com`), never
`Contains` — `notgravatar.com` is a covered negative case, since a substring test would hand an attacker
a way to have their URL silently discarded.

Duende's automatic key management persists rotated IdentityServer signing keys to the `Keys` table via
`AddOperationalStore`, protected through the same Data Protection key ring configured above. The two
lifetimes must match: a signing key written under one key ring is permanently undecryptable once that
ring is gone. In production this holds by construction (both are durable — Azure Blob Storage + Key
Vault). Non-production persists the key ring to a local, gitignored `.dataprotection-keys/` folder next
to the app for the same reason — an ephemeral key ring here would silently orphan every signing key
across a restart, which is what happened before this was fixed (accumulated `CryptographicException`s on
startup, one per orphaned row).

### Azure credential strategy

| Environment | Credentials enabled |
|---|---|
| `Development` | `AzureCliCredential`, `VisualStudioCredential` |
| `CI` | `AzureCliCredential` only |
| `Production` | Full `DefaultAzureCredential` chain (managed identity) |

Options are sourced from `DefaultAzureCredentialOptions` in User Secrets (development) or environment variables (CI/production) — not from `appsettings.Development.json`, which is not loaded in CI.

---

## Observability

### Structured logging — Serilog

- Bootstrap logger writes to console before the host is built.
- After build, a full Serilog pipeline is configured:
  - **Production:** Elasticsearch sink to data stream `logs-app-Identity` (ECS format, basic auth, `BootstrapMethod.Failure` so a missing node does not crash startup) + OpenTelemetry sink.
  - **Non-production:** console sink only; Duende's `IdentityServer.Diagnostics.Summary` source filtered out.

### Distributed tracing and metrics — OpenTelemetry → Grafana Alloy

`AddOtlpExporter()` (pointed at `AlloyEndpoint`) exports all signals — Azure Monitor/`UseAzureMonitor()` was removed. Additional sources subscribed:

**Meters:**
- `Duende.IdentityServer` — built-in token issuance, introspection, secret validation
- `Identity` — custom UI counters (see below)
- ASP.NET Core, HTTP client, .NET runtime instrumentation

**Trace sources:**
- `IdentityServerConstants.Tracing.Basic/.Cache/.Services/.Stores/.Validation`
- ASP.NET Core instrumentation (health-check requests filtered out via `/health` path check)
- HTTP client instrumentation
- Console exporter in Development

### Custom `Identity` meter

Defined in `Telemetry.cs`, meter name `"Identity"`:

| Counter | Tags | Emitted from |
|---|---|---|
| `identity.consent.granted` | `client_id`, `remember` (bool), `scope_count` | `/Account/Manage/Consent` on allow |
| `identity.consent.denied` | `client_id`, `scope_count` | `/Account/Manage/Consent` on deny |
| `identity.grants.revoked` | `client_id` | `/Account/Manage/Grants` on revoke |
| `identity.exceptions` | `exception.type` | Global exception handler (`HttpContextExtensions.HandleException`) |

---

## Security

### Security headers

`UseSecurityHeaders()` (`Extensions/ApplicationBuilderExtensions.cs`) is registered as the first middleware in `Program.cs`, so it covers every page. Headers are written from a `Response.OnStarting` callback and only when the response content type is `text/html`.

| Header | Value |
|---|---|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `no-referrer` |
| `Content-Security-Policy` | `default-src 'self'; script-src 'self' https://www.google.com https://www.gstatic.com; style-src 'self'; img-src 'self' data: https:; connect-src 'self' https://www.google.com; frame-src https://www.google.com; object-src 'none'; frame-ancestors 'none'; base-uri 'self';` (only if not already set) |

`X-Frame-Options` is `DENY` rather than `SAMEORIGIN` so it agrees with `frame-ancestors 'none'` instead of contradicting it.

This was previously a per-page `[SecurityHeaders]` action filter carried by seven page models. Everything else — 160 pages, including `/Account/Login` — received no headers at all and was therefore framable, which is a clickjacking exposure on the credential-entry page of an identity provider. Applying the headers globally is what closed that.

**The `text/html` gate is load-bearing.** `X-Frame-Options: DENY` on `/connect/authorize` would break any client that renews tokens through a hidden iframe. Those responses are redirects rather than HTML, so they never receive the header. The `ContainsKey` guard on CSP serves the same purpose for Duende, which sets its own policy on some of its responses.

Every library is served from `wwwroot`, so no CDN hosts appear in `script-src` or `style-src`. The remaining allowances exist for reCAPTCHA on `Login` and `Register`, which loads from `www.google.com` and `www.gstatic.com` and renders its badge in a `www.google.com` frame. `img-src` permits arbitrary HTTPS origins because the consent and device pages render client logos from URLs supplied by client configuration.

**No inline scripts or inline styles remain in any view.** That is what allows the policy to stay strict without a nonce or `'unsafe-inline'`. Where a page needs server data in JavaScript it passes it through `data-` attributes — `Login` and `Register` carry the reCAPTCHA site key and action on the hidden token input, consumed by `wwwroot/js/recaptcha.js`; `Redirect` carries its target on `<body data-redirect-uri>`, consumed by `wwwroot/js/redirect.js`. Sizing that would otherwise want an inline `style` uses an HTML attribute instead (`height="32"` on client logos), since `style-src 'self'` blocks style attributes.

### No import map

`_Layout.cshtml` deliberately does not carry a `<script type="importmap">` element. When present, the MVC tag helper fills it at render time with a fingerprint and integrity map covering every static asset — roughly half the HTML of a page like `/Account/Login`, inline and therefore never cached. Nothing consumes it: the app has no ES modules and no `type="module"` scripts.

Fingerprinted URLs for ordinary `<script src>` and `<link href>` come from the static asset manifest server-side and do not depend on the import map.

Restore the element if ES modules are ever introduced — bare and relative module specifiers will not resolve to fingerprinted files without it.

### Bot protection — reCAPTCHA v3

`Login` and `Register` are the only pages that score requests. Both render the site key into the page, collect a token client-side, and pass it to `ICAPTCHAService.VerifyAsync` on POST. A score below `ScoreThreshold` (default `0.5`) fails the request with the generic message "Request could not be verified." — deliberately non-specific, so a bot cannot tell a low score apart from a bad password.

`VerifyAsync` returns `0` — a guaranteed failure — whenever the token is blank, the secret key is unset, the verification call returns a non-success status, or Google reports `success: false`. Verification therefore fails closed: a misconfigured or unreachable reCAPTCHA blocks sign-in and registration rather than silently allowing everything through.

`ICAPTCHAService.IsExempt` skips scoring entirely for two addresses, `AdminEmail` and `TestEmail`, compared case-insensitively. This exists so the E2E and smoke suites can authenticate without solving a challenge, and so the admin account is not locked out by a reCAPTCHA outage. Both values come from Key Vault. Treat them as security-relevant: any address placed in either setting bypasses bot scoring on both pages, so they must name accounts that are not otherwise reachable.

The CSP's `script-src` includes `https://www.google.com https://www.gstatic.com` specifically so `Login` and `Register` can load reCAPTCHA; the frame it renders comes from `www.google.com` too, matched by `frame-src`.

Both pages load `https://www.google.com/recaptcha/api.js?render=<site key>` eagerly (not deferred to form interaction) with the `async` attribute, so it never blocks HTML parsing. That script auto-renders Google's invisible-widget `<iframe>` as soon as it runs — `render=<site key>` requires this, there is no way to get the v3-style `execute(siteKey, { action })` call to work without it. That iframe is what `window.onload` waits on, and per MDN, `load` waits for iframes and for async scripts alike — the `async` attribute changes when/how the browser fetches and executes the script, not whether `load` waits for it. Confirmed by HAR against production on `Register`: `api.js` itself loads in well under a second, but the anchor iframe it spawns doesn't finish for another ~19–30+ seconds (matching the `anchor-ms=20000&execute-ms=30000` budget Google's own script requests), and `window.onload` (and anything gated on it, e.g. a favicon fetch) doesn't fire until that resolves — this is Google-side latency inherent to eager invisible-widget initialization, not a bug in this app. The page itself is styled and interactive within ~1 second regardless; nothing user-visible is blocked by the long tail. Trading eager-async for load-on-interaction was considered and rejected: it would move that same 19–30s cost to the moment a user starts filling the form instead, which is worse.

### Antiforgery

Both passkey minimal API endpoints call `IAntiforgery.ValidateRequestAsync` before processing. The Razor Pages framework handles antiforgery validation for all form submissions automatically.

### CORS

Allowed origins are read from the `CorsPolicy:Origins` configuration array (supplied via User Secrets or environment variables). Applied via `UseCors` in the middleware pipeline.

---

## Dependency Injection & Middleware

### Service registration order (`Program.cs`)

1. User Secrets (Development only)
2. Azure Key Vault secrets — fetched at startup (production only)
3. OpenTelemetry (metrics + tracing) → Grafana Alloy (OTLP)
4. Serilog
5. SQL connection string configuration → `DbContextPool<ApplicationDbContext>`
6. ASP.NET Identity (`IdentityUser<Guid>`, `IdentityRole<Guid>`) + EF stores
7. IdentityServer (configuration store + operational store + ASP.NET Identity integration)
8. Google OpenID Connect external authentication
9. Azure Service Bus (`ServiceBusClient` + named `ServiceBusSender` "email")
10. Gravatar HTTP client (`IGravatar` + `IAvatarService`)
11. Razor Pages
12. CORS
13. Health checks (DbContext check)
14. Data Protection (Azure Blob + Key Vault)
15. Problem Details (`AddProblemDetails`) — enables `IProblemDetailsService` used by the global exception handler
16. Database developer page exception filter (Development only)
17. Passkey origin validator (Development only — relaxed to `https://localhost:7261`)

### Middleware pipeline order

```
UseSerilogRequestLogging
→ UseExceptionHandler(lambda: HandleException) / UseDeveloperExceptionPage
→ UseHsts (production)
→ UseHttpsRedirection
→ UseRouting
→ UseIdentityServer          ← registers the OIDC/OAuth2 protocol middleware
→ UseCors
→ UseAuthorization
→ MapAdditionalIdentityEndpoints   (passkey minimal API)
→ MapHealthChecks("/health")
→ MapStaticAssets
→ MapRazorPages.RequireAuthorization
```

`UseIdentityServer` must come after `UseRouting` and before `UseAuthorization`. It encompasses `UseAuthentication`.

---

## CI/CD Pipeline

Defined in `.github/workflows/main_crgolden-identity.yml`. Triggers: push to `main`, pull request events, manual dispatch, and weekly schedule (mutation tests, Monday 02:00 UTC).

### Build job (`windows-latest`)

1. Set up Java 17 (SonarCloud scanner), .NET 10, restore NuGet cache.
2. Begin SonarCloud scan.
3. `dotnet build --no-incremental --configuration Release` — builds all projects including the `.dacpac`.
4. Run unit tests with `coverlet.console` (OpenCover); write `coverage.opencover.xml`.
5. Azure OIDC login → deploy `.dacpac` to E2E test database.
6. Run E2E tests (`ASPNETCORE_ENVIRONMENT=CI`); write `coverage-e2e.xml`.
7. End SonarCloud scan (reads both coverage files).
8. Publish web app (`-r win-x86 --self-contained false`).
9. Upload artifacts: published app, `.dacpac`, test results.

### Deploy job (`windows-latest`, after build)

1. Azure OIDC login.
2. Deploy `.dacpac` to production SQL Server via `SqlPackage`.
3. Deploy web app to Azure App Service (`crgolden-identity`, Production slot).

The database is always deployed before the application to ensure schema readiness on startup.

### Smoke job (`windows-latest`, after deploy, `main` only)

Downloads the published test binaries and runs the `Category=Smoke` suite against the deployed site (`SMOKE_BASE_URL`), then reports results to Azure DevOps and Azure Monitor.

### Mutation job (`windows-latest`, `schedule` or `workflow_dispatch`)

Builds the solution and runs Stryker.NET (`stryker-config.json`), uploading the report as the `stryker-report` artifact.

### Environment differences

| Setting | Development | CI | Production |
|---|---|---|---|
| Azure credentials | `AzureCliCredential`, `VisualStudioCredential` | `AzureCliCredential` | Full `DefaultAzureCredential` |
| Config source | User Secrets | Environment variables (no `appsettings.CI.json`) | Key Vault + env vars |
| Serilog | Console only | Elasticsearch (`BootstrapMethod.Failure`) | Elasticsearch + OpenTelemetry |
| Passkey origin | Relaxed (`localhost:7261`) | Strict | Strict |
| IdentityServer events | All (errors, info, failures, successes) | Default | Default |
| DB name | `Identity` (User Secrets) | `IdentityTest` / `E2E_DB_NAME` | `Identity` |
