# Identity

[![Build and deploy ASP.Net Core app to Azure Web App - crgolden-identity](https://github.com/crgolden/Identity/actions/workflows/main_crgolden-identity.yml/badge.svg)](https://github.com/crgolden/Identity/actions/workflows/main_crgolden-identity.yml)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=crgolden_Identity&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=crgolden_Identity)

[![Mutation testing badge](https://img.shields.io/endpoint?style=flat&url=https%3A%2F%2Fbadge-api.stryker-mutator.io%2Fgithub.com%2Fcrgolden%2FIdentity%2Fmain)](https://dashboard.stryker-mutator.io/reports/github.com/crgolden/Identity/main)

A standalone **OpenID Connect Identity Provider** built with [Duende IdentityServer](https://duendesoftware.com/products/identityserver) and ASP.NET Core Identity, deployed to Azure App Service.

## Sibling Applications

Identity is the **authorization server** for a five-app system. It issues all access tokens; the other four apps validate them.

| Repo | Role | How Identity interacts |
|---|---|---|
| [Inventory](https://github.com/crgolden/Inventory) | Angular SPA + ASP.NET Core BFF | OIDC client; the BFF holds the session and obtains scoped access tokens for `manuals` and `products` |
| [Manuals](https://github.com/crgolden/Manuals) | Azure OpenAI chat API | Resource server — validates JWTs issued by Identity (scope `manuals`) |
| [Products](https://github.com/crgolden/Products) | OData v4 product catalog API | Resource server — validates JWTs issued by Identity (scope `products`) |
| [Infrastructure](https://github.com/crgolden/Infrastructure) | Health monitoring dashboard | Polls Identity's `/health` endpoint |

## Features

- **OIDC/OAuth2** authorization server via Duende IdentityServer 8
- **Local accounts** with email confirmation (via Azure Service Bus)
- **Google** external login (OpenID Connect)
- **Passkeys / WebAuthn** (ASP.NET Core Identity built-in support)
- **TOTP two-factor authentication** with recovery codes
- **Gravatar** profile avatars
- **Consent screen** — OAuth2 scope approval UI with allow/deny and remember-consent
- **Grants management** — view and revoke previously granted client permissions
- **Device authorization flow** — user code entry and scope consent for constrained devices
- **CIBA** — client-initiated backchannel authentication request display
- **Server-side session management** — view and remove active user sessions
- **Redirect page** — loading page for native client redirects
- **Diagnostics page** — current user claims and tokens (development only)
- **OpenTelemetry** → Grafana Alloy (OTLP metrics & traces — including Duende IdentityServer built-in signals)
- **Serilog** structured logging with Elasticsearch sink
- **Data Protection** keys persisted to Azure Blob Storage, protected by Azure Key Vault

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 (Razor Pages) |
| Auth server | Duende IdentityServer 8 |
| Identity | ASP.NET Core Identity (`IdentityUser<Guid>`) |
| Database | SQL Server via EF Core 10 |
| Schema deployment | SQL Database Project (dacpac) |
| Email | Azure Service Bus |
| Pictures | Gravatar API |
| Observability | OpenTelemetry → Grafana Alloy (OTLP), Serilog → Elasticsearch |
| Hosting | Azure App Service |
| Secrets | Azure Key Vault |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server instance
- Azure subscription (Key Vault, Blob Storage, App Service)
- Elasticsearch cluster
- Azure Service Bus namespace (production) or connection string (non-production)
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
dotnet user-secrets set "AlloyEndpoint" "<your-grafana-alloy-otlp-endpoint>"
```

The following secrets must be present in Azure Key Vault. In production they are fetched at startup via `DefaultAzureCredential` (see `SecretClientExtensions.GetIdentitySecrets`):

| Secret Name | Description |
|---|---|
| `GoogleClientId` | Google OAuth client ID |
| `GoogleClientSecret` | Google OAuth client secret |
| `GravatarApiSecretKey` | Gravatar API key |
| `IdentitySqlServerUserId` | SQL Server login user |
| `IdentitySqlServerPassword` | SQL Server login password |
| `ElasticsearchUsername` | Elasticsearch username |
| `ElasticsearchPassword` | Elasticsearch password |
| `ReCAPTCHASiteKey` | Google reCAPTCHA v3 site key |
| `ReCAPTCHASecretKey` | Google reCAPTCHA v3 secret key |
| `AdminEmail` | Admin-role account email |
| `TestEmail` | E2E/smoke test account email |

The Azure Service Bus namespace is supplied as the `ServiceBusNamespace` **configuration** value (not fetched through `GetIdentitySecrets`).

### 2. Set Up the Database

The schema is managed entirely via the `Identity.Data` SQL Server Database Project (dacpac).

**Development** — build and publish the dacpac to your local SQL Server:

```bash
# Install sqlpackage once (if not already installed)
dotnet tool install --global microsoft.sqlpackage

dotnet build Identity.Data/Identity.Data.sqlproj --configuration Release
sqlpackage /Action:Publish /SourceFile:Identity.Data/bin/Release/Identity.Data.dacpac /TargetConnectionString:"<your-local-connection-string>"
```

**Production** — the schema is deployed automatically via the CI/CD pipeline (see [Deployment](#deployment) below).

### 3. Run

```bash
cd Identity
dotnet run
```

App is available at `https://localhost:7261` (the only profile defined in `launchSettings.json`).

## Project Structure

```
Identity/            # ASP.NET Core 10 Razor Pages web app and DbContext
Identity.Data/       # SQL Server Database Project — schema source of truth, builds to .dacpac
Identity.Tests.Unit/ # xUnit v3 test project: unit tests (Moq)
Identity.Tests.E2E/  # xUnit v3 test project: E2E tests (Playwright/Chromium), load tests, smoke tests
Identity.Benchmarks/ # BenchmarkDotNet microbenchmarks for authentication hot paths
```

## Commands

> **Shell note:** commands that set environment variables inline (e.g. `ASPNETCORE_ENVIRONMENT=...`) use bash syntax. On Windows, use Git Bash, WSL, or set the variables separately before running the `dotnet` command.

```bash
# Build
dotnet build

# Unit tests only
dotnet test --project Identity.Tests.Unit --configuration Release -- --filter-trait "Category=Unit"

# Unit tests with TRX report (written to TestResults/unit-tests.trx)
dotnet test --project Identity.Tests.Unit --configuration Release -- --filter-trait "Category=Unit" --report-trx --report-trx-filename unit-tests.trx

# E2E tests (local) — requires Development environment for User Secrets
ASPNETCORE_ENVIRONMENT=Development SqlConnectionStringBuilder__InitialCatalog=IdentityTest dotnet test --project Identity.Tests.E2E --configuration Release -- --filter-trait "Category=E2E"

# Load tests (requires E2E infrastructure — database + Key Vault)
ASPNETCORE_ENVIRONMENT=Development SqlConnectionStringBuilder__InitialCatalog=IdentityTest dotnet test --project Identity.Tests.E2E --configuration Release -- --filter-trait "Category=Load"

# Install sqlpackage once (if not already installed)
dotnet tool install --global microsoft.sqlpackage

# Build dacpac (schema deployment)
dotnet build Identity.Data/Identity.Data.sqlproj --configuration Release

# Deploy dacpac to production SQL Server
sqlpackage /Action:Publish /SourceFile:Identity.Data/bin/Release/Identity.Data.dacpac /TargetConnectionString:"<connection-string>"

# Run benchmarks
dotnet run --project Identity.Benchmarks -c Release

# Run mutation tests (Stryker — slow; see stryker-config.json for the mutated files)
dotnet stryker --config-file stryker-config.json

# Publish web app (-r win-x86 required: Azure App Service Free tier supports 32-bit only)
dotnet publish Identity -c Release -r win-x86 --self-contained false -o ./publish
```

## AI Assistant Integration (Claude Code)

Five MCP servers are configured in `.mcp.json` at the `crgolden/` workspace root (`github`, `azure`, `sonarqube`, `playwright`, `chrome-devtools`). `crgolden/.claude/settings.json` governs which are auto-allowed; `chrome-devtools` tool calls require manual approval.

| Server | Source | Purpose |
|---|---|---|
| `github` | `@modelcontextprotocol/server-github` (official) | Search issues/PRs, create issues, review code |
| `azure` | `@azure/mcp@latest` (Microsoft official) | Query Key Vault, App Service, Blob Storage |
| `playwright` | `@playwright/mcp@latest` (Microsoft official) | Drive browser sessions for E2E investigation |
| `sonarqube` | `mcp/sonarqube` Docker image (SonarSource official) | Query issues, quality gates, security hotspots |
| `chrome-devtools` | `chrome-devtools-mcp@latest` | Drive Chrome DevTools sessions |

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
1. Builds the full solution (`dotnet build --no-incremental --configuration Release`), which also compiles `Identity.Data.sqlproj` and produces the `.dacpac`
2. Runs unit tests with coverage
3. Deploys the E2E test database schema (`SqlPackage`), then runs E2E tests with `ASPNETCORE_ENVIRONMENT=CI`. There is no `appsettings.CI.json` — the test server's secrets (Google, Gravatar, reCAPTCHA, SQL login, Service Bus) are injected as environment variables, and the production-only Key Vault fetch path is never reached under the `CI` environment
4. Reports E2E results to Azure DevOps and Azure Monitor; uploads test results and Playwright failure artifacts
5. Runs load tests (on `schedule` or `workflow_dispatch` only)
6. Runs SonarCloud analysis, publishes the web app (`-r win-x86`), and uploads the app, `.dacpac`, and test binaries

**Deploy job** — runs after a successful build (skipped on `pull_request`):
1. Deploys the `.dacpac` to the production SQL Server via `SqlPackage`
2. Deploys the web app to **Azure App Service** `crgolden-identity` (Production slot) via Azure OIDC

Database schema is always deployed before the app to ensure a valid schema is in place when the app starts.

**Smoke job** — runs after deploy, only on `main`:
- Downloads the test binaries and runs the `Category=Smoke` suite against the deployed site (`SMOKE_BASE_URL`); reports results to Azure DevOps and Azure Monitor

**Mutation job** — runs on `schedule` or `workflow_dispatch`:
- Runs Stryker mutation testing over the source files listed in `stryker-config.json` (`GravatarService.cs`, `ConfigurationExtensions.cs`, `EndpointRouteBuilderExtensions.cs`), and uploads the HTML/JSON report as `stryker-report`
