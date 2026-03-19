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
| Schema deployment | SQL Database Project (dacpac) |
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
cd Identity.Api
dotnet user-secrets set "ElasticsearchNode" "<your-elasticsearch-node-uri>"
dotnet user-secrets set "KeyVaultUri" "<your-key-vault-uri>"
dotnet user-secrets set "BlobUri" "<your-blob-storage-uri>"
dotnet user-secrets set "DataProtectionKeyIdentifier" "<your-key-vault-key-uri>"
dotnet user-secrets set "SqlConnectionStringBuilder:DataSource" "<your-sql-server>"
dotnet user-secrets set "APPLICATIONINSIGHTS_CONNECTION_STRING" "<your-app-insights-connection-string>"
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

### 2. Set Up the Database

The schema is managed entirely via the `Identity.Data` SQL Server Database Project (dacpac).

**Development** — build and publish the dacpac to your local SQL Server:

```bash
dotnet build Identity.Data/Identity.Data.sqlproj --configuration Release
sqlpackage /Action:Publish /SourceFile:Identity.Data/bin/Release/Identity.Data.dacpac /TargetConnectionString:"<your-local-connection-string>"
```

**Production** — the schema is deployed automatically via the CI/CD pipeline (see [Deployment](#deployment) below).

### 3. Run

```bash
cd Identity.Api
dotnet run
```

App is available at `https://localhost:7261` (HTTPS) or `http://localhost:5021` (HTTP).

## Project Structure

```
Identity.Api/        # ASP.NET Core 10 Razor Pages web app and DbContext
Identity.Data/       # SQL Server Database Project — schema source of truth, builds to .dacpac
Identity.Tests/      # xUnit v3 test project: unit tests (Moq), E2E tests (Playwright/Chromium), load tests
Identity.Benchmarks/ # BenchmarkDotNet microbenchmarks for authentication hot paths
```

## Commands

```bash
# Build
dotnet build

# Unit tests only
dotnet test --project Identity.Tests --configuration Release -- --filter-trait "Category=Unit"

# E2E tests (local) — requires Development environment for User Secrets
ASPNETCORE_ENVIRONMENT=Development dotnet test --project Identity.Tests --configuration Release -- --filter-trait "Category=E2E"

# All tests (unit + E2E, local)
ASPNETCORE_ENVIRONMENT=Development dotnet test --project Identity.Tests --configuration Release

# Load tests (requires E2E infrastructure — database + Key Vault)
ASPNETCORE_ENVIRONMENT=Development dotnet test --project Identity.Tests --configuration Release -- --filter-trait "Category=Load"

# Build dacpac (schema deployment)
dotnet build Identity.Data/Identity.Data.sqlproj --configuration Release

# Deploy dacpac to production SQL Server
sqlpackage /Action:Publish /SourceFile:Identity.Data/bin/Release/Identity.Data.dacpac /TargetConnectionString:"<connection-string>"

# Run benchmarks
dotnet run --project Identity.Benchmarks -c Release

# Run mutation tests (Stryker — slow, runs against 5 core source files)
dotnet stryker --config-file stryker-config.json

# Publish web app (add -r win-x64 --self-contained false when targeting Windows App Service)
dotnet publish Identity.Api -c Release -o ./publish
```

## AI Assistant Integration (Claude Code)

This repo ships a `.mcp.json` that configures five MCP servers for use with Claude Code. `.claude/settings.json` explicitly allows `github` and `playwright`; `azure` and `sonarqube` are denied by default; `ef-dacpac-mcp` prompts on first use.

| Server | Source | Purpose |
|---|---|---|
| `github` | `@modelcontextprotocol/server-github` (official) | Search issues/PRs, create issues, review code |
| `azure` | `@azure/mcp@latest` (Microsoft official) | Query Key Vault, App Service, Blob Storage |
| `playwright` | `@playwright/mcp@latest` (Microsoft official) | Drive browser sessions for E2E investigation |
| `sonarqube` | `mcp/sonarqube` Docker image (SonarSource official) | Query issues, quality gates, security hotspots |
| `ef-dacpac-mcp` | `tools/ef-dacpac-mcp/` (this repo) | EF Core migration management and DACPAC schema-drift analysis |

### Required environment variables

```powershell
$env:GITHUB_PERSONAL_ACCESS_TOKEN = "ghp_..."   # GitHub PAT with repo scope
$env:SONAR_TOKEN = "squ_..."                     # SonarCloud user token
```

`azure` uses `DefaultAzureCredential` (same as the app — `az login` covers it).
`sonarqube` requires Docker Desktop.

## Deployment

The GitHub Actions workflow triggers on pushes to `main`, pull requests, and manual dispatch.

The workflow also runs on a **weekly schedule** (Monday 02:00 UTC).

**Build job** — runs on every trigger:
1. Builds the full solution (`dotnet build --configuration Release`), which also compiles `Identity.Data.sqlproj` and produces the `.dacpac`
2. Runs unit tests with coverage
3. Logs in to Azure via OIDC, deploys the E2E test database schema, then runs E2E tests with `ASPNETCORE_ENVIRONMENT=CI` — the `CI` environment loads `appsettings.CI.json` which enables only `AzureCliCredential`, so the in-process test server authenticates against Key Vault using the workflow's OIDC identity
4. Runs load tests (on `schedule` or `workflow_dispatch` only)
5. Runs SonarCloud analysis, publishes the web app, and uploads both artifacts

**Mutation job** — runs on `schedule` or `workflow_dispatch`:
- Runs Stryker mutation testing against five core source files; uploads the HTML/JSON report as `stryker-report`

**Deploy job** — runs after a successful build:
1. Deploys the `.dacpac` to the production SQL Server via `SqlPackage`
2. Deploys the web app to **Azure App Service** `crgolden-identity` (Production slot) via Azure OIDC

Database schema is always deployed before the app to ensure a valid schema is in place when the app starts.
