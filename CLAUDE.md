# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Solution Structure

The solution (`Identity.slnx`) contains three projects:

- **`Identity.Api/`** — ASP.NET Core 10 Razor Pages web application (the main app), also houses `ApplicationDbContext` and EF Core migrations
- **`Identity.Data/`** — SQL Server Database Project (SSDT); the schema source of truth for production, builds to a `.dacpac`
- **`Identity.Tests/`** — xUnit v3 test project (uses `Microsoft.AspNetCore.Mvc.Testing`, `Moq`)

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

Migrations live in `Identity.Api/Migrations/` and are registered with `.MigrationsAssembly(assembly)` in `Program.cs`.

### Database Change Strategy

Two separate mechanisms are used depending on environment:

- **Development** — EF Core migrations (`dotnet ef migrations add` / `dotnet ef database update`) are used to evolve the local database schema during development.
- **Production** — The **`Identity.Data` SQL project** is the source of truth. Schema changes are deployed via `.dacpac` (built from `Identity.Data.sqlproj` and published with `SqlPackage`). Migrations are **not** run against production.

When making a schema change, update both the EF Core migration (for dev) and the corresponding table definition in `Identity.Data/` (for prod).

### Passkey Endpoints

Two minimal API endpoints are registered in `PasskeyEndpointRouteBuilderExtensions.cs` (called via `app.MapAdditionalIdentityEndpoints()`):
- `POST /Account/PasskeyCreationOptions` — returns WebAuthn creation options JSON
- `POST /Account/PasskeyRequestOptions` — returns WebAuthn request options JSON

Both require antiforgery validation. The Passkey Razor Pages live in `Pages/Account/Manage/`.

### Identity User Type

All services use `IdentityUser<Guid>` (not the default `IdentityUser`). This must be kept consistent throughout — `UserManager<IdentityUser<Guid>>`, `SignInManager<IdentityUser<Guid>>`, `AddAspNetIdentity<IdentityUser<Guid>>()`, etc.

### Observability

- **Azure Monitor** — OpenTelemetry metrics and traces via `UseAzureMonitor()`
- **Serilog** — structured logging with an Elasticsearch sink (`logs-dotnet-identity` data stream) and console sink bootstrap
- Health checks endpoint at `/Health` with `DbContext` check; health check requests are filtered from traces

### Data Protection

Keys are persisted to **Azure Blob Storage** and protected with **Azure Key Vault** (via `PersistKeysToAzureBlobStorage` / `ProtectKeysWithAzureKeyVault`).

### Configuration

All sensitive values are retrieved at startup from **Azure Key Vault** using `DefaultAzureCredential`. Non-secret infrastructure values are set via `appsettings.json` (nulled out) and supplied through User Secrets (development) or environment/Azure App Configuration (production):

```json
{
  "ElasticsearchNode": "<Elasticsearch node URI>",
  "KeyVaultUri": "<Azure Key Vault URI>",
  "BlobUri": "<Azure Blob Storage URI for Data Protection keys>",
  "DataProtectionKeyIdentifier": "<Azure Key Vault key URI for Data Protection>",
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
dotnet test
```

### EF Core Migrations (development only — not used in production)
Run from the solution root:

```bash
dotnet ef migrations add <MigrationName> --project Identity.Api
dotnet ef database update --project Identity.Api
```

### SQL Database Project (production schema deployment)
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

The GitHub Actions workflow (`.github/workflows/main_crgolden-identity.yml`) runs on every push to `main`:

1. Builds `Identity.Data.sqlproj` to produce a `.dacpac`
2. Builds and tests the .NET solution (with SonarCloud analysis)
3. Deploys the `.dacpac` to the production SQL Server via `SqlPackage` (uses `SQL_CONNECTION_STRING` secret)
4. Deploys the web app to **Azure App Service** (`crgolden-identity`, Production slot)

The database is always deployed before the app so the schema is in place when the app starts.

In production, `IdentityPasskeyOptions.ValidateOrigin` is not overridden (the default strict validation applies). In development, it is relaxed to allow `https://localhost:7261`.
