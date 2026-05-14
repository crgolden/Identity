#Requires -Version 7
# Runs the smoke test suite.
# Credentials are read from User Secrets (ID aspnet-Identity-149346d0-999f-4a74-8ff7-2a92d39790f2)
# so they never need to be set as OS environment variables.
#
# Local (default): targets https://localhost:7261 — requires Identity running locally with
# ReCAPTCHA:SmokeTestEmail set via its User Secrets to match TEST_USERNAME.
#
# Deployed: pass -BaseUrl https://crgolden-identity.azurewebsites.net
param(
    [string]$BaseUrl = "https://crgolden-identity.azurewebsites.net"
)

$secretsPath = Join-Path $env:APPDATA "Microsoft\UserSecrets\aspnet-Identity-149346d0-999f-4a74-8ff7-2a92d39790f2\secrets.json"
$secrets     = Get-Content $secretsPath -Raw | ConvertFrom-Json

$env:SMOKE_BASE_URL = $BaseUrl
$env:TEST_USERNAME  = $secrets.TEST_USERNAME
$env:TEST_PASSWORD  = $secrets.TEST_PASSWORD

try
{
    & ".\Identity.Tests\bin\Debug\net10.0\Identity.Tests.exe" -trait "Category=Smoke" -showLiveOutput
}
finally
{
    Remove-Item Env:SMOKE_BASE_URL, Env:TEST_USERNAME, Env:TEST_PASSWORD -ErrorAction SilentlyContinue
}
