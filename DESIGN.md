# Design Documentation

## Purpose

This application is a standalone **OpenID Connect Identity Provider** (IdP). Client applications redirect users here to authenticate, and this app issues tokens that clients use to verify identity. It does not host any application logic beyond authentication and account management.

Built on:
- **Duende IdentityServer 7** — authorization server (OIDC/OAuth2 token issuance)
- **ASP.NET Core Identity** — user store, password hashing, 2FA, passkeys
- **SQL Server** — persistence via EF Core (`ApplicationDbContext`)

---

## Solution Structure

| Project | Type | Role |
|---|---|---|
| `Identity.Api/` | ASP.NET Core 10 web app | The running application — Razor Pages, services, `Program.cs`, `ApplicationDbContext` |
| `Identity.Data/` | SQL Server Database Project (SSDT) | Authoritative schema source; builds to a `.dacpac` for production deployment |
| `Identity.Tests/` | xUnit v3 test project | Unit, E2E (Playwright/Chromium), load, and property-based tests |
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

### IdentityServer flow pages

These pages participate in the OAuth2/OIDC protocol flows. They use `[Authorize]` or `[AllowAnonymous]` explicitly and always carry the `[SecurityHeaders]` filter.

| URL | Auth | Purpose |
|---|---|---|
| `/Consent/Index` | `[Authorize]` | OAuth2 scope approval — allow/deny, optional remember |
| `/Grants/Index` | `[Authorize]` | View and revoke previously granted client permissions |
| `/Device/Index` | `[Authorize]` | Device authorization flow — user code entry + scope consent |
| `/Device/Success` | `[AllowAnonymous]` | Device flow success confirmation |
| `/Ciba/Index` | `[AllowAnonymous]` | CIBA backchannel login request display |
| `/ServerSideSessions/Index` | `[Authorize]` | View and revoke active server-side sessions |
| `/Diagnostics/Index` | `[Authorize]` | Current user claims/tokens (loopback only, development nav link) |
| `/Redirect/Index` | `[AllowAnonymous]` | Intermediate loading page for native-client protocol redirects |

### Infrastructure endpoints

| URL | Mechanism | Purpose |
|---|---|---|
| `/Health` | `MapHealthChecks` | `ApplicationDbContext` connectivity check; HTTP metrics disabled |
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
2. ASP.NET Core redirects to Google, using `GoogleOpenIdConnectDefaults.AuthenticationScheme` with `SignInScheme = IdentityConstants.ExternalScheme`.
3. Google redirects back to `/Account/ExternalLogin`, which either links the account to an existing user or creates a new one.

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
| **Resend** | Transactional email (confirmation, password reset) | `IEmailSender` → `EmailSender` (transient); API token from Key Vault |
| **Gravatar** | User avatar images via SHA-256 email hash | `IAvatarService` → `GravatarService` (scoped); NSwag-generated `IGravatar` HTTP client with Bearer auth |
| **Google APIs** | External OpenID Connect login | `AddGoogleOpenIdConnect`; Client ID/Secret from Key Vault |
| **Azure Key Vault** | Runtime secrets (DB credentials, API keys, OAuth secrets) | `SecretClient`; 8 secrets fetched concurrently at startup via `Task.WhenAll` |
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
  - **Production:** Elasticsearch sink to data stream `logs-dotnet-identity` (ECS format, basic auth, `BootstrapMethod.Failure` so a missing node does not crash startup) + OpenTelemetry sink.
  - **Non-production:** console sink only; Duende's `IdentityServer.Diagnostics.Summary` source filtered out.

### Distributed tracing and metrics — OpenTelemetry → Azure Monitor

`UseAzureMonitor()` exports all signals. Additional sources subscribed:

**Meters:**
- `Duende.IdentityServer` — built-in token issuance, introspection, secret validation
- `Identity` — custom UI counters (see below)
- ASP.NET Core, HTTP client, .NET runtime instrumentation

**Trace sources:**
- `IdentityServerConstants.Tracing.Basic/.Cache/.Services/.Stores/.Validation`
- ASP.NET Core instrumentation (health-check requests filtered out via `/Health` path check)
- HTTP client instrumentation
- Console exporter in Development

### Custom `Identity` meter

Defined in `Telemetry.cs`, meter name `"Identity"`:

| Counter | Tags | Emitted from |
|---|---|---|
| `identity.consent.granted` | `client_id`, `remember` (bool), `scope_count` | `/Consent/Index` on allow |
| `identity.consent.denied` | `client_id`, `scope_count` | `/Consent/Index` on deny |
| `identity.grants.revoked` | `client_id` | `/Grants/Index` on revoke |

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
2. Azure Key Vault secrets — fetched concurrently at startup
3. OpenTelemetry (metrics + tracing) → Azure Monitor
4. Serilog
5. SQL connection string configuration → `DbContextPool<ApplicationDbContext>`
6. ASP.NET Identity (`IdentityUser<Guid>`, `IdentityRole<Guid>`) + EF stores
7. IdentityServer (configuration store + operational store + ASP.NET Identity integration)
8. Google OpenID Connect external authentication
9. Resend (`IResend` + `IEmailSender`)
10. Gravatar HTTP client (`IGravatar` + `IAvatarService`)
11. Razor Pages
12. CORS
13. Health checks (DbContext check)
14. Data Protection (Azure Blob + Key Vault)
15. Database developer page exception filter (Development only)
16. Passkey origin validator (Development only — relaxed to `https://localhost:7261`)

