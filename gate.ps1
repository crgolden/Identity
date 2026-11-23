param([string]$Goal, [string]$Tests, [string]$TestProject, [string]$Only)

$ErrorActionPreference = 'Continue'
$gateCommon = $env:GATE_LIBRARY
if (-not $gateCommon -or -not (Test-Path -LiteralPath $gateCommon -PathType Leaf)) {
    Write-Host 'GATE: FAILED (GATE_LIBRARY names no gate library script; README.md, section Gate, says how to plug one in or run the steps without one)'
    exit 1
}
. $gateCommon
$gateLibraryFolder = Split-Path -Parent $gateCommon
. (Join-Path $gateLibraryFolder 'SelectedTests.ps1')
$gateOutput = if (Get-Command Get-GateOutputDirectory -ErrorAction SilentlyContinue) {
    Get-GateOutputDirectory (Split-Path -Leaf $PSScriptRoot)
} else {
    (New-Item -ItemType Directory -Force -Path (Join-Path ([IO.Path]::GetTempPath()) "claude\crgolden-gates\$(Split-Path -Leaf $PSScriptRoot)")).FullName
}
Register-GateStep @('Install dotnet-coverage', 'Restore local tools',
    'S101 dictionary control', 'Begin Sonar analysis',
    'Build with dotnet', 'jb inspectcode', 'Unit tests (Identity.Tests.Unit', 'Install SqlPackage',
    'Deploy E2E test database schema', 'Install Playwright browsers', 'E2E tests (Identity.Tests.E2E',
    'Cucumber messages report (Identity.Tests.E2E', 'Playwright artifact temp folder (Identity.Tests.E2E',
    'Publish command reaches the CLI',
    'Integration tests (Identity.Tests.Integration',
    'End Sonar analysis', 'Fail on open Sonar issues', 'Run Stryker mutation tests')
