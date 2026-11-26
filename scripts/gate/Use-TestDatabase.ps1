[string[]]$Command = @($args)
if (-not $Command.Count) {
    Write-Host 'Use-TestDatabase.ps1 was given no command to run'
    exit 1
}
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $env:SqlConnectionStringBuilder__DataSource) { sqllocaldb start MSSQLLocalDB | Out-Null }
Set-Location -LiteralPath $repo
$global:LASTEXITCODE = $null
& $Command[0] @($Command | Select-Object -Skip 1)
if ($null -eq $global:LASTEXITCODE) { exit 1 }
exit $global:LASTEXITCODE
