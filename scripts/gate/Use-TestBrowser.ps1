[string[]]$arguments = @($args)
if ($arguments.Count -lt 2) {
    Write-Host 'Use-TestBrowser.ps1 takes the project to build, then the command to run'
    exit 1
}
$project = $arguments[0]
[string[]]$Command = @($arguments | Select-Object -Skip 1)
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location -LiteralPath $repo
dotnet build $project --configuration Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
pwsh (Join-Path $repo 'Identity.Tests.E2E\bin\Release\net11.0\playwright.ps1') install chromium
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$global:LASTEXITCODE = $null
& $Command[0] @($Command | Select-Object -Skip 1)
if ($null -eq $global:LASTEXITCODE) { exit 1 }
exit $global:LASTEXITCODE
