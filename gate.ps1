param([string]$Goal, [string[]]$Steps, [string]$Tests, [string]$TestProject)

$ErrorActionPreference = 'Continue'
$gateCommon = Join-Path $PSScriptRoot '..\Tools\Gates\GateCommon.ps1'
if (-not (Test-Path -LiteralPath $gateCommon)) {
    Write-Host "GATE: FAILED (the Tools repository must be cloned beside this one: $gateCommon)"
    exit 1
}
. $gateCommon
. (Join-Path $PSScriptRoot '..\Tools\Gates\SelectedTests.ps1')
$gateOutput = Join-Path ([IO.Path]::GetTempPath()) "crgolden-gates\$(Split-Path -Leaf $PSScriptRoot)"
New-Item -ItemType Directory -Force -Path $gateOutput | Out-Null
Register-GateSteps @('Install dotnet-coverage', 'Restore local tools', 'node_modules install markers', 'npm run lint',
    'S101 dictionary control', 'Begin Sonar analysis',
    'Build with dotnet', 'jb inspectcode', 'Unit tests (Identity.Tests.Unit', 'Install SqlPackage',
    'Deploy E2E test database schema', 'Install Playwright browsers', 'E2E tests (Identity.Tests.E2E',
    'Cucumber messages report (Identity.Tests.E2E', 'Playwright artifact temp folder (Identity.Tests.E2E',
    'Publish command reaches the CLI',
    'Integration tests (Identity.Tests.Integration',
    'End Sonar analysis', 'Fail on open Sonar issues', 'Run Stryker mutation tests')
