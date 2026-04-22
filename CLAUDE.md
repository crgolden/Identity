# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Reference Source

When working with Duende IdentityServer types, **prefer the local clones** listed below. Fall back to GitHub raw URLs only if the local clone is absent or stale.

### Local clones (preferred)

| Repo | Local path |
|---|---|
| `DuendeSoftware/products` (IdentityServer source) | `%USERPROFILE%\source\repos\DuendeSoftware\products\identity-server\` |
| `DuendeSoftware/samples` (sample apps) | `%USERPROFILE%\source\repos\DuendeSoftware\samples\IdentityServer\v7\AspNetIdentityPasskeys\` |

Key subdirectories inside the local `products` clone:
- `src\IdentityServer\Services\Default\` — interaction services
- `src\IdentityServer\Models\` — domain models
- `src\EntityFramework.Storage\Entities\` — EF entity definitions

### GitHub raw URLs (fallback)

**Duende products monorepo** (`DuendeSoftware/products`, `main` branch):

| What | Raw URL pattern |
|---|---|
| EF entity definitions (Client, ApiScope, IdentityResource, etc.) | `https://raw.githubusercontent.com/DuendeSoftware/products/refs/heads/main/identity-server/src/EntityFramework.Storage/Entities/{EntityName}.cs` |
| IdentityServer models (non-EF) | `https://raw.githubusercontent.com/DuendeSoftware/products/refs/heads/main/identity-server/src/IdentityServer/Models/{ModelName}.cs` |
| IdentityServer services / interfaces | `https://raw.githubusercontent.com/DuendeSoftware/products/refs/heads/main/identity-server/src/IdentityServer/Services/` |

**Quickstart sample** (Quickstart 5 — ASP.NET Identity integration):

| What | URL |
|---|---|
| Sample source root | `https://raw.githubusercontent.com/DuendeSoftware/samples/main/IdentityServer/v7/Quickstarts/5_AspNetIdentity/src/IdentityServerAspNetIdentity/` |
| Pages folder | append `Pages/{PageFolder}/Index.cshtml` or `Index.cshtml.cs` to the above |

**Key EF entities to reference when seeding test data or writing migrations:**
- `Client.cs` — navigation collections are `AllowedGrantTypes`, `RedirectUris`, `AllowedScopes`, `ClientSecrets`, `AllowedCorsOrigins`
- `ClientGrantType.cs` — has `GrantType` string property
- `ClientRedirectUri.cs` — has `RedirectUri` string property
- `ClientScope.cs` — has `Scope` string property

---

## Solution Structure

The solution (`Identity.slnx`) contains four projects:

- **`Identity.Api/`** — ASP.NET Core 10 Razor Pages web application (the main app), also houses `ApplicationDbContext`
- **`Identity.Data/`** — SQL Server Database Project (SSDT); the schema source of truth for production, builds to a `.dacpac`
- **`Identity.Tests/`** — xUnit v3 test project: unit tests (using `Microsoft.AspNetCore.Mvc.Testing`, `Moq`) and browser-based E2E tests (using `Microsoft.Playwright`)
- **`Identity.Benchmarks/`** — BenchmarkDotNet microbenchmarks for authentication-critical hot paths (password hashing, Gravatar hash computation)

## What This App Does

This is a standalone **OpenID Connect Identity Provider** built on:
- **Duende IdentityServer 7** — acts as the authorization server (OIDC/OAuth2)
- **ASP.NET Core Identity** — user management (stored as `IdentityUser<Guid>`)
- **SQL Server** — backing store via EF Core

Authentication features supported:
- Local username/password with email confirmation
- **Google OpenID Connect** external login (via `Google.Apis.Auth.AspNetCore3`)
- **Passkeys / WebAuthn** (ASP.NET Core Identity built-in passkey support)
- TOTP two-factor authentication (with QR code via `davidshimjs-qrcodejs`)
- Recovery codes

Email is sent via **Resend** (`EmailSender.cs` implements `IEmailSender`).

