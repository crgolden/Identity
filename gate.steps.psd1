@{
    Schema      = 1
    Repo        = 'Identity'
    Workflow    = '.github/workflows/main_crgolden-identity.yml'
    Sonar       = @{ ProjectKey = 'crgolden_Identity' }
    Environment = @{ SqlConnectionStringBuilder__InitialCatalog = 'IdentityTest' }
    Probe       = @{
        Kind        = 'dotnet'
        Projects    = @('Identity.Tests.Unit', 'Identity.Tests.E2E', 'Identity.Tests.Integration')
        Environment = @{
            'Identity.Tests.E2E'         = @{ ASPNETCORE_ENVIRONMENT = 'Development'; PasskeyOrigin = 'https://127.0.0.1' }
            'Identity.Tests.Integration' = @{ ASPNETCORE_ENVIRONMENT = 'Development'; PasskeyOrigin = 'https://127.0.0.1' }
        }
        Wrap        = @{
            'Identity.Tests.E2E'         = @('pwsh', '-NoProfile', '-File', 'scripts/gate/Use-TestDatabase.ps1', 'pwsh', '-NoProfile', '-File', 'scripts/gate/Use-TestBrowser.ps1', 'Identity.Tests.E2E')
            'Identity.Tests.Integration' = @('pwsh', '-NoProfile', '-File', 'scripts/gate/Use-TestDatabase.ps1', 'pwsh', '-NoProfile', '-File', 'scripts/gate/Use-TestBrowser.ps1', 'Identity.Tests.Integration')
        }
    }
    Steps       = @(
        @{ Name = 'Regenerate lock files and push'; Delivery = $true }
        @{ Name = 'Install SonarQube Cloud scanner'; Tool = 'dotnet-sonarscanner'; Inputs = @('*') }
        @{ Name = 'Install dotnet-coverage'; Tool = 'dotnet-coverage'; Inputs = @('*') }
        @{ Name = 'Restore local tools'; Run = @('dotnet', 'tool', 'restore'); Inputs = @('dotnet-tools.json'); Always = $true }
        @{
            Name   = 'S101 dictionary control'
            Inputs = @('*')
            Run    = @('pwsh', '-NoProfile', '-File', 'scripts/gate/Assert-S101Control.ps1', '-BackupPath', '{output}\Identity.csproj.s101-control-backup')
        }
        @{
            Name   = 'Begin Sonar analysis'
            Sonar  = 'begin'
            Needs  = @('Install SonarQube Cloud scanner')
            Inputs = @('*')
            Run    = @(
                'dotnet-sonarscanner', 'begin', '/k:{sonarProject}', '/o:crgolden', '/d:sonar.host.url=https://sonarcloud.io',
                '/d:sonar.cs.opencover.reportsPaths=coverage.opencover.xml',
                '/d:sonar.cs.vscoveragexml.reportsPaths=coverage-e2e.xml,coverage-integration.xml',
                '/d:sonar.exclusions=**/bin/**,**/obj/**', '/d:sonar.coverage.exclusions=**/Program.cs,**/gate.ps1,**/gate.steps.psd1,scripts/gate/**',
                '/d:sonar.qualitygate.wait=true', '/d:sonar.scanner.skipJreProvisioning=true',
                '/d:sonar.scm.use.blame.algorithm=GIT_NATIVE_BLAME', '/d:sonar.branch.name={sonarBranch}'
            )
        }
        @{
            Name   = 'Build with dotnet'
            Sonar  = 'build'
            Inputs = @('*')
            Run    = @('dotnet', 'build', '--no-incremental', '--configuration', 'Release', '/p:RestoreLockedMode=true', '-warnaserror')
        }
        @{
            Name   = 'ReSharper inspection (jb inspectcode)'
            Needs  = @('Build with dotnet', 'Restore local tools')
            Inputs = @('*')
            Remove = @('{output}\identity-inspect.sarif')
            Run    = @('dotnet', 'jb', 'inspectcode', 'Identity.slnx', '--no-build', '-e=WARNING', '--caches-home={inspectCache}', '--output={output}\identity-inspect.sarif')
            Check  = @{ Sarif = '{output}\identity-inspect.sarif' }
        }
        @{
            Name   = 'Run unit tests with coverage'
            Needs  = @('Build with dotnet', 'Restore local tools')
            Inputs = @('*', '!Identity.Tests.E2E/*', '!Identity.Tests.Integration/*', '!Identity.Tests.Load/*')
            Remove = @('Identity.Tests.Unit\bin\Release\net11.0\TestResults\unit-tests.trx')
            Run    = @(
                'dotnet', 'coverlet', 'Identity.Tests.Unit/bin/Release/net11.0', '--target', 'dotnet',
                '--targetargs', 'test --project Identity.Tests.Unit --no-build --configuration Release -- --filter-trait Category=Unit --report-xunit-trx --report-xunit-trx-filename unit-tests.trx --results-directory=Identity.Tests.Unit/bin/Release/net11.0/TestResults',
                '--format', 'opencover', '--output', 'coverage.opencover.xml', '--skipautoprops',
                '--exclude-by-attribute', 'GeneratedCodeAttribute', '--exclude-by-file', '**/obj/**', '--exclude-by-file', '**/Program.cs',
                '--does-not-return-attribute', 'DoesNotReturnAttribute', '--include', '[Identity]*', '--exclude', '[Identity]*Pages_*'
            )
            Check  = @{ Trx = 'Identity.Tests.Unit\bin\Release\net11.0\TestResults\unit-tests.trx'; Floor = 1 }
        }
        @{ Name = 'Install SqlPackage'; Tool = 'sqlpackage'; Inputs = @('*') }
        @{
            Name   = 'Deploy E2E test database schema'
            Needs  = @('Build with dotnet', 'Install SqlPackage')
            Inputs = @('*', '!Identity.Tests.Unit/*')
            Run    = @('pwsh', '-NoProfile', '-File', 'scripts/gate/Use-TestDatabase.ps1', 'pwsh', '-NoProfile', '-File', 'scripts/gate/Publish-TestSchema.ps1')
        }
        @{ Name = 'Get Playwright version'; Delivery = $true }
        @{
            Name              = 'Install Playwright browsers'
            PlaywrightInstall = @{
                DryRun  = @('pwsh', 'Identity.Tests.E2E/bin/Release/net11.0/playwright.ps1', 'install', '--dry-run', 'chromium')
                Install = @('pwsh', 'Identity.Tests.E2E/bin/Release/net11.0/playwright.ps1', 'install', 'chromium')
            }
            Needs             = @('Build with dotnet')
            Inputs            = @('*', '!Identity.Tests.Unit/*')
            Always            = $true
        }
        @{
            Name     = 'Create the HTTPS development certificate'
            Run      = @('dotnet', 'dev-certs', 'https')
            Inputs   = @('*', '!Identity.Tests.Unit/*')
            Volatile = $true
        }
        @{
            Name        = 'Run E2E tests with coverage'
            Needs       = @('Build with dotnet', 'Install dotnet-coverage', 'Deploy E2E test database schema', 'Install Playwright browsers', 'Create the HTTPS development certificate')
            Inputs      = @('*', '!Identity.Tests.Unit/*')
            Environment = @{ ASPNETCORE_ENVIRONMENT = 'Development'; PasskeyOrigin = 'https://127.0.0.1' }
            Remove      = @(
                'Identity.Tests.E2E\bin\Release\net11.0\TestResults\e2e-tests.trx'
                'Identity.Tests.E2E\bin\Release\net11.0\TestResults\cucumber-report\messages.ndjson'
                'Identity.Tests.E2E\bin\Release\net11.0\TestResults\PlaywrightArtifacts\.tmp'
            )
            Run         = @(
                'pwsh', '-NoProfile', '-File', 'scripts/gate/Use-TestDatabase.ps1', 'dotnet-coverage', 'collect',
                'dotnet test --project Identity.Tests.E2E --no-build --configuration Release -- --filter-trait Category=E2E --report-xunit-trx --report-xunit-trx-filename e2e-tests.trx --results-directory=Identity.Tests.E2E/bin/Release/net11.0/TestResults',
                '-f', 'xml', '-o', 'coverage-e2e.xml', '-s', 'coverage.settings.xml'
            )
            Check       = @{ Trx = 'Identity.Tests.E2E\bin\Release\net11.0\TestResults\e2e-tests.trx'; Floor = 1 }
        }
        @{ Name = 'Snapshot TCP state after an E2E failure'; Delivery = $true }
        @{ Name = 'Locate the Cucumber messages report'; Delivery = $true }
        @{
            Name   = 'Assert E2E executed at least its floor'
            Needs  = @('Run E2E tests with coverage')
            Inputs = @('*', '!Identity.Tests.Unit/*')
            Run    = @('pwsh', '-NoProfile', '-File', 'scripts/gate/Assert-E2eFloor.ps1')
        }
        @{
            Name           = 'Publish E2E scenario results'
            PublishCommand = @{ App = 'Identity'; Messages = 'Identity.Tests.E2E\bin\Release\net11.0\TestResults/cucumber-report/messages.ndjson' }
            Needs          = @('Assert E2E executed at least its floor')
            Inputs         = @('*')
        }
        @{
            Name        = 'Run integration tests with coverage'
            Needs       = @('Build with dotnet', 'Install dotnet-coverage', 'Deploy E2E test database schema')
            Inputs      = @('*', '!Identity.Tests.Unit/*')
            Environment = @{ ASPNETCORE_ENVIRONMENT = 'Development'; PasskeyOrigin = 'https://127.0.0.1' }
            Remove      = @('Identity.Tests.Integration\bin\Release\net11.0\TestResults\integration-tests.trx')
            Run         = @(
                'pwsh', '-NoProfile', '-File', 'scripts/gate/Use-TestDatabase.ps1', 'dotnet-coverage', 'collect',
                'dotnet test --project Identity.Tests.Integration --no-build --configuration Release -- --filter-trait Category=Integration --report-xunit-trx --report-xunit-trx-filename integration-tests.trx --results-directory=Identity.Tests.Integration/bin/Release/net11.0/TestResults',
                '-f', 'xml', '-o', 'coverage-integration.xml', '-s', 'coverage.settings.xml'
            )
            Check       = @{ Trx = 'Identity.Tests.Integration\bin\Release\net11.0\TestResults\integration-tests.trx'; Floor = 27 }
        }
        @{ Name = 'Run load tests'; Delivery = $true }
        @{ Name = 'dotnet publish'; Delivery = $true }
        @{
            Name   = 'End Sonar analysis'
            Sonar  = 'end'
            Needs  = @('Begin Sonar analysis', 'Build with dotnet')
            Inputs = @('*')
            Run    = @('dotnet-sonarscanner', 'end')
        }
        @{ Name = 'Fail on open Sonar issues'; Sonar = 'issues'; Needs = @('End Sonar analysis'); Inputs = @('*') }
        @{ Name = 'Deploy database'; Delivery = $true }
        @{ Name = 'Run the app on .NET 11'; Delivery = $true }
        @{
            Name     = 'Run Stryker mutation tests'
            Mutation = @{ Folder = 'Identity'; Config = 'stryker-config.json'; Output = 'Identity\StrykerOutput\gate'; Dashboard = $true }
            Needs    = @('Build with dotnet', 'Restore local tools')
            Inputs   = @('*')
        }
    )
}