Register-StepInputs @{
    'Install dotnet-coverage'                             = @('*')
    'Restore local tools'                                 = @('dotnet-tools.json')
    'node_modules install markers'                        = @('*')
    'npm run lint'                                        = @('*')
    'S101 dictionary control'                             = @('*')
    'Begin Sonar analysis'                                = @('*')
    'Build with dotnet'                                   = @('*')
    'jb inspectcode'                                      = @('*')
    'Unit tests (Identity.Tests.Unit'                     = @('*')
    'Install SqlPackage'                                  = @('*')
    'Deploy E2E test database schema'                     = @('*')
    'Install Playwright browsers'                         = @('*')
    'E2E tests (Identity.Tests.E2E'                       = @('*')
    'Cucumber messages report (Identity.Tests.E2E'        = @('*')
    'Playwright artifact temp folder (Identity.Tests.E2E' = @('*')
    'Publish command reaches the CLI'                     = @('*')
    'Integration tests (Identity.Tests.Integration'       = @('*')
    'End Sonar analysis'                                  = @('*')
    'Fail on open Sonar issues'                           = @('*')
    'Run Stryker mutation tests'                          = @('*')
}
$repo = $PSScriptRoot
$sarif = (Join-Path $gateOutput 'identity-inspect.sarif')
$unitTrx = Join-Path $repo 'Identity.Tests.Unit\bin\Release\net10.0\TestResults\unit-tests.trx'
$e2eTrx = Join-Path $repo 'Identity.Tests.E2E\bin\Release\net10.0\TestResults\e2e-tests.trx'
$integrationTrx = Join-Path $repo 'Identity.Tests.Integration\bin\Release\net10.0\TestResults\integration-tests.trx'
$testCatalog = $env:SqlConnectionStringBuilder__InitialCatalog ?? 'IdentityTest'
$dataSource = $env:SqlConnectionStringBuilder__DataSource ?? '(localdb)\MSSQLLocalDB'
$sqlAuthentication = if ($env:SqlConnectionStringBuilder__UserID) { "User ID=$env:SqlConnectionStringBuilder__UserID;Password=$env:SqlConnectionStringBuilder__Password;Encrypt=True;TrustServerCertificate=False" } else { 'Integrated Security=True;TrustServerCertificate=True' }
function Start-LocalDbWhenTargeted { if (-not $env:SqlConnectionStringBuilder__DataSource) { sqllocaldb start MSSQLLocalDB | Out-Null } }
$sonarBranch = Get-SonarBranchName
$beginSonar = "Begin Sonar analysis (branch $sonarBranch)"
$build = 'Build with dotnet (Release, RestoreLockedMode)'
$endSonar = 'End Sonar analysis (quality gate waited)'
$sonarIssues = 'Fail on open Sonar issues'
$s101 = 'S101 dictionary control (must FAIL without the dictionary)'
$unit = 'Unit tests (Identity.Tests.Unit, Category=Unit)'
$schema = "Deploy E2E test database schema ($testCatalog)"
$e2e = 'E2E tests (Identity.Tests.E2E, Category=E2E)'
$e2eOutput = Join-Path $repo 'Identity.Tests.E2E\bin\Release\net10.0'
$e2eFloor = [int](Get-Content (Join-Path $repo 'Identity.Tests.E2E\e2e-settings.json') -Raw | ConvertFrom-Json).executedTestFloor
$cucumberMessagesRelative = Join-Path 'Identity.Tests.E2E\bin\Release\net10.0' (Get-Content (Join-Path $repo 'Identity.Tests.E2E\reqnroll.json') -Raw | ConvertFrom-Json).formatters.message.outputFilePath
$cucumberMessages = Join-Path $repo $cucumberMessagesRelative
$publishStep = 'Publish command reaches the CLI (the workflow line run without GITHUB_RUN_ID must fail on the run identity, not on usage)'
$cucumberReport = 'Cucumber messages report (Identity.Tests.E2E, one testCaseFinished per floor scenario)'
$playwrightSettings = (Get-Content (Join-Path $repo 'Identity\appsettings.Development.json') -Raw | ConvertFrom-Json).PlaywrightSettings
$artifactTemp = [IO.Path]::Combine($e2eOutput, $playwrightSettings.TestResultsFolderName, $playwrightSettings.ArtifactsFolderName, $playwrightSettings.TempFolderName)
$artifactTempStep = 'Playwright artifact temp folder (Identity.Tests.E2E, empty after a passing run)'
$integration = 'Integration tests (Identity.Tests.Integration, Category=Integration)'
$stryker = "Run Stryker mutation tests (dashboard version $sonarBranch)"
$env:TZ = 'UTC'
if ($env:TZ -ne 'UTC') { Write-Host 'GATE: FAILED (TZ pin)'; exit 1 }
Set-Location $repo
if ($Tests) {
    Invoke-SelectedTests $repo $TestProject $Tests {
        if ($TestProject -in 'Identity.Tests.E2E', 'Identity.Tests.Integration') {
            Start-LocalDbWhenTargeted
            $env:ASPNETCORE_ENVIRONMENT = 'Development'
            $env:SqlConnectionStringBuilder__InitialCatalog = $testCatalog
            $env:PasskeyOrigin = 'https://127.0.0.1'
            pwsh "$repo\Identity.Tests.E2E\bin\Release\net10.0\playwright.ps1" install chromium
        }
    }
}
Initialize-GateState 'Identity' $repo
Assert-RequestedSteps $Steps
Invoke-CatalogSteps

if (-not (Test-StepCarried 'Install dotnet-coverage')) {
    if (Get-Command dotnet-coverage -ErrorAction SilentlyContinue) { Write-Row 'Install dotnet-coverage' 'PASS' 'present on PATH' }
    else { Stop-Gate 'Install dotnet-coverage' 'not on PATH' }
}
$global:LASTEXITCODE = $null
dotnet tool restore
$null = Test-Exit 'Restore local tools (dotnet tool restore)'

$installed = (Test-Path (Join-Path $repo 'node_modules\.package-lock.json')) -and
    (Test-Path (Join-Path $repo 'node_modules\.bin\eslint.cmd'))
if (-not $installed) { Stop-Gate 'node_modules install markers' 'incomplete install; run npm ci deliberately first' }
Write-Row 'node_modules install markers' 'PASS' '.package-lock.json, eslint.cmd present'