### Middleware pipeline order

```
UseSerilogRequestLogging
→ UseExceptionHandler / UseDeveloperExceptionPage
→ UseHsts (production)
→ UseHttpsRedirection
→ UseRouting
→ UseIdentityServer          ← registers the OIDC/OAuth2 protocol middleware
→ UseCors
→ UseAuthorization
→ MapAdditionalIdentityEndpoints   (passkey minimal API)
→ MapHealthChecks("/Health")
→ MapStaticAssets
→ MapRazorPages.RequireAuthorization
```

`UseIdentityServer` must come after `UseRouting` and before `UseAuthorization`. It encompasses `UseAuthentication`.

---

## Testing Strategy

### Test categories

| Category | Mechanism | Notes |
|---|---|---|
| `Unit` | In-process, no I/O | Services, extensions, page HTML structure, property-based fuzzing |
| `E2E` | Playwright (Chromium headless) against in-process Kestrel | Requires real DB (schema deployed), Azure credentials, Key Vault access |
| `Load` | Playwright against in-process Kestrel | Requires running server; not run in standard CI build |

### Infrastructure

**`IdentityWebApplicationFactory`** — extends `WebApplicationFactory<Program>`:
- Starts a real Kestrel HTTPS server on a random port for Playwright.
- Replaces `IEmailSender` with `EmailCaptureService` (captures email tokens instead of calling Resend).
- Replaces `IAvatarService` with `NullAvatarService` (avoids Gravatar HTTP calls).
- In Development, replaces Serilog `ILoggerFactory` with a console logger (avoids Elasticsearch connection at startup).

**`PlaywrightFixture`** — xUnit `IAsyncLifetime`:
- Installs Chromium (`playwright install chromium`) on first run.
- Warms up the server.
- Provides `NewPageAsync()` for each test.
- In CI, cleans up the test database after the suite.

Test collections run serially (`parallelizeTestCollections: false` in `xunit.runner.json`) to prevent `WebApplicationFactory` startup from timing out Key Vault calls when the thread pool is saturated.

### Benchmarks

`Identity.Benchmarks/` measures two hot paths:
- **`AuthenticationBenchmarks`** — `PasswordHasher.HashPassword` and `VerifyHashedPassword` (PBKDF2) across short, medium, and long passwords.
- **`GravatarBenchmarks`** — SHA-256 hash computation comparing the allocating `Convert.ToHexString` baseline against a stack-allocated `Span<byte>` alternative.

### Mutation testing

Stryker targets five core files: `EmailSender.cs`, `GravatarService.cs`, `SecretClientExtensions.cs`, `ConfigurationExtensions.cs`, `EndpointRouteBuilderExtensions.cs`. Thresholds: high ≥ 80, low ≥ 60, break < 50.

---

## CI/CD Pipeline

Defined in `.github/workflows/main_crgolden-identity.yml`. Triggers: push to `main`, pull request events, manual dispatch, and weekly schedule (mutation tests, Monday 02:00 UTC).

### Build job (`windows-latest`)

1. Set up Java 17 (SonarCloud scanner), .NET 10, restore NuGet cache.
2. Begin SonarCloud scan.
3. `dotnet build --no-incremental --configuration Release` — builds all projects including the `.dacpac`.
4. Run unit tests with `dotnet-coverage`; write `coverage.xml`.
5. Azure OIDC login → deploy `.dacpac` to E2E test database.
6. Run E2E tests (`ASPNETCORE_ENVIRONMENT=CI`); write `coverage-e2e.xml`.
7. End SonarCloud scan (reads both coverage files).
8. Publish web app (`-r win-x64 --self-contained false`).
9. Upload artifacts: published app, `.dacpac`, test results.

### Deploy job (`windows-latest`, after build)

1. Azure OIDC login.
2. Deploy `.dacpac` to production SQL Server via `SqlPackage`.
3. Deploy web app to Azure App Service (`crgolden-identity`, Production slot).

The database is always deployed before the application to ensure schema readiness on startup.

### Environment differences

| Setting | Development | CI | Production |
|---|---|---|---|
| Azure credentials | `AzureCliCredential`, `VisualStudioCredential` | `AzureCliCredential` | Full `DefaultAzureCredential` |
| Config source | User Secrets | `appsettings.CI.json` + env vars | Azure App Configuration + env vars |
| Serilog | Console only | Elasticsearch (`BootstrapMethod.Failure`) | Elasticsearch + OpenTelemetry |
| Passkey origin | Relaxed (`localhost:7261`) | Strict | Strict |
| IdentityServer events | All (errors, info, failures, successes) | Default | Default |
| DB name | `Identity` (User Secrets) | `IdentityTest` / `E2E_DB_NAME` | `Identity` |
