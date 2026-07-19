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
| `Identity.Benchmarks/` | BenchmarkDotNet project | Microbenchmarks for password hashing and Gravatar SHA-256 computation |

---

## URL & Routing Conventions

All Razor Pages require authentication by default (`MapRazorPages().RequireAuthorization()`). Pages that must be reachable before login explicitly opt out with `[AllowAnonymous]`.

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
| SAML Service Providers | `/Admin/SamlServiceProviders` | `IConfigurationDbContext` | Flat edit |
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

### IdentityServer flow pages

These pages participate in the OAuth2/OIDC protocol flows. They use `[Authorize]` or `[AllowAnonymous]` explicitly and always carry the `[SecurityHeaders]` filter.

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
- At login, if 2FA is active, the user is redirected to `/Account/LoginWith2fa`.
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

---

## External Services

| Service | Purpose | Registration |
|---|---|---|
| **Azure Service Bus** | Transactional email (confirmation, password reset) | Pages inject `IAzureClientFactory<ServiceBusClient>`; call `CreateClient("crgolden")` then `CreateSender("email")` per send; namespace from Key Vault (production) or connection string (non-production) |
| **Gravatar** | User avatar images via SHA-256 email hash | `IAvatarService` → `GravatarService` (scoped); NSwag-generated `IGravatar` HTTP client with Bearer auth |
| **Google APIs** | External OpenID Connect login | `AddGoogleOpenIdConnect`; Client ID/Secret from Key Vault |
| **Azure Key Vault** | Runtime secrets (DB credentials, API keys, OAuth secrets) | `SecretClient`; the 11 secrets in `SecretClientExtensions.GetIdentitySecrets()` are fetched at startup (production only) |
| **Azure Blob Storage** | Data Protection key persistence | `PersistKeysToAzureBlobStorage` |
| **Azure Key Vault** | Data Protection key encryption | `ProtectKeysWithAzureKeyVault` |

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

### `SecurityHeadersAttribute`

Applied to all IdentityServer flow pages (`[SecurityHeaders]`). Adds headers to every `PageResult`:

| Header | Value |
|---|---|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `SAMEORIGIN` |
| `Referrer-Policy` | `no-referrer` |
| `Content-Security-Policy` | `default-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self';` (only if not already set) |

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