Register-StepInput 'Install dotnet-coverage' @('*')
Register-StepInput 'Restore local tools' @('dotnet-tools.json')
Register-StepInput 'S101 dictionary control' @('*')
Register-StepInput 'Begin Sonar analysis' @('*')
Register-StepInput 'Build with dotnet' @('*')
Register-StepInput 'jb inspectcode' @('*')
Register-StepInput 'Unit tests (Identity.Tests.Unit' @('*', '!Identity.Tests.E2E/*', '!Identity.Tests.Integration/*', '!Identity.Tests.Load/*')
Register-StepInput 'Install SqlPackage' @('*')
Register-StepInput 'Deploy E2E test database schema' @('*', '!Identity.Tests.Unit/*')
Register-StepInput 'Install Playwright browsers' @('*', '!Identity.Tests.Unit/*')
Register-StepInput 'E2E tests (Identity.Tests.E2E' @('*', '!Identity.Tests.Unit/*')
Register-StepInput 'Cucumber messages report (Identity.Tests.E2E' @('*', '!Identity.Tests.Unit/*')
Register-StepInput 'Playwright artifact temp folder (Identity.Tests.E2E' @('*', '!Identity.Tests.Unit/*')
Register-StepInput 'Publish command reaches the CLI' @('*')
Register-StepInput 'Integration tests (Identity.Tests.Integration' @('*', '!Identity.Tests.Unit/*')
Register-StepInput 'End Sonar analysis' @('*')
Register-StepInput 'Fail on open Sonar issues' @('*')
Register-StepInput 'Run Stryker mutation tests' @('*')
$repo = $PSScriptRoot
$sarif = (Join-Path $gateOutput 'identity-inspect.sarif')
$unitTrx = Join-Path $repo 'Identity.Tests.Unit\bin\Release\net11.0\TestResults\unit-tests.trx'
$e2eTrx = Join-Path $repo 'Identity.Tests.E2E\bin\Release\net11.0\TestResults\e2e-tests.trx'
$integrationTrx = Join-Path $repo 'Identity.Tests.Integration\bin\Release\net11.0\TestResults\integration-tests.trx'
$testCatalog = $env:SqlConnectionStringBuilder__InitialCatalog ?? 'IdentityTest'
$developmentSql = (Get-Content -Raw (Join-Path $repo 'Identity\appsettings.Development.json') | ConvertFrom-Json).SqlConnectionStringBuilder
$dataSource = $env:SqlConnectionStringBuilder__DataSource ?? $developmentSql.DataSource
$targetConnection = [Data.Common.DbConnectionStringBuilder]::new()
$targetConnection['Data Source'] = $dataSource
$targetConnection['Initial Catalog'] = $testCatalog
if ($env:SqlConnectionStringBuilder__UserID) {
    $targetConnection['User ID'] = $env:SqlConnectionStringBuilder__UserID
    $targetConnection['Password'] = $env:SqlConnectionStringBuilder__Password
    $targetConnection['Encrypt'] = 'True'
    $targetConnection['TrustServerCertificate'] = 'False'
}
else {
    $targetConnection['Integrated Security'] = [string]$developmentSql.IntegratedSecurity
    $targetConnection['TrustServerCertificate'] = 'True'
}
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
$e2eOutput = Join-Path $repo 'Identity.Tests.E2E\bin\Release\net11.0'
$e2eFloor = [int](Get-Content (Join-Path $repo 'Identity.Tests.E2E\e2e-settings.json') -Raw | ConvertFrom-Json).executedTestFloor
$cucumberMessagesRelative = Join-Path 'Identity.Tests.E2E\bin\Release\net11.0' (Get-Content (Join-Path $repo 'Identity.Tests.E2E\reqnroll.json') -Raw | ConvertFrom-Json).formatters.message.outputFilePath
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
    Invoke-SelectedTest $repo $TestProject $Tests {
        if ($TestProject -in 'Identity.Tests.E2E', 'Identity.Tests.Integration') {
            Start-LocalDbWhenTargeted
            $env:ASPNETCORE_ENVIRONMENT = 'Development'
            $env:SqlConnectionStringBuilder__InitialCatalog = $testCatalog
            $env:PasskeyOrigin = 'https://127.0.0.1'
            pwsh "$repo\Identity.Tests.E2E\bin\Release\net11.0\playwright.ps1" install chromium
        }
    }
}
$probeProjects = @('Identity.Tests.Unit', 'Identity.Tests.E2E', 'Identity.Tests.Integration')
if ($Only) {
    $probeFile, $probeTest = $Only -split '::', 2
    $probeFile = "$probeFile".Trim().Replace('\', '/')
    $probeTest = "$probeTest".Trim()
    $probeProject = $probeFile.Split('/')[0]
    if (-not $probeTest -or $probeTest.Contains(',') -or $probeProject -notin $probeProjects -or
            -not (Test-Path -LiteralPath (Join-Path $repo $probeFile) -PathType Leaf)) {
        Write-Host "PROBE: FAILED (a probe runs exactly one test, named by the file it lives in: -Only '<a test file under $($probeProjects -join ' | ')>::<test method>')"
        exit 1
    }
    $probeClass = if ($probeFile.EndsWith('.cs', [StringComparison]::OrdinalIgnoreCase)) { [IO.Path]::GetFileNameWithoutExtension($probeFile) } else { '*' }
    if ($probeProject -ne $probeProjects[0]) {
        Start-LocalDbWhenTargeted
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        $env:SqlConnectionStringBuilder__InitialCatalog = $testCatalog
        $env:PasskeyOrigin = 'https://127.0.0.1'
        dotnet build $probeProject --configuration Release
        pwsh "$repo\Identity.Tests.E2E\bin\Release\net11.0\playwright.ps1" install chromium
    }
    $probeResults = Join-Path $gateOutput 'probe-results'
    $probeTrx = Join-Path $probeResults 'probe.trx'
    if (Test-Path -LiteralPath $probeTrx) { Remove-Item -LiteralPath $probeTrx -Force }
    Write-Host "PROBE: running one test, '$probeTest' of $probeClass in $probeProject; no gate record or verdict is written"
    $global:LASTEXITCODE = $null
    dotnet test --project $probeProject --configuration Release -- --filter-query "/*/*/$probeClass/$probeTest" --report-xunit-trx --report-xunit-trx-filename probe.trx --results-directory=$probeResults
    $probeExit = $global:LASTEXITCODE
    if (-not (Test-Path -LiteralPath $probeTrx)) { Write-Host "PROBE: FAILED (no TRX, exit $probeExit)"; exit 1 }
    [xml]$probeDoc = Get-Content -LiteralPath $probeTrx -Raw
    $counters = $probeDoc.TestRun.ResultSummary.Counters
    if ($probeExit -ne 0 -or [int]$counters.executed -ne 1 -or [int]$counters.failed -ne 0) {
        Write-Host "PROBE: FAILED (a probe runs one test, and '$Only' executed $($counters.executed), failed $($counters.failed), exit $probeExit)"
        exit 1
    }
    Write-Host "PROBE: PASSED for the one test named, against the last build's binaries; no verdict was saved"
    exit 0
}
exit (Invoke-Gate {
Initialize-GateState 'Identity' $repo
Get-OwedStep | Register-OwedStep
Invoke-GateLibraryStep

$coverageTool = 'Install dotnet-coverage'
Invoke-GateStep -Name $coverageTool -Body {
    if (Get-Command dotnet-coverage -ErrorAction SilentlyContinue) { Write-Row $coverageTool 'PASS' 'present on PATH' }
    else { Stop-Gate $coverageTool 'not on PATH' }
}
$restore = 'Restore local tools (dotnet tool restore)'
Invoke-GateStep -Name $restore -Body {
    $global:LASTEXITCODE = $null
    dotnet tool restore
    Assert-StepExit $restore
}

Invoke-GateStep -Name $s101 -Body {
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
    Invoke-GateStep -Name $beginSonar -Always -Body {
        $env:JAVA_HOME = Get-SonarScannerJavaHome $beginSonar
        $global:LASTEXITCODE = $null
        dotnet-sonarscanner begin /k:"crgolden_Identity" /o:"crgolden" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.cs.opencover.reportsPaths="coverage.opencover.xml" /d:sonar.cs.vscoveragexml.reportsPaths="coverage-e2e.xml,coverage-integration.xml" /d:sonar.exclusions="**/bin/**,**/obj/**" /d:sonar.coverage.exclusions="**/Program.cs,**/gate.ps1" /d:sonar.qualitygate.wait=true /d:sonar.scanner.skipJreProvisioning=true /d:sonar.scm.use.blame.algorithm=GIT_NATIVE_BLAME /d:sonar.branch.name="$sonarBranch"
        Assert-StepExit $beginSonar
    }
}

if ($sonarCarried) { $null = Test-StepCarried $build }
else {
    Invoke-GateStep -Name $build -Always -Body {
        $global:LASTEXITCODE = $null
        dotnet build --no-incremental --configuration Release /p:RestoreLockedMode=true -warnaserror
        Assert-StepExit $build
    }
}

Invoke-GateStep -Name 'jb inspectcode' -Needs @($build, $restore) -Body {
    if (Test-Path $sarif) { Remove-Item $sarif -Force }
    dotnet jb inspectcode "$repo\Identity.slnx" --no-build -e=WARNING --caches-home="$(New-InspectCodeCache $gateOutput)" --output="$sarif"
    Test-Sarif $sarif
}

Invoke-GateStep -Name $unit -Needs @($build, $restore) -Body {
    if (Test-Path $unitTrx) { Remove-Item $unitTrx -Force }
    $global:LASTEXITCODE = $null
    dotnet coverlet Identity.Tests.Unit\bin\Release\net11.0 `
        --target "dotnet" `
        --targetargs "test --project Identity.Tests.Unit --no-build --configuration Release -- --filter-trait Category=Unit --report-xunit-trx --report-xunit-trx-filename unit-tests.trx --results-directory=Identity.Tests.Unit/bin/Release/net11.0/TestResults" `
        --format opencover --output "coverage.opencover.xml" `
        --skipautoprops --exclude-by-attribute GeneratedCodeAttribute --exclude-by-file "**/obj/**" `
        --exclude-by-file "**/Program.cs" --does-not-return-attribute DoesNotReturnAttribute `
        --include "[Identity]*" --exclude "[Identity]*Pages_*"
    Test-Trx $unit $unitTrx $global:LASTEXITCODE -floor 1
}

$sqlPackageTool = 'Install SqlPackage'
Invoke-GateStep -Name $sqlPackageTool -Body {
    if (Get-Command sqlpackage -ErrorAction SilentlyContinue) { Write-Row $sqlPackageTool 'PASS' 'present on PATH' }
    else { Stop-Gate $sqlPackageTool 'not on PATH' }
}
Invoke-GateStep -Name $schema -Needs @($build, $sqlPackageTool) -Body {
    Start-LocalDbWhenTargeted
    $global:LASTEXITCODE = $null
    sqlpackage /Action:Publish /SourceFile:Identity.Data/bin/Release/Identity.Data.dacpac /TargetConnectionString:"$($targetConnection.ConnectionString)"
    Assert-StepExit $schema
}

$playwright = 'Install Playwright browsers'
Invoke-GateStep -Name $playwright -Needs @($build) -Always -Body {
    Install-PlaywrightBrowser $playwright { pwsh "$repo\Identity.Tests.E2E\bin\Release\net11.0\playwright.ps1" install --dry-run chromium } { pwsh "$repo\Identity.Tests.E2E\bin\Release\net11.0\playwright.ps1" install chromium }
}

$e2eRun = @{ Ran = $false }
Invoke-GateStep -Name $e2e -Needs @($build, $coverageTool, $schema, $playwright) -Body {
    $e2eRun.Ran = $true
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
        "dotnet test --project Identity.Tests.E2E --no-build --configuration Release -- --filter-trait Category=E2E --report-xunit-trx --report-xunit-trx-filename e2e-tests.trx --results-directory=Identity.Tests.E2E/bin/Release/net11.0/TestResults" `
        -f xml -o "coverage-e2e.xml" -s "coverage.settings.xml"
    Test-Trx $e2e $e2eTrx $global:LASTEXITCODE -floor $e2eFloor
}
$e2eChecks = @($cucumberReport, $artifactTempStep, $publishStep)
if ($e2eRun.Ran) {
    Invoke-GateStep -Name $cucumberReport -Always -Body {
        if (-not (Test-Path $cucumberMessages)) { Stop-Gate $cucumberReport "missing: $cucumberMessages" }
        $finishedCases = @(Select-String -LiteralPath $cucumberMessages -SimpleMatch '"testCaseFinished"').Count
        if ($finishedCases -lt $e2eFloor) { Stop-Gate $cucumberReport "$finishedCases testCaseFinished messages, floor $e2eFloor" }
        Write-Row $cucumberReport 'PASS' "$finishedCases testCaseFinished messages, floor $e2eFloor"
    }
    Invoke-GateStep -Name $artifactTempStep -Always -Body {
        $leakedSessions = if (Test-Path $artifactTemp) { @(Get-ChildItem -LiteralPath $artifactTemp -Recurse -File).Count } else { 0 }
        if ($leakedSessions -gt 0) { Stop-Gate $artifactTempStep "$leakedSessions file(s) left in ${artifactTemp}: artifacts were never finalized" }
        Write-Row $artifactTempStep 'PASS' "no files left in $artifactTemp"
    }
    Invoke-GateStep -Name $publishStep -Needs @($cucumberReport) -Always -Body {
        Test-PublishCommandLine $publishStep (Join-Path $repo '.github\workflows\main_crgolden-identity.yml') 'Identity' $repo $cucumberMessagesRelative
    }
}
elseif (@(Get-FailedStep -Step @($e2e)).Count) {
    Write-PrerequisiteUnreached -Step $e2eChecks -Failed @($e2e)
}

Invoke-GateStep -Name $integration -Needs @($build, $coverageTool, $schema) -Body {
    if (Test-Path $integrationTrx) { Remove-Item $integrationTrx -Force }
    Start-LocalDbWhenTargeted
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:SqlConnectionStringBuilder__InitialCatalog = $testCatalog
    $env:PasskeyOrigin = 'https://127.0.0.1'
    $global:LASTEXITCODE = $null
    dotnet-coverage collect `
        "dotnet test --project Identity.Tests.Integration --no-build --configuration Release -- --filter-trait Category=Integration --report-xunit-trx --report-xunit-trx-filename integration-tests.trx --results-directory=Identity.Tests.Integration/bin/Release/net11.0/TestResults" `
        -f xml -o "coverage-integration.xml" -s "coverage.settings.xml"
    Test-Trx $integration $integrationTrx $global:LASTEXITCODE -floor 27
}

if (-not $sonarCarried) {
    Invoke-GateStep -Name $endSonar -Needs @($beginSonar, $build, $unit, $e2e, $integration) -Always -Body {
        $global:LASTEXITCODE = $null
        dotnet-sonarscanner end
        Assert-StepExit $endSonar
    }
    Invoke-GateStep -Name $sonarIssues -Needs @($endSonar) -Always -Body {
        Assert-NoOpenSonarIssue $sonarIssues 'crgolden_Identity' $sonarBranch $sonarStartedAt
    }
}

Invoke-GateStep -Name $stryker -Needs @($build, $restore) -Body {
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
    node (Join-Path $gateLibraryFolder 'upload-stryker-report.mjs') (Join-Path $strykerOutputDir 'reports\mutation-report.json') $strykerConfig.'project-info'.name $sonarBranch
    Assert-StepExit $stryker
}

Write-Row 'Publish E2E scenario results / Upload Cucumber report / dotnet publish / uploads / deploy' 'NOT RUN' 'delivery steps, not checks'
Complete-Gate
})
