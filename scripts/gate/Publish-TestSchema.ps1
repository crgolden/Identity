$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$developmentSql = (Get-Content -Raw (Join-Path $repo 'Identity\appsettings.Development.json') | ConvertFrom-Json).SqlConnectionStringBuilder
$catalog = $env:SqlConnectionStringBuilder__InitialCatalog
if ([string]::IsNullOrWhiteSpace($catalog)) {
    Write-Host 'SqlConnectionStringBuilder__InitialCatalog names no catalog, so there is no test database to publish the schema to'
    exit 1
}
$target = [Data.Common.DbConnectionStringBuilder]::new()
$target['Data Source'] = $env:SqlConnectionStringBuilder__DataSource ?? $developmentSql.DataSource
$target['Initial Catalog'] = $catalog
if ($env:SqlConnectionStringBuilder__UserID) {
    $target['User ID'] = $env:SqlConnectionStringBuilder__UserID
    $target['Password'] = $env:SqlConnectionStringBuilder__Password
    $target['Encrypt'] = 'True'
    $target['TrustServerCertificate'] = 'False'
}
else {
    $target['Integrated Security'] = [string]$developmentSql.IntegratedSecurity
    $target['TrustServerCertificate'] = 'True'
}
Write-Host "Publishing Identity.Data to $catalog on $($target['Data Source'])"
$source = Join-Path $repo 'Identity.Data\bin\Release\Identity.Data.dacpac'
sqlpackage /Action:Publish "/SourceFile:$source" "/TargetConnectionString:$($target.ConnectionString)"
exit $LASTEXITCODE