Avatar/profile images are served via **Gravatar** (`GravatarService` implements `IAvatarService`; the `Gravatar` HTTP client is generated from the Gravatar OpenAPI spec at `OpenAPIs/gravatar.json`).

## Key Architecture Points

### Single DbContext for Everything

`ApplicationDbContext` (in `Identity.Api/ApplicationDbContext.cs`) implements three interfaces simultaneously:
- `IdentityDbContext<IdentityUser<Guid>, ...>` — ASP.NET Identity tables
- `IConfigurationDbContext` — Duende IdentityServer clients/resources
- `IPersistedGrantDbContext` — Duende IdentityServer grants/sessions

EF Core migrations are not currently present in the repository. The `Identity.Data` SQL project is the authoritative schema source for all environments.

### Database Change Strategy

Two separate mechanisms are used depending on environment:

- **Development** — Apply schema changes directly to the local SQL Server (see `Identity.Data/` for table definitions). EF Core migrations are not currently used.
- **Production** — The **`Identity.Data` SQL project** is the source of truth. Schema changes are deployed via `.dacpac` (built from `Identity.Data.sqlproj` and published with `SqlPackage`).

When making a schema change, update the corresponding table definition in `Identity.Data/` for both environments.

### Passkey Endpoints

Two minimal API endpoints are registered in `EndpointRouteBuilderExtensions.cs` (called via `app.MapAdditionalIdentityEndpoints()`):
- `POST /Account/PasskeyCreationOptions` — returns WebAuthn creation options JSON
- `POST /Account/PasskeyRequestOptions` — returns WebAuthn request options JSON

Both require antiforgery validation. The Passkey Razor Pages live in `Pages/Account/Manage/`.

### Passkey client-side implementation (`passkey-submit.js`)

`wwwroot/js/passkey-submit.js` is a form-associated custom element that drives the WebAuthn flow on both the Login page (`operation="Request"`) and the Passkeys management page (`operation="Create"`). It is rendered by `PasskeySubmitTagHelper` — **`Pages/_ViewImports.cshtml` must include `@addTagHelper *, Identity.Api`** or the tag helper is silently ignored and no button is rendered.

**Reference implementation:** `C:\Users\crgol\source\repos\DuendeSoftware\samples\IdentityServer\v7\AspNetIdentityPasskeys\IdentityServerAspNetIdentityPasskeys\wwwroot\js\passkey-submit.js` — keep in sync when updating.

**Known Chrome `credential.toJSON()` bugs worked around in `passkey-submit.js`:**

