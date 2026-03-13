# Identity

[![Build and deploy ASP.Net Core app to Azure Web App - crgolden-identity](https://github.com/crgolden/Identity/actions/workflows/main_crgolden-identity.yml/badge.svg)](https://github.com/crgolden/Identity/actions/workflows/main_crgolden-identity.yml)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=crgolden_Identity&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=crgolden_Identity)

A standalone **OpenID Connect Identity Provider** built with [Duende IdentityServer](https://duendesoftware.com/products/identityserver) and ASP.NET Core Identity, deployed to Azure App Service.

## Features

- **OIDC/OAuth2** authorization server via Duende IdentityServer 7
- **Local accounts** with email confirmation (via [Resend](https://resend.com))
- **Google** external login (OpenID Connect)
- **Passkeys / WebAuthn** (ASP.NET Core Identity built-in support)
- **TOTP two-factor authentication** with recovery codes
- **Gravatar** profile avatars
- **Azure Monitor** (OpenTelemetry metrics & traces)
- **Serilog** structured logging with Elasticsearch sink
- **Data Protection** keys persisted to Azure Blob Storage, protected by Azure Key Vault

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 (Razor Pages) |
| Auth server | Duende IdentityServer 7 |
| Identity | ASP.NET Core Identity (`IdentityUser<Guid>`) |
| Database | SQL Server via EF Core 10 |
| Email | Resend |
| Avatars | Gravatar API |
| Observability | Azure Monitor, OpenTelemetry, Serilog, Elasticsearch |
| Hosting | Azure App Service |
| Secrets | Azure Key Vault |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server instance
- Azure subscription (Key Vault, Blob Storage, App Service)
- Elasticsearch cluster
- Resend API token
- Google OAuth 2.0 credentials

## Getting Started

### 1. Configure User Secrets

```bash
cd Identity
dotnet user-secrets set "ElasticsearchNode" "<your-elasticsearch-node-uri>"
dotnet user-secrets set "KeyVaultUri" "<your-key-vault-uri>"
dotnet user-secrets set "BlobUri" "<your-blob-storage-uri>"
dotnet user-secrets set "DataProtectionKeyIdentifier" "<your-key-vault-key-uri>"
dotnet user-secrets set "SqlConnectionStringBuilder:DataSource" "<your-sql-server>"
```

The following secrets must be present in Azure Key Vault (fetched at startup via `DefaultAzureCredential`):

| Secret Name | Description |
|---|---|
| `GravatarApiSecretKey` | Gravatar API key |
| `ElasticsearchUsername` | Elasticsearch username |
| `ElasticsearchPassword` | Elasticsearch password |
| `SqlServerUserId` | SQL Server login user |
| `SqlServerPassword` | SQL Server login password |
| `GoogleClientId` | Google OAuth client ID |
| `GoogleClientSecret` | Google OAuth client secret |
| `ResendApiToken` | Resend API token |

### 2. Apply Migrations

```bash
dotnet ef database update --project Identity
```

### 3. Run

```bash
cd Identity
dotnet run
```

App is available at `https://localhost:7261` (HTTPS) or `http://localhost:5021` (HTTP).

## Project Structure

```
Identity/               # ASP.NET Core web app, DbContext, and migrations
Identity.Tests/         # xUnit v3 test project
```

## Commands

```bash
# Build
dotnet build

# Test
dotnet test

# Add a migration
dotnet ef migrations add <MigrationName> --project Identity

# Update database
dotnet ef database update --project Identity

# Publish
dotnet publish -c Release -o ./publish
```

## Deployment

Pushes to `main` automatically build and deploy to the **Azure App Service** `crgolden-identity` (Production slot) via GitHub Actions.
