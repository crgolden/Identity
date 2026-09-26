param([string]$Goal)

$ErrorActionPreference = 'Continue'
$gateCommon = Join-Path $PSScriptRoot '..\Tools\Gates\GateCommon.ps1'
if (-not (Test-Path -LiteralPath $gateCommon)) {
    Write-Host "GATE: FAILED (the Tools repository must be cloned beside this one: $gateCommon)"
    exit 1
}
. $gateCommon
$gateOutput = Join-Path ([IO.Path]::GetTempPath()) "crgolden-gates\$(Split-Path -Leaf $PSScriptRoot)"
New-Item -ItemType Directory -Force -Path $gateOutput | Out-Null
Register-GateSteps @('Install dotnet-coverage', 'Restore local tools', 'Begin Sonar analysis', 'S101 dictionary control',
    'Build with dotnet', 'jb inspectcode', 'Unit tests (Identity.Tests.Unit', 'Install SqlPackage',
    'Deploy E2E test database schema', 'Install Playwright browsers', 'E2E tests (Identity.Tests.E2E',
    'End Sonar analysis', 'Run Stryker mutation tests')
$repo = $PSScriptRoot
$sarif = (Join-Path $gateOutput 'identity-inspect.sarif')
$unitTrx = Join-Path $repo 'Identity.Tests.Unit\bin\Release\net10.0\TestResults\unit-tests.trx'
$e2eTrx = Join-Path $repo 'Identity.Tests.E2E\bin\Release\net10.0\TestResults\e2e-tests.trx'
$testCatalog = 'IdentityTest'
$sonarBranch = "branch-local-$($env:COMPUTERNAME.ToLowerInvariant())"
$beginSonar = "Begin Sonar analysis (branch $sonarBranch)"
$build = 'Build with dotnet (Release, RestoreLockedMode)'
$endSonar = 'End Sonar analysis (quality gate waited)'
$s101 = 'S101 dictionary control (must FAIL without the dictionary)'
$unit = 'Unit tests (Identity.Tests.Unit, Category=Unit)'
$schema = "Deploy E2E test database schema ($testCatalog)"
$e2e = 'E2E tests (Identity.Tests.E2E, Category=E2E)'
$stryker = "Run Stryker mutation tests (dashboard version $sonarBranch)"
$env:TZ = 'UTC'
if ($env:TZ -ne 'UTC') { Write-Host 'GATE: FAILED (TZ pin)'; exit 1 }
Set-Location $repo
Initialize-GateState 'Identity' $repo
Invoke-CatalogSteps

if (-not (Test-StepCarried 'Install dotnet-coverage')) {
    if (Get-Command dotnet-coverage -ErrorAction SilentlyContinue) { Write-Row 'Install dotnet-coverage' 'PASS' 'present on PATH' }
    else { Stop-Gate 'Install dotnet-coverage' 'not on PATH' }
}
$global:LASTEXITCODE = $null
dotnet tool restore
$null = Test-Exit 'Restore local tools (dotnet tool restore)'

$sonarCarried = Test-StepCarried $endSonar
if ($sonarCarried) { $null = Test-StepCarried $beginSonar }
else {
    $env:JAVA_HOME = "$env:SystemDrive\sonar-scanner-8.0.1.6346-windows-x64\jre"
    $global:LASTEXITCODE = $null
    dotnet-sonarscanner begin /k:"crgolden_Identity" /o:"crgolden" /d:sonar.token="$env:SONAR_TOKEN" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.cs.opencover.reportsPaths="coverage.opencover.xml" /d:sonar.cs.vscoveragexml.reportsPaths="coverage-e2e.xml" /d:sonar.exclusions="**/bin/**,**/obj/**" /d:sonar.coverage.exclusions="**/Program.cs" /d:sonar.qualitygate.wait=true /d:sonar.scanner.skipJreProvisioning=true /d:sonar.branch.name="$sonarBranch"
    $null = Test-Exit $beginSonar
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

if ($sonarCarried) { $null = Test-StepCarried $build }
else {
    $global:LASTEXITCODE = $null
    dotnet build --no-incremental --configuration Release /p:RestoreLockedMode=true -warnaserror
    $null = Test-Exit $build
}

if (-not (Test-StepCarried 'jb inspectcode')) {
    if (Test-Path $sarif) { Remove-Item $sarif -Force }
    dotnet jb inspectcode "$repo\Identity.slnx" --no-build -e=WARNING --output="$sarif"
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
    Test-Trx $unit $unitTrx $global:LASTEXITCODE 1
}

if (-not (Test-StepCarried 'Install SqlPackage')) {
    if (Get-Command sqlpackage -ErrorAction SilentlyContinue) { Write-Row 'Install SqlPackage' 'PASS' 'present on PATH' }
    else { Stop-Gate 'Install SqlPackage' 'not on PATH' }
}
if (-not (Test-StepCarried $schema)) {
    sqllocaldb start MSSQLLocalDB | Out-Null
    $global:LASTEXITCODE = $null
    sqlpackage /Action:Publish /SourceFile:Identity.Data/bin/Release/Identity.Data.dacpac /TargetConnectionString:"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=$testCatalog;Integrated Security=True;TrustServerCertificate=True"
    $null = Test-Exit $schema
}

$global:LASTEXITCODE = $null
pwsh "$repo\Identity.Tests.E2E\bin\Release\net10.0\playwright.ps1" install chromium
$null = Test-Exit 'Install Playwright browsers'

if (-not (Test-StepCarried $e2e)) {
    if (Test-Path $e2eTrx) { Remove-Item $e2eTrx -Force }
    sqllocaldb start MSSQLLocalDB | Out-Null
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:SqlConnectionStringBuilder__InitialCatalog = $testCatalog
    $env:PasskeyOrigin = 'https://127.0.0.1'
    $global:LASTEXITCODE = $null
    dotnet-coverage collect `
        "dotnet test --project Identity.Tests.E2E --no-build --configuration Release -- --filter-trait Category=E2E --stop-on-fail on --report-xunit-trx --report-xunit-trx-filename e2e-tests.trx --results-directory=Identity.Tests.E2E/bin/Release/net10.0/TestResults" `
        -f xml -o "coverage-e2e.xml" -s "coverage.settings.xml"
    Test-Trx $e2e $e2eTrx $global:LASTEXITCODE 1
}

if (-not $sonarCarried) {
    $global:LASTEXITCODE = $null
    dotnet-sonarscanner end /d:sonar.token="$env:SONAR_TOKEN"
    $null = Test-Exit $endSonar
}

if (-not (Test-StepCarried $stryker)) {
    if ([string]::IsNullOrWhiteSpace($env:STRYKER_DASHBOARD_API_KEY)) { Stop-Gate $stryker 'STRYKER_DASHBOARD_API_KEY is not in the environment' }
    Push-Location (Join-Path $repo 'Identity')
    $global:LASTEXITCODE = $null
    dotnet stryker --config-file ../stryker-config.json --version $sonarBranch | Tee-Object -Variable strykerOutput
    $strykerExit = $global:LASTEXITCODE
    Pop-Location
    if ($strykerOutput -match 'Failed to upload report to the dashboard') { Stop-Gate $stryker 'the report did not reach the dashboard' }
    $global:LASTEXITCODE = $strykerExit
    $null = Test-Exit $stryker
}

Write-Row 'dotnet publish / uploads / deploy' 'NOT RUN' 'delivery steps, not checks'
Complete-Gate