1. **Standard base64 instead of base64url** — Chrome encodes `rawId`, `attestationObject`, and other binary fields with `+`/`/` (standard base64) rather than `-`/`_` (base64url). ASP.NET Core Identity rejects this. Fixed by post-processing: `.replace(/\+/g, '-').replace(/\//g, '_')`.
2. **`clientExtensionResults` omitted when empty** — Chrome omits this required property when no extensions are active. Fixed by injecting `clientExtensionResults: {}` if absent after parsing.
3. **`convertToBase64` fallback** — The manual serialization fallback (triggered by password managers that don't implement `PublicKeyCredential.prototype.toJSON` correctly) requires a `convertToBase64` helper method on the class. It is defined in `passkey-submit.js`.

### Open Redirect Protection

All login-path handlers validate `returnUrl` before redirecting. **Never call `LocalRedirect(returnUrl)` directly.** Always use:

```csharp
return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : LocalRedirect("~/");
```

This guards against protocol-relative URL open redirects (e.g. `//evil.com`), which browsers interpret as `https://evil.com`. ASP.NET Core's built-in `LocalRedirect` performs its own `IsLocalUrl` check, but this has been observed to be insufficient for protocol-relative URLs in some .NET versions. The explicit guard is applied in `Login.cshtml.cs`, `LoginWith2fa.cshtml.cs`, `LoginWithRecoveryCode.cshtml.cs`, and `ExternalLogin.cshtml.cs`.

### Identity User Type

All services use `IdentityUser<Guid>` (not the default `IdentityUser`). This must be kept consistent throughout — `UserManager<IdentityUser<Guid>>`, `SignInManager<IdentityUser<Guid>>`, `AddAspNetIdentity<IdentityUser<Guid>>()`, etc.

### IdentityServer UI Pages

All IdentityServer-specific Razor Pages live under `Identity.Api/Pages/` in `Identity.Pages.*` namespaces. They follow the same conventions as the ASP.NET Identity pages (XML doc comments, `[Authorize]`/`[AllowAnonymous]`, `[SecurityHeaders]` filter, async handlers).

| Path | Namespace | Purpose |
|---|---|---|
| `/Account/Manage/Consent` | `Identity.Pages.Account.Manage` | OAuth2 scope approval (allow/deny, remember consent) |
| `/Account/Manage/Grants` | `Identity.Pages.Account.Manage` | View and revoke previously granted client permissions |
| `/Account/Manage/Device` | `Identity.Pages.Account.Manage` | Device authorization flow — user code entry + scope consent |
| `/Account/Manage/DeviceSuccess` | `Identity.Pages.Account.Manage` | Device flow success confirmation (`[AllowAnonymous]`) |
| `/Ciba` | `Identity.Pages` | CIBA backchannel login request display (`[AllowAnonymous]`) |
| `/Account/Manage/ServerSideSessions` | `Identity.Pages.Account.Manage` | View and remove active server-side user sessions |
| `/Redirect` | `Identity.Pages` | Loading page for native client redirects (`[AllowAnonymous]`) |
| `/Account/Manage/Diagnostics` | `Identity.Pages.Account.Manage` | Current user claims/tokens — loopback only, dev-only nav link |

**Support infrastructure:**
- `Identity.Api/Filters/SecurityHeadersAttribute.cs` — `IResultFilter` adding `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, and `Content-Security-Policy` headers to all `PageResult` responses. Namespace: `Identity.Filters`.
- `Identity.Api/Extensions/PageModelExtensions.cs` — `LoadingPage()` extension on `PageModel` that redirects to `/Redirect`. Namespace: `Identity.Extensions`.
- `Identity.Api/Pages/Account/Manage/ConsentOptions.cs` — static options (`EnableOfflineAccess`, display names, error messages) shared by Consent and Device pages.
- `Identity.Api/Telemetry.cs` — `System.Diagnostics.Metrics.Meter`-based counters for `identity.consent.granted`, `identity.consent.denied`, and `identity.grants.revoked`. Namespace: `Identity`.

**Error redirects** use `/Error` (the existing root error page, matching `identityServerOptions.UserInteraction.ErrorUrl = "/Error"` in Program.cs) — not `/Home/Error/Index`.

### Observability

- **Azure Monitor** — OpenTelemetry metrics and traces via `UseAzureMonitor()`
- **IdentityServer OTel signals** — `Program.cs` subscribes to both the Duende built-in meter (`Duende.IdentityServer` — token issuance, introspection, secret validation) and the custom `Identity` meter (consent/grants UI events). Tracing also subscribes to all five Duende trace sources: `IdentityServerConstants.Tracing.Basic/.Cache/.Services/.Stores/.Validation`.
- **Serilog** — structured logging with an Elasticsearch sink (`logs-dotnet-identity` data stream) and console sink bootstrap. `Elastic.Serilog.Sinks` 9.0.0.
- Health checks endpoint at `/Health` with `DbContext` check; health check requests are filtered from traces

### Data Protection

Keys are persisted to **Azure Blob Storage** and protected with **Azure Key Vault** (via `PersistKeysToAzureBlobStorage` / `ProtectKeysWithAzureKeyVault`).

### Configuration

All sensitive values are retrieved at startup from **Azure Key Vault** using `DefaultAzureCredential`. The credential chain is narrowed per environment to prevent ambiguous or slow credential probing:

| Environment | Enabled credential | Typical use |
|---|---|---|
| `Development` | `AzureCliCredential`, `VisualStudioCredential` | Local dev via `az login` or VS sign-in; configured via User Secrets (see below) |
| `CI` | `AzureCliCredential` | CI — after `azure/login` OIDC step |
| `Production` (default) | All credentials (full `DefaultAzureCredential` chain) | Azure App Service managed identity |

Non-secret infrastructure values are set via `appsettings.json` (nulled out) and supplied through User Secrets (development) or environment/Azure App Configuration (production):

```json
{
  "ElasticsearchNode": "<Elasticsearch node URI>",
  "KeyVaultUri": "<Azure Key Vault URI>",
  "BlobUri": "<Azure Blob Storage URI for Data Protection keys>",
  "DataProtectionKeyIdentifier": "<Azure Key Vault key URI for Data Protection>",
  "APPLICATIONINSIGHTS_CONNECTION_STRING": "<Azure Monitor connection string>",
  "SqlConnectionStringBuilder": {
    "DataSource": "<SQL Server host>",
    "InitialCatalog": "Identity",
    "IntegratedSecurity": false
  },
  "CorsPolicy": {
    "Origins": []
  }
}
```

**Key Vault secrets** fetched at startup:
- `GravatarApiSecretKey`
- `ElasticsearchUsername` / `ElasticsearchPassword`
- `SqlServerUserId` / `SqlServerPassword`
- `GoogleClientId` / `GoogleClientSecret`
- `ResendApiToken`

User Secrets ID: `aspnet-Identity-149346d0-999f-4a74-8ff7-2a92d39790f2`

> **Important:** `Program.cs` calls `builder.Configuration.AddUserSecrets("aspnet-Identity-149346d0-999f-4a74-8ff7-2a92d39790f2")` explicitly (by string ID). See [../CLAUDE.md](../CLAUDE.md) for why this is required (`Assembly.GetEntryAssembly()` returns the test runner assembly inside `WebApplicationFactory`, so implicit loading targets the wrong secrets file). `DefaultAzureCredentialOptions` must also be configured in User Secrets — see the parent CLAUDE.md for the recommended Development values.

## Commands

### Build
```bash
dotnet build
dotnet build --configuration Release
```

### Run (development)
```bash
cd Identity.Api
dotnet run
# HTTPS: https://localhost:7261  HTTP: http://localhost:5021
```

### Test

```bash
# Unit tests only — Debug is fine locally (no Angular build or Release-only artifact)
dotnet test --project Identity.Tests --configuration Debug -- --filter-trait "Category=Unit"

# E2E tests only (local)
ASPNETCORE_ENVIRONMENT=Development SqlConnectionStringBuilder__InitialCatalog=IdentityTest dotnet test --project Identity.Tests --configuration Debug -- --filter-trait "Category=E2E"

# All tests (local)
ASPNETCORE_ENVIRONMENT=Development SqlConnectionStringBuilder__InitialCatalog=IdentityTest dotnet test --project Identity.Tests --configuration Debug
```

E2E tests use Playwright (Chromium) against a real Kestrel server started in-process. They require the same User Secrets as `dotnet run` (database, Key Vault, etc.). See [../CLAUDE.md](../CLAUDE.md) for cross-cutting E2E operations: `ASPNETCORE_ENVIRONMENT` requirement, Azure CLI token warmup, and credential setup.

`xunit.runner.json` sets `parallelizeTestCollections: false` so all test collections run one at a time (8 concurrent AKV calls at startup time out under parallel load). The trade-off is that the combined run takes ~5-6 minutes instead of ~2.5 minutes with parallelism enabled.

`IdentityWebApplicationFactory.ConfigureWebHost` stubs out `IAvatarService` with a no-op (`NullAvatarService`) to prevent live Gravatar HTTP calls during test runs. This avoids 2-4 second delays per registration-based test caused by Gravatar API timeouts for test email addresses.

In CI the workflow sets `ASPNETCORE_ENVIRONMENT=CI`, which loads `appsettings.CI.json` (enables only `AzureCliCredential`) after the `azure/login` OIDC step instead of User Secrets. The `IdentityWebApplicationFactory` replaces the Serilog logger factory (to avoid the Elasticsearch sink) when `IsDevelopment()` is true. In CI, the Serilog logger is not replaced — instead, the Elasticsearch sink uses `BootstrapMethod.Failure`, which prevents a missing Elasticsearch node from failing startup.

`PlaywrightFixture.InitializeAsync` unconditionally calls `Microsoft.Playwright.Program.Main(["install", "chromium"])` to ensure Chromium is present. In CI, the workflow also runs a dedicated `pwsh playwright.ps1 install --with-deps chromium` step beforehand to install system-level browser dependencies.

### Code Quality

Both `Identity.Api` and `Identity.Tests` set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. All build warnings are errors — fix them before committing.

**Never set `TreatWarningsAsErrors` to `false` and never add suppressions to `.editorconfig` just to make your changes build.** If new code triggers a warning, fix the code — change the approach, restructure the type, rename the parameter, etc. These settings exist to enforce quality; working around them defeats the purpose.

`.editorconfig` suppresses StyleCop false positives for modern C# syntax:
- `SA1000` — target-typed `new()` (C# 9+)
- `SA1010` — collection expressions `[...]` (C# 12)
- `SA1011` — nullable array types `T[]?`
- `SA1313` — positional record parameters that define public properties

### Benchmarks
```bash
# Run all benchmarks (release build required)
dotnet run --project Identity.Benchmarks -c Release
```

### Mutation Testing (Stryker)
```bash
# Install Stryker globally (once)
dotnet tool install -g dotnet-stryker

# Run mutation tests (requires STRYKER_DASHBOARD_API_KEY env var for dashboard reporter)
STRYKER_DASHBOARD_API_KEY=<key> dotnet stryker --config-file stryker-config.json
```

Stryker targets `EmailSender.cs`, `GravatarService.cs`, `ConfigurationExtensions.cs`, and `EndpointRouteBuilderExtensions.cs`. Thresholds: high=80, low=60, break=50. The CI workflow runs mutation tests weekly (Monday 02:00 UTC) and on manual dispatch; results are uploaded as the `stryker-report` artifact.

**Critical `stryker-config.json` constraints — do not change without understanding why:**

| Setting | Value | Reason |
|---|---|---|
| `test-runner` | `mtp` | `Identity.Tests` uses `xunit.v3.mtp-v2` and `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>` — vstest will not work |
| `mutate` paths | Relative to the **project file** (`Identity.Api/Identity.Api.csproj`), not the solution/config root | e.g. `"EmailSender.cs"`, NOT `"Identity.Api/EmailSender.cs"` — wrong prefix silently ignores all mutants |
| `test-case-filter` | `"Category=Unit"` | The mutation CI workflow has no Azure login. E2E tests require Azure + database. Without this filter, E2E tests contaminate coverage capture and cause "not fully tested" for every mutant → score collapses to 0% |
| `coverage-analysis` | `"all"` | MTP runner only partially implements coverage analysis — `"perTest"` (per-test breakdown) is not yet implemented and fails with `Value cannot be null (Parameter 'collection')`. `"all"` is the working mode: filters out mutants not covered by any test, then runs all covering tests per mutant. |

**Why `ToTokenCredentialAsync` is excluded from mutation (`// Stryker disable all`):**
`DefaultAzureCredential` is a sealed class — it cannot be mocked. The method is covered only by E2E tests (which use `WebApplicationFactory<Program>`). Mutation testing of this method requires live Azure credentials and is excluded by design.

**Why all unit test classes have `[Collection(UnitCollection.Name)]`:**
Stryker's MTP coverage capture throws `Value cannot be null. (Parameter 'collection')` when test classes have no explicit collection name. `UnitCollection` is defined in `Identity.Tests/Infrastructure/TestCollections.cs`. Every `[Trait("Category", "Unit")]` test class must have `[Collection(UnitCollection.Name)]` to prevent this. E2E tests already use `[Collection(E2ECollection.Name)]`.

**Why `test-case-filter` does not restrict E2E tests in MTP mode:**
The MTP runner ignores `test-case-filter` entirely — it is a VSTest-only config option. The MTP runner uses `testUidFilter` (UID-based selection) internally. To exclude E2E tests from mutation runs, `PlaywrightFixture.InitializeAsync()` checks `STRYKER_MUTANT_FILE` (an env var the MTP runner always sets — value `-1` during coverage capture, mutant ID during mutations). When set, `InitializeAsync` returns immediately without starting `WebApplicationFactory` or Playwright. E2E tests then fail in Stryker's baseline run, and Stryker excludes always-failing tests from mutation coverage — leaving only unit tests to kill mutants. **Never remove this guard.**

### Load Tests
```bash
# Run load tests only (requires a running server — uses E2E infrastructure)
ASPNETCORE_ENVIRONMENT=Development SqlConnectionStringBuilder__InitialCatalog=IdentityTest dotnet test --project Identity.Tests --configuration Debug -- --filter-trait "Category=Load"
```

### SQL Database Project (schema deployment)
```bash
# Install sqlpackage once (if not already installed)
dotnet tool install --global microsoft.sqlpackage

# Build the dacpac
dotnet build Identity.Data/Identity.Data.sqlproj --configuration Release

# Deploy to a SQL Server instance
sqlpackage /Action:Publish /SourceFile:Identity.Data/bin/Release/Identity.Data.dacpac /TargetConnectionString:"<connection-string>"
```

### Publish
```bash
dotnet publish Identity.Api -c Release -r win-x86 --self-contained false -o ./publish
```

> **Note:** `-r win-x86` is required. The app runs on Azure App Service **Free tier**, which only supports 32-bit (x86) worker processes. Upgrading to Basic tier or above would enable x64.

## Deployment

The GitHub Actions workflow (`.github/workflows/main_crgolden-identity.yml`) triggers on pushes to `main`, pull request events (opened, synchronize, reopened), and manual dispatch.

**Build job:**
1. Builds the full solution via `dotnet build --no-incremental --configuration Release` — this includes `Identity.Data.sqlproj`, which produces the `.dacpac`
2. Runs unit tests with coverage using `dotnet-coverage collect ... -s "coverage.settings.xml"`, writing `coverage.xml`. `coverage.settings.xml` excludes `[GeneratedCode]`-decorated types (e.g. the NSwag-generated Gravatar client) from coverage metrics.
3. Logs in to Azure via OIDC, then deploys the E2E test database schema and runs E2E tests with `ASPNETCORE_ENVIRONMENT=CI`, writing `coverage-e2e.xml` with the same settings file
4. Publishes the web app (`-r win-x86 --self-contained false`) and uploads both the app artifact and the `.dacpac` artifact
5. Runs SonarCloud analysis, reading both `coverage.xml` and `coverage-e2e.xml`

**Deploy job** (runs after build):
1. Deploys the `.dacpac` to the production SQL Server via `SqlPackage` — builds the connection string from `DB_SERVER`, `DB_USERID`, `DB_PASSWORD`, and `DB_NAME` secrets
2. Deploys the web app to **Azure App Service** (`crgolden-identity`, Production slot) via Azure OIDC (`AZUREAPPSERVICE_CLIENTID_*`, `AZUREAPPSERVICE_TENANTID_*`, `AZUREAPPSERVICE_SUBSCRIPTIONID_*` secrets)

The database is always deployed before the app so the schema is in place when the app starts.

In production, `IdentityPasskeyOptions.ValidateOrigin` is not overridden (the default strict validation applies). In development, it is relaxed to allow `https://localhost:7261`.

## MCP Servers

Four MCP servers are configured in `.mcp.json`. `.claude/settings.json` explicitly allows `github` and `playwright`; `azure` and `sonarqube` are denied and require manual approval.

| Server | Package / Source | Auth |
|---|---|---|
| `github` | `@modelcontextprotocol/server-github` | `GITHUB_PERSONAL_ACCESS_TOKEN` env var |
| `azure` | `@azure/mcp@latest` | `DefaultAzureCredential` (`az login`) |
| `playwright` | `@playwright/mcp@latest` | None |
| `sonarqube` | `mcp/sonarqube` Docker image | `SONAR_TOKEN` env var; requires Docker Desktop |
