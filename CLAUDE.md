# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Solution Structure

The solution (`Identity.slnx`) contains two projects:

- **`Identity/`** — ASP.NET Core 10 Razor Pages web application (the main app)
- **`Identity.Data/`** — Class library housing `ApplicationDbContext` and EF Core migrations

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

## Key Architecture Points

### Single DbContext for Everything

`ApplicationDbContext` (in `Identity.Data`) implements three interfaces simultaneously:
- `IdentityDbContext<IdentityUser<Guid>, ...>` — ASP.NET Identity tables
- `IConfigurationDbContext` — Duende IdentityServer clients/resources
- `IPersistedGrantDbContext` — Duende IdentityServer grants/sessions

Migrations live in `Identity.Data/Migrations/` and are registered with `.MigrationsAssembly(assembly)` in `Program.cs`.

### Passkey Endpoints

Two minimal API endpoints are registered in `PasskeyEndpointRouteBuilderExtensions.cs` (called via `app.MapAdditionalIdentityEndpoints()`):
- `POST /Account/PasskeyCreationOptions` — returns WebAuthn creation options JSON
- `POST /Account/PasskeyRequestOptions` — returns WebAuthn request options JSON

Both require antiforgery validation. The Passkey Razor Pages live in `Areas/Identity/Pages/Account/Manage/`.

### Identity User Type

All services use `IdentityUser<Guid>` (not the default `IdentityUser`). This must be kept consistent throughout — `UserManager<IdentityUser<Guid>>`, `SignInManager<IdentityUser<Guid>>`, `AddAspNetIdentity<IdentityUser<Guid>>()`, etc.

### Configuration

Sensitive values are `null` in `appsettings.json` and must be set via User Secrets (development) or Azure App Configuration/Key Vault (production):

```json
{
  "ConnectionStrings:DefaultConnection": "<SQL Server connection string>",
  "ResendClientOptions:ApiToken": "<Resend API token>",
  "Authentication:Google:ClientId": "<Google OAuth client ID>",
  "Authentication:Google:ClientSecret": "<Google OAuth client secret>"
}
```

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

### EF Core Migrations
Run from the solution root; specify the `Identity.Data` project as the migrations project and `Identity` as the startup project:

```bash
dotnet ef migrations add <MigrationName> --project Identity.Data --startup-project Identity
dotnet ef database update --project Identity.Data --startup-project Identity
```

### Publish
```bash
dotnet publish -c Release -o ./publish
```

## Deployment

The GitHub Actions workflow (`.github/workflows/main_crgolden-identity.yml`) deploys to **Azure App Service** (`crgolden-identity`, Production slot) on every push to `main`. It builds with `dotnet build --configuration Release` and publishes with `dotnet publish`.

In production, `IdentityPasskeyOptions.ValidateOrigin` is not overridden (the default strict validation applies). In development, it is relaxed to allow `https://localhost:7261`.
