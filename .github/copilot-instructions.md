# Copilot instructions for this repository

Purpose
- Give AI assistants (Copilot/Claude) focused repo knowledge: build/test commands, high-level architecture, and codebase-specific conventions.

Build, test, and (lint) commands
- Build entire solution: `dotnet build --no-incremental --configuration Release`
- Build single project: `dotnet build Identity.Api` or `dotnet build Identity.Data/Identity.Data.sqlproj`
- Run the web app (dev): `cd Identity.Api && dotnet run` (HTTPS: https://localhost:7261)
- Run all tests: `dotnet test --project Identity.Tests --configuration Release`
- Run unit tests only: `dotnet test --project Identity.Tests --configuration Release -- --filter-trait "Category=Unit"`
- Run E2E tests (local, uses User Secrets + real DB):
  `ASPNETCORE_ENVIRONMENT=Development SqlConnectionStringBuilder__InitialCatalog=IdentityTest dotnet test --project Identity.Tests --configuration Release -- --filter-trait "Category=E2E"`
- Run a single test: use `--filter` with fully-qualified name or trait, e.g.: `dotnet test --filter "FullyQualifiedName=Namespace.ClassName.TestMethod"` or use `--filter-trait "Category=Unit"` for trait filtering.
- Build & publish dacpac (schema):
  `dotnet build Identity.Data/Identity.Data.sqlproj --configuration Release`
  `sqlpackage /Action:Publish /SourceFile:Identity.Data/bin/Release/Identity.Data.dacpac /TargetConnectionString:"<conn>"`
- Benchmarks: `dotnet run --project Identity.Benchmarks -c Release`
- Mutation testing (Stryker): `dotnet stryker --config-file stryker-config.json`

High-level architecture (summary)
- Multi-project solution:
  - Identity.Api: ASP.NET Core 10 Razor Pages app, hosts OIDC/OAuth via Duende IdentityServer 7 and the single ApplicationDbContext.
  - Identity.Data: SQL Server Database Project (SSDT) — authoritative schema; builds to a .dacpac and deployed via SqlPackage.
  - Identity.Tests: xUnit v3 tests — Unit, E2E (Playwright/Chromium), Load tests; uses WebApplicationFactory for an in-process Kestrel server.
  - Identity.Benchmarks: BenchmarkDotNet microbenchmarks.
- Single ApplicationDbContext implements three interfaces: IdentityDbContext<IdentityUser<Guid>,...>, IConfigurationDbContext (Duende clients/resources), IPersistedGrantDbContext (operational grants, server-side sessions).
- Passkeys / WebAuthn: Two minimal API endpoints (`POST /Account/PasskeyCreationOptions`, `POST /Account/PasskeyRequestOptions`) used by client-side `passkey-submit.js` custom element and a TagHelper.
- Schema workflow: Development applies the dacpac to local SQL Server; CI/Production always deploys the Identity.Data .dacpac before the app.

Key conventions and patterns (repo-specific)
- Identity types: Always use `IdentityUser<Guid>` (not default string-based IdentityUser). Keep Guid key type consistent across services and DI.
- User Secrets: Program.cs explicitly calls `builder.Configuration.AddUserSecrets("aspnet-Identity-149346d0-999f-4a74-8ff7-2a92d39790f2")` — AI agents should respect this when reconstructing local dev instructions.
- Passkey tag helper: `passkey-submit.js` is rendered by `PasskeySubmitTagHelper`; ensure `@addTagHelper *, Identity.Api` present in `_ViewImports.cshtml` or the element won't render.
- Open-redirect protection: Login and redirect handlers validate return URLs — prefer `Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : LocalRedirect("~/")` instead of raw redirects.
- Tests:
  - xUnit runner config sets `parallelizeTestCollections: false`. The WebApplicationFactory starts a Kestrel server and uses `PlaywrightFixture` which installs Chromium and may require Azure credentials for E2E.
  - Use `--filter-trait "Category=Unit"` or `Category=E2E` to run subsets. E2E tests require DB schema and Key Vault secrets.
  - Test classes use explicit collection attributes (`UnitCollection`, `E2ECollection`) for Stryker compatibility.
- CI notes:
  - CI uses Azure OIDC to provide Key Vault access during E2E tests. In CI, `ASPNETCORE_ENVIRONMENT=CI` loads `appsettings.CI.json` which enables `AzureCliCredential`.
  - App Service publish uses `-r win-x86` for production publish due to App Service Free tier x86 requirement.
- Observability & secrets:
  - Serilog + Elasticsearch in Production (console for non-prod). OpenTelemetry → Azure Monitor used for metrics/traces.
  - Data Protection keys persisted to Azure Blob Storage and protected with Key Vault.

Important doc files and AI assistant configs to consult
- CLAUDE.md — extended guidance and local clone references for Duende source, passkey notes, and many repo-specific rules. (Consult first when generating code-aware responses.)
- README.md, TESTING.md, DESIGN.md — architecture, commands, and testing details.
- .mcp.json and .claude/settings.json exist and configure MCP servers used by Claude Code (github, azure, playwright, sonarqube). Check these when enabling assistant integrations.

Notes for Copilot sessions
- Prefer reading CLAUDE.md and DESIGN.md for architecture/context before making changes.
- When suggesting code changes touching schema, update Identity.Data SQL project (.sqlproj) rather than adding EF migrations.
- Do not change TreatWarningsAsErrors in csproj — fix warnings rather than suppressing them.
- For E2E or mutation testing workflows, read TESTING.md and the Playwright fixtures to avoid flaky runs.

MCP server configuration
- This repo benefits from a Playwright MCP server for E2E debugging and a GitHub MCP server for repo operations. Ask to enable/approve them if you want the assistant to run browser-driven tests or manage PRs.

Summary
- Created focused Copilot instructions consolidating build/test commands, architecture highlights, and repo conventions. Ask if adjustments are needed or if coverage of any area should be expanded.

