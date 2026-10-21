# Identity

[![Build and deploy ASP.Net Core app to Azure Web App - crgolden-identity](https://github.com/crgolden/Identity/actions/workflows/main_crgolden-identity.yml/badge.svg)](https://github.com/crgolden/Identity/actions/workflows/main_crgolden-identity.yml)

[![Mutation testing badge](https://img.shields.io/endpoint?style=flat&url=https%3A%2F%2Fbadge-api.stryker-mutator.io%2Fgithub.com%2Fcrgolden%2FIdentity%2Fmain)](https://dashboard.stryker-mutator.io/reports/github.com/crgolden/Identity/main)

[![Quality gate](https://sonarcloud.io/api/project_badges/quality_gate?project=crgolden_Identity)](https://sonarcloud.io/summary/new_code?id=crgolden_Identity)

A standalone **OpenID Connect identity provider** built with [Duende IdentityServer](https://duendesoftware.com/products/identityserver)
and ASP.NET Core Identity. It signs users in (passwords, passkeys, Google, two-factor) and issues the tokens other
applications validate.

How the parts fit together (routing, authentication flows, the data layer, observability, security, CI/CD):
[ARCHITECTURE.md](ARCHITECTURE.md). The visual design rules: [DESIGN.md](DESIGN.md). Every test tier, the
fixtures and mutation testing: [TESTING.md](TESTING.md).

## Features

- **OIDC/OAuth2** authorization server via Duende IdentityServer 8
- **Local accounts** with email confirmation (sent through an Azure Service Bus queue)
- **Google** external login (OpenID Connect)
- **Passkeys / WebAuthn** (ASP.NET Core Identity's built-in support)
- **TOTP two-factor authentication** with recovery codes
- **Gravatar** profile avatars
- **reCAPTCHA v3**: bot scoring on sign-in and registration, failing closed when unreachable
- **Consent**, **grants management**, **device authorization**, **CIBA** and **server-side session** pages
- **Admin UI** (`/Admin`, `Admin` role): CRUD over every IdentityServer configuration and operational store, plus
  users and roles. The SAML entity editors are storage only; Identity does not serve the SAML protocol
- **OpenTelemetry** traces and metrics, **Serilog** to Elasticsearch, and **Data Protection** keys in Blob Storage
  protected by Key Vault, in production

## Technologies

| Layer | Technology |
|---|---|
| Framework | .NET 10 (`global.json`), ASP.NET Core Razor Pages |
| Auth server | Duende IdentityServer 8 |
| Identity | ASP.NET Core Identity (`IdentityUser<Guid>`) |
| Database | SQL Server through EF Core 10; schema in a SQL Database Project (`Identity.Data`, a dacpac) |
| Email | Azure Service Bus (a separate worker delivers the queue) |
| Front-end scripts | vanilla JavaScript in `Identity/wwwroot/js`, linted with ESLint from the repo root (Node 24, `.nvmrc`) |
| Tests | xUnit v3, Reqnroll with Playwright (E2E), Stryker.NET, ReSharper `jb inspectcode`, SonarCloud |
| Hosting | Azure App Service; Key Vault references |

## Prerequisites

- The .NET 10 SDK, Node.js 24, and SqlPackage (`dotnet tool install --global microsoft.sqlpackage`).
- A GitHub personal access token with the `read:packages` scope: `NuGet.Config` and `.npmrc` read the `crgolden`
  GitHub Packages feed, which asks for a token even for a public package. Store it outside the repo:

  ```powershell
  dotnet nuget update source GitHub --username <your GitHub user> --password <token> --store-password-in-clear-text --configfile "$env:APPDATA\NuGet\NuGet.Config"
  ```

  and in your user `~/.npmrc`: `//npm.pkg.github.com/:_authToken=<token>`.
- SQL Server: LocalDB (`appsettings.Development.json`'s default) or any instance.
- A Google OAuth 2.0 client and a reCAPTCHA v3 site and secret key.
- An Azure Service Bus namespace or connection string with an `email` queue, to send confirmation email.
- A trusted ASP.NET Core development certificate (`dotnet dev-certs https --trust`).

## Clone and build

```powershell
git clone https://github.com/crgolden/Identity.git
cd Identity
dotnet tool restore
npm ci
dotnet build
```

## Configuration

Settings come from `appsettings*.json`, then User Secrets in Development (id
`aspnet-Identity-149346d0-999f-4a74-8ff7-2a92d39790f2`), then environment variables. A missing required setting stops
the app at startup.

| Setting | Required | Purpose |
|---|---|---|
| `GoogleClientId`, `GoogleClientSecret` | always | Google external login |
| `ReCAPTCHASiteKey`, `ReCAPTCHASecretKey` | always | reCAPTCHA v3 |
| `SqlConnectionStringBuilder` section (`DataSource`, `InitialCatalog`, and `UserID`/`Password` or `IntegratedSecurity`) | always | the database |
| `PasskeyOrigin` | always | the origin passkey ceremonies must come from (scheme and host) |
| `ServiceBusConnectionString` | outside Production | the email queue |
| `CorsPolicy`, `ConsentOptions`, `TelemetryOptions`, `AdminSections`, `ManageSections`, `ErrorExplanations`, `RecaptchaVerifyEndpoint`, `RecaptchaScriptEndpoint` | always (in `appsettings.json`) | the app's configuration data |
| `DefaultAzureCredentialOptions`, `ServiceBusNamespace`, `BlobUri`, `DataProtectionKeyIdentifier`, `ElasticsearchNode`, `ElasticsearchUsername`, `ElasticsearchPassword`, `AlloyEndpoint`, `WEBSITE_SITE_NAME` | Production only | managed identity, email, data protection, logs, traces and metrics |

```powershell
dotnet user-secrets set GoogleClientId "<client id>" --project Identity
dotnet user-secrets set GoogleClientSecret "<client secret>" --project Identity
dotnet user-secrets set ReCAPTCHASiteKey "<site key>" --project Identity
dotnet user-secrets set ReCAPTCHASecretKey "<secret key>" --project Identity
dotnet user-secrets set ServiceBusConnectionString "<connection string>" --project Identity
```

In production each secret is an App Service setting holding a Key Vault reference, which the platform resolves at
startup; the app makes no vault calls of its own. **A setting's name is its configuration key**: the two SQL
credentials bind into the `SqlConnectionStringBuilder` section (`SqlConnectionStringBuilder:UserID`,
`SqlConnectionStringBuilder:Password`), so a secret set under any other name does nothing.

## Set up the database

The `Identity.Data` project owns the schema; there are no EF Core migrations.

```powershell
dotnet build Identity.Data/Identity.Data.sqlproj --configuration Release
sqlpackage /Action:Publish /SourceFile:Identity.Data/bin/Release/Identity.Data.dacpac /TargetConnectionString:"<connection string>"
```

Configuration-store rows (clients, resources, scopes) are data you add for your own applications, through the Admin
UI or SQL.

## Run locally

```powershell
dotnet run --project Identity     # https://localhost:7261
```

## Tests

```powershell
dotnet test --project Identity.Tests.Unit -- --filter-trait "Category=Unit"
```

The unit tier needs nothing else. The E2E, Integration and Load tiers host the app against a SQL database whose
catalog name ends in `Test`, published from the dacpac; [TESTING.md](TESTING.md) has the setup.

## Gate

`gate.ps1` is the maintainers' gate. It loads a private toolkit from a folder beside this repo, so in a plain clone it
stops at once. To gate your own change, run the checks CI runs, in this order, from the repo root (the hosted tiers
with `ASPNETCORE_ENVIRONMENT=CI` and the settings above pointing at a `*Test` database):

```powershell
npm ci
npm run lint
npm run audit
dotnet tool restore
dotnet build --no-incremental --configuration Release /p:RestoreLockedMode=true -warnaserror
dotnet jb inspectcode Identity.slnx --no-build -e=WARNING --output=inspectcode.sarif
dotnet test --project Identity.Tests.Unit --no-build --configuration Release -- --filter-trait "Category=Unit"
sqlpackage /Action:Publish /SourceFile:Identity.Data/bin/Release/Identity.Data.dacpac /TargetConnectionString:"<test database>"
pwsh Identity.Tests.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
dotnet dev-certs https
dotnet test --project Identity.Tests.E2E --no-build --configuration Release -- --filter-trait "Category=E2E"
dotnet test --project Identity.Tests.Integration --no-build --configuration Release -- --filter-trait "Category=Integration"
dotnet stryker --config-file ../stryker-config.json    # from Identity/
```

Every command must exit 0, and `inspectcode.sarif` must hold no results. CI additionally runs a SonarCloud analysis
and fails on any open issue.

## Project structure

| Path | What it holds |
|---|---|
| `Identity/` | the web app: Razor Pages, the DbContext, IdentityServer and Identity wiring |
| `Identity.Data/` | the SQL Server Database Project, the schema's source of truth |
| `Identity.Tests.Unit/` | unit tests |
| `Identity.Tests.Integration/` | endpoint and service tests over `HttpClient` against a hosted app |
| `Identity.Tests.E2E/` | browser journeys as Gherkin (Reqnroll and Playwright), and the synthetic walker |
| `Identity.Tests.Load/` | throughput tests, run on manual dispatch |
| `package.json`, `eslint.config.js` | linting `Identity/wwwroot/js` |

## Deployment

`.github/workflows/main_crgolden-identity.yml` builds, inspects and tests on every push and pull request, publishing
the dacpac to the E2E database first. On `main` the deploy job publishes the dacpac to the production database, then
the app to Azure App Service over OIDC federated credentials, so the schema is always ahead of or equal to the code.
A `mutation` job runs Stryker in parallel; it is not a deploy dependency. `synthetic.yml` walks the deployed app on a
schedule ([TESTING.md](TESTING.md#synthetic-walker)).

| Repository secret or variable | Used by |
|---|---|
| `PACKAGES_READ_TOKEN` (secret) | restoring from GitHub Packages |
| `SONAR_TOKEN` (secret); `SONAR_PROJECT_KEY`, `SONAR_ORGANIZATION` (variables) | the SonarCloud steps |
| `DB_USERID`, `DB_PASSWORD` (secrets); `DB_SERVER`, `DB_NAME_E2E`, `DB_NAME` (variables) | the test and production databases |
| `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `RECAPTCHA_SITE_KEY_E2E`, `RECAPTCHA_SECRET_KEY`, `SERVICE_BUS_CONNECTION_STRING` (secrets) | the hosted test tiers |
| `PLAYWRIGHT_*`, `E2E_INDEPENDENTLY_PINNED_ADMIN_CARD_COUNT`, `LOAD_*` (variables) | test-tier settings |
| `TEST_RESULTS_PGPASSWORD` (secret); `TEST_RESULTS_PGHOST`, `TEST_RESULTS_PGDATABASE`, `TEST_RESULTS_PGUSER`, `TEST_RESULTS_PGSSLMODE`, `TEST_RESULTS_APP` (variables) | publishing the E2E scenario results |
| `STRYKER_DASHBOARD_API_KEY` (secret) | the mutation report |
| `AZUREAPPSERVICE_CLIENTID_*`, `AZUREAPPSERVICE_TENANTID_*`, `AZUREAPPSERVICE_SUBSCRIPTIONID_*` | the deploy job |
| `WALKER_*`, `PLAYWRIGHT_*` (variables); `PASSKEY_CREDENTIAL1`, `PASSKEY_CREDENTIAL3` (secrets) | the synthetic walker |
| `CLAUDE_CODE_OAUTH_TOKEN` (secret) | the Claude Code review workflows |
