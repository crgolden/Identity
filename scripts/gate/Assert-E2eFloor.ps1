$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$output = Join-Path $repo 'Identity.Tests.E2E\bin\Release\net11.0'
$floor = [int](Get-Content -LiteralPath (Join-Path $repo 'Identity.Tests.E2E\e2e-settings.json') -Raw | ConvertFrom-Json).executedTestFloor
if ($floor -le 0) {
    Write-Host 'Identity.Tests.E2E\e2e-settings.json carries no positive executedTestFloor'
    exit 1
}
$trx = Join-Path $output 'TestResults\e2e-tests.trx'
if (-not (Test-Path -LiteralPath $trx)) {
    Write-Host "$trx is missing: the E2E suite never reported"
    exit 1
}
$executed = [int]([xml](Get-Content -LiteralPath $trx -Raw)).TestRun.ResultSummary.Counters.executed
$messages = Join-Path $output (Get-Content -LiteralPath (Join-Path $repo 'Identity.Tests.E2E\reqnroll.json') -Raw | ConvertFrom-Json).formatters.message.outputFilePath
$finished = if (Test-Path -LiteralPath $messages) { @(Select-String -LiteralPath $messages -SimpleMatch '"testCaseFinished"').Count } else { 0 }
if ($executed -lt $floor -or $finished -lt $floor) {
    Write-Host "E2E executed $executed and the Cucumber report finished $finished, below the floor of $floor"
    exit 1
}
$playwright = (Get-Content -LiteralPath (Join-Path $repo 'Identity\appsettings.Development.json') -Raw | ConvertFrom-Json).PlaywrightSettings
$artifactTemp = [IO.Path]::Combine($output, $playwright.TestResultsFolderName, $playwright.ArtifactsFolderName, $playwright.TempFolderName)
$leaked = if (Test-Path -LiteralPath $artifactTemp) { @(Get-ChildItem -LiteralPath $artifactTemp -Recurse -File).Count } else { 0 }
if ($leaked -gt 0) {
    Write-Host "$leaked file(s) left in ${artifactTemp}: artifacts were never finalized"
    exit 1
}
Write-Host "E2E executed $executed, Cucumber report finished $finished, floor $floor; no files left in $artifactTemp"
exit 0