if (-not (Test-StepCarried 'npm run lint')) {
    $global:LASTEXITCODE = $null
    npm run lint
    $null = Test-Exit 'npm run lint'
}

if (-not (Test-StepCarried $s101)) {
    $identityCsproj = Join-Path $repo 'Identity\Identity.csproj'
    $csprojBackup = Join-Path $gateOutput 'Identity.csproj.s101-control-backup'
    if (Test-Path -LiteralPath $csprojBackup) {
        [IO.File]::WriteAllBytes($identityCsproj, [IO.File]::ReadAllBytes($csprojBackup))
        Remove-Item -LiteralPath $csprojBackup -Force
        Write-Host "Restored Identity.csproj from the backup an interrupted S101 control left behind"
    }
    $csprojBytes = [IO.File]::ReadAllBytes($identityCsproj)
    $csprojOriginal = [IO.File]::ReadAllText($identityCsproj)
    $includePattern = '(?m)^[ \t]*<AdditionalFiles Include="\.\.\\CustomDictionary\.xml" />\r?\n'
    if ($csprojOriginal -notmatch $includePattern) {
        Stop-Gate $s101 'the CustomDictionary.xml AdditionalFiles include is absent from Identity.csproj'
    }
    $named = @()
    $expectedNames = 'ICAPTCHAService', 'ReCAPTCHAService', 'ReCAPTCHAOptions', 'CAPTCHAVerdict'
    [IO.File]::WriteAllBytes($csprojBackup, $csprojBytes)
    try {
        [IO.File]::WriteAllText($identityCsproj, ($csprojOriginal -replace $includePattern, ''))
        $controlLog = (dotnet build "$identityCsproj" --configuration Release /p:RestoreLockedMode=true 2>&1 | Out-String)
        $named = @($expectedNames | Where-Object { $controlLog -match ("S101[^\r\n]*'" + $_ + "'") })
    }
    finally {
        [IO.File]::WriteAllBytes($identityCsproj, $csprojBytes)
    }
    $restoredBytes = [IO.File]::ReadAllBytes($identityCsproj)
    if (-not [Linq.Enumerable]::SequenceEqual($restoredBytes, $csprojBytes)) { Write-Host 'GATE: FAILED (the control could not restore Identity.csproj)'; exit 1 }
    Remove-Item -LiteralPath $csprojBackup -Force
    if ($named.Count -ne $expectedNames.Count) {
        Stop-Gate $s101 "S101 named $($named.Count) of $($expectedNames.Count); the dictionary is not what silences it"
    }
    Write-Row $s101 'PASS' "S101 named all $($named.Count): $($named -join ', ')"
}

$sonarCarried = Test-StepCarried $sonarIssues
if ($sonarCarried) {
    $null = Test-StepCarried $beginSonar
    $null = Test-StepCarried $endSonar
}
else {
    $sonarStartedAt = [DateTimeOffset]::UtcNow
    $env:JAVA_HOME = "$env:SystemDrive\sonar-scanner-8.0.1.6346-windows-x64\jre"
    $global:LASTEXITCODE = $null
    dotnet-sonarscanner begin /k:"crgolden_Identity" /o:"crgolden" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.cs.opencover.reportsPaths="coverage.opencover.xml" /d:sonar.cs.vscoveragexml.reportsPaths="coverage-e2e.xml,coverage-integration.xml" /d:sonar.exclusions="**/bin/**,**/obj/**" /d:sonar.coverage.exclusions="**/Program.cs,**/gate.ps1" /d:sonar.qualitygate.wait=true /d:sonar.scanner.skipJreProvisioning=true /d:sonar.branch.name="$sonarBranch"
    $null = Test-Exit $beginSonar
}

if ($sonarCarried) { $null = Test-StepCarried $build }
else {
    $global:LASTEXITCODE = $null
    dotnet build --no-incremental --configuration Release /p:RestoreLockedMode=true -warnaserror
    $null = Test-Exit $build
}

