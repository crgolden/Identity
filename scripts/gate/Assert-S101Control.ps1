param([Parameter(Mandatory)][string]$BackupPath)

$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$project = Join-Path $repo 'Identity\Identity.csproj'
if (Test-Path -LiteralPath $BackupPath) {
    [IO.File]::WriteAllBytes($project, [IO.File]::ReadAllBytes($BackupPath))
    Remove-Item -LiteralPath $BackupPath -Force
    Write-Host 'Restored Identity.csproj from the backup an interrupted S101 control left behind'
}
$projectBytes = [IO.File]::ReadAllBytes($project)
$projectText = [IO.File]::ReadAllText($project)
$includePattern = '(?m)^[ \t]*<AdditionalFiles Include="\.\.\\CustomDictionary\.xml" />\r?\n'
if ($projectText -notmatch $includePattern) {
    Write-Host 'the CustomDictionary.xml AdditionalFiles include is absent from Identity.csproj'
    exit 1
}
$expectedNames = 'ICAPTCHAService', 'ReCAPTCHAService', 'ReCAPTCHAOptions', 'CAPTCHAVerdict'
$named = @()
[IO.File]::WriteAllBytes($BackupPath, $projectBytes)
try {
    [IO.File]::WriteAllText($project, ($projectText -replace $includePattern))
    $controlLog = (dotnet build "$project" --configuration Release /p:RestoreLockedMode=true 2>&1 | Out-String)
    $named = @($expectedNames | Where-Object { $controlLog -match ("S101[^\r\n]*'" + $_ + "'") })
}
finally {
    [IO.File]::WriteAllBytes($project, $projectBytes)
}
if (-not [Linq.Enumerable]::SequenceEqual([IO.File]::ReadAllBytes($project), $projectBytes)) {
    Write-Host "the control could not restore Identity.csproj; its bytes are in $BackupPath"
    exit 1
}
Remove-Item -LiteralPath $BackupPath -Force
if ($named.Count -ne $expectedNames.Count) {
    Write-Host "S101 named $($named.Count) of $($expectedNames.Count) without the dictionary, so the dictionary is not what silences it"
    exit 1
}
Write-Host "S101 named all $($named.Count) without the dictionary: $($named -join ', ')"
exit 0
