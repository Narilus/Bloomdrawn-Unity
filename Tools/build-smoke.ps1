[CmdletBinding()]
param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot ".."),
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"
$resolvedProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$unity = Get-Command unity -ErrorAction Stop

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $resolvedProjectPath "Builds/Smoke/Bloomdrawn.exe"
}

$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

& $unity.Source build $resolvedProjectPath `
    --target StandaloneWindows64 `
    --execute-method Bloomdrawn.Editor.Build.BloomdrawnBuild.PerformWindowsSmokeBuild `
    --output-path $OutputPath `
    --allow-dirty-build

if ($LASTEXITCODE -ne 0) {
    throw "Windows smoke build failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath $OutputPath)) {
    throw "Windows smoke build did not produce $OutputPath."
}

Write-Host "Windows smoke build passed: $OutputPath"