if (-not (Test-StepCarried 'jb inspectcode')) {
    if (Test-Path $sarif) { Remove-Item $sarif -Force }
    dotnet jb inspectcode "$repo\Identity.slnx" --no-build -e=WARNING --caches-home="$(New-InspectCodeCaches $gateOutput)" --output="$sarif"
    Test-Sarif $sarif
}

if (-not (Test-StepCarried $unit)) {
    if (Test-Path $unitTrx) { Remove-Item $unitTrx -Force }
    $global:LASTEXITCODE = $null
    dotnet coverlet Identity.Tests.Unit\bin\Release\net10.0 `
        --target "dotnet" `
        --targetargs "test --project Identity.Tests.Unit --no-build --configuration Release -- --filter-trait Category=Unit --stop-on-fail on --report-xunit-trx --report-xunit-trx-filename unit-tests.trx --results-directory=Identity.Tests.Unit/bin/Release/net10.0/TestResults" `
        --format opencover --output "coverage.opencover.xml" `
        --skipautoprops --exclude-by-attribute GeneratedCodeAttribute --exclude-by-file "**/obj/**" `
        --exclude-by-file "**/Program.cs" --does-not-return-attribute DoesNotReturnAttribute `
        --include "[Identity]*" --exclude "[Identity]*Pages_*"
    Test-Trx $unit $unitTrx $global:LASTEXITCODE -floor 1
}

if (-not (Test-StepCarried 'Install SqlPackage')) {
    if (Get-Command sqlpackage -ErrorAction SilentlyContinue) { Write-Row 'Install SqlPackage' 'PASS' 'present on PATH' }
    else { Stop-Gate 'Install SqlPackage' 'not on PATH' }
}
if (-not (Test-StepCarried $schema)) {
    Start-LocalDbWhenTargeted
    $global:LASTEXITCODE = $null
    sqlpackage /Action:Publish /SourceFile:Identity.Data/bin/Release/Identity.Data.dacpac /TargetConnectionString:"Data Source=$dataSource;Initial Catalog=$testCatalog;$sqlAuthentication"
    $null = Test-Exit $schema
}

Install-PlaywrightBrowsers 'Install Playwright browsers' { pwsh "$repo\Identity.Tests.E2E\bin\Release\net10.0\playwright.ps1" install --dry-run chromium } { pwsh "$repo\Identity.Tests.E2E\bin\Release\net10.0\playwright.ps1" install chromium }

if (-not (Test-StepCarried $e2e)) {
    if ($e2eFloor -le 0) { Stop-Gate $e2e 'Identity.Tests.E2E\e2e-settings.json carries no positive executedTestFloor' }
    if (Test-Path $e2eTrx) { Remove-Item $e2eTrx -Force }
    if (Test-Path $cucumberMessages) { Remove-Item $cucumberMessages -Force }
    if (Test-Path $artifactTemp) { Remove-Item $artifactTemp -Recurse -Force }
    Start-LocalDbWhenTargeted
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:SqlConnectionStringBuilder__InitialCatalog = $testCatalog
    $env:PasskeyOrigin = 'https://127.0.0.1'
    $global:LASTEXITCODE = $null
    dotnet-coverage collect `
        "dotnet test --project Identity.Tests.E2E --no-build --configuration Release -- --filter-trait Category=E2E --stop-on-fail on --report-xunit-trx --report-xunit-trx-filename e2e-tests.trx --results-directory=Identity.Tests.E2E/bin/Release/net10.0/TestResults" `
        -f xml -o "coverage-e2e.xml" -s "coverage.settings.xml"
    Test-Trx $e2e $e2eTrx $global:LASTEXITCODE -floor $e2eFloor
    if (-not (Test-Path $cucumberMessages)) { Stop-Gate $cucumberReport "missing: $cucumberMessages" }
    $finishedCases = @(Select-String -LiteralPath $cucumberMessages -SimpleMatch '"testCaseFinished"').Count
    if ($finishedCases -lt $e2eFloor) { Stop-Gate $cucumberReport "$finishedCases testCaseFinished messages, floor $e2eFloor" }
    Write-Row $cucumberReport 'PASS' "$finishedCases testCaseFinished messages, floor $e2eFloor"
    $leakedSessions = if (Test-Path $artifactTemp) { @(Get-ChildItem -LiteralPath $artifactTemp -Recurse -File).Count } else { 0 }
    if ($leakedSessions -gt 0) { Stop-Gate $artifactTempStep "$leakedSessions file(s) left in ${artifactTemp}: artifacts were never finalized" }
    Write-Row $artifactTempStep 'PASS' "no files left in $artifactTemp"
    Test-PublishCommandLine $publishStep (Join-Path $repo '.github\workflows\main_crgolden-identity.yml') 'Identity' $repo $cucumberMessagesRelative
}

