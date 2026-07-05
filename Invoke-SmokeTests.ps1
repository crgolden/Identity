#Requires -Version 7
# Runs the smoke test suite.
# Credentials are read from User Secrets (ID aspnet-Identity-149346d0-999f-4a74-8ff7-2a92d39790f2)
# so they never need to be set as OS environment variables.
#
# Local (default): targets https://localhost:7261 — requires Identity running locally with
# ReCAPTCHA:SmokeTestEmail set via its User Secrets to match TestEmail.
#
# Deployed: pass -BaseUrl https://crgolden-identity.azurewebsites.net
param(
    [string]$BaseUrl = "https://crgolden-identity.azurewebsites.net"
)

$secretsPath = Join-Path $env:APPDATA "Microsoft\UserSecrets\aspnet-Identity-149346d0-999f-4a74-8ff7-2a92d39790f2\secrets.json"
$secrets     = Get-Content $secretsPath -Raw | ConvertFrom-Json

$env:SmokeBaseUrl = $BaseUrl
$env:SmokeDataSource = $secrets.SmokeDataSource
$env:TestEmail = $secrets.TestEmail
$env:TestPassword = $secrets.TestPassword
$env:SqlConnectionStringBuilder__InitialCatalog = $secrets.SqlConnectionStringBuilder.InitialCatalog
$env:SqlConnectionStringBuilder__UserID         = $secrets.SqlConnectionStringBuilder.UserID
$env:SqlConnectionStringBuilder__Password       = $secrets.SqlConnectionStringBuilder.Password

try
{
    & ".\Identity.Tests.E2E\bin\Debug\net10.0\Identity.Tests.E2E.exe" -trait "Category=Smoke" -showLiveOutput
}
finally
{
    Remove-Item Env:SmokeBaseUrl, Env:TestEmail, Env:TestPassword `
        Env:SmokeDataSource, Env:SqlConnectionStringBuilder__InitialCatalog, `
        Env:SqlConnectionStringBuilder__UserID, Env:SqlConnectionStringBuilder__Password `
        -ErrorAction SilentlyContinue
}
