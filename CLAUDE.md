# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

### Identity User Type

All services use `IdentityUser<Guid>` (not the default `IdentityUser`). This must be kept consistent throughout — `UserManager<IdentityUser<Guid>>`, `SignInManager<IdentityUser<Guid>>`, `AddAspNetIdentity<IdentityUser<Guid>>()`, etc.

### Observability

- **Azure Monitor** — OpenTelemetry metrics and traces via `UseAzureMonitor()`
- **Serilog** — structured logging with an Elasticsearch sink (`logs-dotnet-identity` data stream) and console sink bootstrap. `Elastic.Serilog.Sinks` is pinned to exact version `[8.11.1]` to match the self-hosted Elasticsearch 8.x node; ECS 9.x templates use `synthetic_source_keep` which is not supported by Elasticsearch 8.x.
- Health checks endpoint at `/Health` with `DbContext` check; health check requests are filtered from traces

### Data Protection

Keys are persisted to **Azure Blob Storage** and protected with **Azure Key Vault** (via `PersistKeysToAzureBlobStorage` / `ProtectKeysWithAzureKeyVault`).

### Configuration

All sensitive values are retrieved at startup from **Azure Key Vault** using `DefaultAzureCredential`. The credential chain is narrowed per environment to prevent ambiguous or slow credential probing:

| Environment | Enabled credential | Typical use |
|---|---|---|
| `Development` | `VisualStudioCredential`, `SharedTokenCacheCredential` | Local dev via VS / VS Code |
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
# Unit tests only
dotnet test --project Identity.Tests --configuration Release -- --filter-trait "Category=Unit"

# E2E tests only (local) — Development environment loads User Secrets and uses VS/VS Code credentials
ASPNETCORE_ENVIRONMENT=Development dotnet test --project Identity.Tests --configuration Release -- --filter-trait "Category=E2E"

# All tests (local)
ASPNETCORE_ENVIRONMENT=Development dotnet test --project Identity.Tests --configuration Release
```

E2E tests use Playwright (Chromium) against a real Kestrel server started in-process. They require the same User Secrets as `dotnet run` (database, Key Vault, etc.).

In CI the workflow sets `ASPNETCORE_ENVIRONMENT=CI`, which loads `appsettings.CI.json` (enables only `AzureCliCredential`) after the `azure/login` OIDC step instead of User Secrets. The `IdentityWebApplicationFactory` replaces the Serilog logger factory (to avoid the Elasticsearch sink) for any non-Production environment; in `CI` the console logger is used instead.

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

# Run mutation tests against the five core source files
dotnet stryker --config-file stryker-config.json
```

Stryker targets `EmailSender.cs`, `GravatarService.cs`, `SecretClientExtensions.cs`, `ConfigurationExtensions.cs`, and `EndpointRouteBuilderExtensions.cs`. Thresholds: high=80, low=60, break=50. The CI workflow runs mutation tests weekly (Monday 02:00 UTC) and on manual dispatch; results are uploaded as the `stryker-report` artifact.

### Load Tests
```bash
# Run load tests only (requires a running server — uses E2E infrastructure)
ASPNETCORE_ENVIRONMENT=Development dotnet test --project Identity.Tests --configuration Release -- --filter-trait "Category=Load"
```

### SQL Database Project (schema deployment)
```bash
# Build the dacpac
dotnet build Identity.Data/Identity.Data.sqlproj --configuration Release

# Deploy to a SQL Server instance
sqlpackage /Action:Publish /SourceFile:Identity.Data/bin/Release/Identity.Data.dacpac /TargetConnectionString:"<connection-string>"
```

### Publish
```bash
dotnet publish Identity.Api -c Release -o ./publish
```

## Deployment

The GitHub Actions workflow (`.github/workflows/main_crgolden-identity.yml`) triggers on pushes to `main`, pull request events (opened, synchronize, reopened), and manual dispatch.

**Build job:**
1. Builds the full solution via `dotnet build --no-incremental --configuration Release` — this includes `Identity.Data.sqlproj`, which produces the `.dacpac`
2. Runs unit tests with coverage using `dotnet-coverage collect ... -s "coverage.settings.xml"`, writing `coverage.xml`. `coverage.settings.xml` excludes `[GeneratedCode]`-decorated types (e.g. the NSwag-generated Gravatar client) from coverage metrics.
3. Logs in to Azure via OIDC, then deploys the E2E test database schema and runs E2E tests with `ASPNETCORE_ENVIRONMENT=CI`, writing `coverage-e2e.xml` with the same settings file
4. Publishes the web app (`-r win-x64 --self-contained false`) and uploads both the app artifact and the `.dacpac` artifact
5. Runs SonarCloud analysis, reading both `coverage.xml` and `coverage-e2e.xml`

**Deploy job** (runs after build):
1. Deploys the `.dacpac` to the production SQL Server via `SqlPackage` — builds the connection string from `DB_SERVER`, `DB_USERID`, `DB_PASSWORD`, and `DB_NAME` secrets
2. Deploys the web app to **Azure App Service** (`crgolden-identity`, Production slot) via Azure OIDC (`AZUREAPPSERVICE_CLIENTID_*`, `AZUREAPPSERVICE_TENANTID_*`, `AZUREAPPSERVICE_SUBSCRIPTIONID_*` secrets)

The database is always deployed before the app so the schema is in place when the app starts.

In production, `IdentityPasskeyOptions.ValidateOrigin` is not overridden (the default strict validation applies). In development, it is relaxed to allow `https://localhost:7261`.

## MCP Servers

Five MCP servers are configured in `.mcp.json`. `.claude/settings.json` explicitly allows `github` and `playwright`; `azure` and `sonarqube` are denied and require manual approval; `ef-dacpac-mcp` is not listed (defaults to prompt-on-use).

| Server | Package / Source | Auth |
|---|---|---|
| `github` | `@modelcontextprotocol/server-github` | `GITHUB_PERSONAL_ACCESS_TOKEN` env var |
| `azure` | `@azure/mcp@latest` | `DefaultAzureCredential` (`az login`) |
| `playwright` | `@playwright/mcp@latest` | None |
| `sonarqube` | `mcp/sonarqube` Docker image | `SONAR_TOKEN` env var; requires Docker Desktop |
| `ef-dacpac-mcp` | `tools/ef-dacpac-mcp/ef-dacpac-mcp.csproj` (not currently present in repo) | None |

### ef-dacpac-mcp

A custom MCP server that bridges the EF Core / DACPAC schema strategy. The source project is referenced in `.mcp.json` at `tools/ef-dacpac-mcp/ef-dacpac-mcp.csproj` but the `tools/` directory is not currently present in the repository — the server will fail to start until the source is restored.

**EF Core tools** (`dotnet-ef` global tool required):
- `ef_list_migrations` — shows applied/pending migrations
- `ef_migration_script` — generates idempotent SQL for a migration range
- `ef_dbcontext_info` — provider, connection source, migrations assembly
- `ef_dbcontext_list` — all DbContext types in the project

**DACPAC tools** (`sqlpackage` global tool required):
- `dacpac_build` — builds `.dacpac` from `Identity.Data.sqlproj`
- `dacpac_deploy_report` — raw XML report of changes sqlpackage would apply
- `dacpac_drift_check` — human-readable drift summary between dacpac and a live database
- `dacpac_script` — full T-SQL deploy script for pre-deploy review

Install prerequisites:
```bash
dotnet tool install -g dotnet-ef
dotnet tool install -g microsoft.sqlpackage
```