if (-not (Test-StepCarried $integration)) {
    if (Test-Path $integrationTrx) { Remove-Item $integrationTrx -Force }
    Start-LocalDbWhenTargeted
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:SqlConnectionStringBuilder__InitialCatalog = $testCatalog
    $env:PasskeyOrigin = 'https://127.0.0.1'
    $global:LASTEXITCODE = $null
    dotnet-coverage collect `
        "dotnet test --project Identity.Tests.Integration --no-build --configuration Release -- --filter-trait Category=Integration --stop-on-fail on --report-xunit-trx --report-xunit-trx-filename integration-tests.trx --results-directory=Identity.Tests.Integration/bin/Release/net10.0/TestResults" `
        -f xml -o "coverage-integration.xml" -s "coverage.settings.xml"
    Test-Trx $integration $integrationTrx $global:LASTEXITCODE -floor 27
}

if (-not $sonarCarried) {
    $global:LASTEXITCODE = $null
    dotnet-sonarscanner end
    $null = Test-Exit $endSonar
    Test-SonarIssues $sonarIssues 'crgolden_Identity' $sonarBranch $sonarStartedAt
}

if (-not (Test-StepCarried $stryker)) {
    if ([string]::IsNullOrWhiteSpace($env:STRYKER_DASHBOARD_API_KEY)) { Stop-Gate $stryker 'STRYKER_DASHBOARD_API_KEY is not in the environment' }
    $strykerConfig = (Get-Content (Join-Path $repo 'stryker-config.json') -Raw | ConvertFrom-Json).'stryker-config'
    $strykerReporters = @($strykerConfig.reporters | Where-Object { $_ -ne 'dashboard' } | ForEach-Object { '--reporter', $_ })
    $strykerOutputDir = Join-Path $repo 'Identity\StrykerOutput\gate'
    if (Test-Path -LiteralPath $strykerOutputDir) { Remove-Item -LiteralPath $strykerOutputDir -Recurse -Force }
    Push-Location (Join-Path $repo 'Identity')
    $global:LASTEXITCODE = $null
    dotnet stryker --config-file ../stryker-config.json --version $sonarBranch --output $strykerOutputDir @strykerReporters
    $strykerExit = $global:LASTEXITCODE
    Pop-Location
    if ($null -eq $strykerExit) { Stop-Gate $stryker 'command never ran' }
    if ($strykerExit -ne 0) { Stop-Gate $stryker "exit $strykerExit" }
    $global:LASTEXITCODE = $null
    node (Join-Path $PSScriptRoot '..\Tools\Gates\upload-stryker-report.mjs') (Join-Path $strykerOutputDir 'reports\mutation-report.json') $strykerConfig.'project-info'.name $sonarBranch
    $null = Test-Exit $stryker
}

Write-Row 'Publish E2E scenario results / Upload Cucumber report / dotnet publish / uploads / deploy' 'NOT RUN' 'delivery steps, not checks'
Complete-Gate
