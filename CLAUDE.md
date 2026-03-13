# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Solution Structure

The solution (`Identity.slnx`) contains two projects:

- **`Identity/`** — ASP.NET Core 10 Razor Pages web application (the main app), also houses `ApplicationDbContext` and EF Core migrations
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

`ApplicationDbContext` (in `Identity/ApplicationDbContext.cs`) implements three interfaces simultaneously:
- `IdentityDbContext<IdentityUser<Guid>, ...>` — ASP.NET Identity tables
- `IConfigurationDbContext` — Duende IdentityServer clients/resources
- `IPersistedGrantDbContext` — Duende IdentityServer grants/sessions

Migrations live in `Identity/Migrations/` and are registered with `.MigrationsAssembly(assembly)` in `Program.cs`.

### Passkey Endpoints

Two minimal API endpoints are registered in `PasskeyEndpointRouteBuilderExtensions.cs` (called via `app.MapAdditionalIdentityEndpoints()`):
- `POST /Account/PasskeyCreationOptions` — returns WebAuthn creation options JSON
- `POST /Account/PasskeyRequestOptions` — returns WebAuthn request options JSON

Both require antiforgery validation. The Passkey Razor Pages live in `Areas/Identity/Pages/Account/Manage/`.

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
cd Identity
dotnet run
# HTTPS: https://localhost:7261  HTTP: http://localhost:5021
```

### Test
```bash
dotnet test
```

### EF Core Migrations
Run from the solution root; `ApplicationDbContext` and migrations are now in the `Identity` web project itself:

```bash
dotnet ef migrations add <MigrationName> --project Identity
dotnet ef database update --project Identity
```

### Publish
```bash
dotnet publish -c Release -o ./publish
```

## Deployment

The GitHub Actions workflow (`.github/workflows/main_crgolden-identity.yml`) deploys to **Azure App Service** (`crgolden-identity`, Production slot) on every push to `main`. It builds with `dotnet build --configuration Release` and publishes with `dotnet publish`.

In production, `IdentityPasskeyOptions.ValidateOrigin` is not overridden (the default strict validation applies). In development, it is relaxed to allow `https://localhost:7261`.
