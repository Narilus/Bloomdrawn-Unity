[CmdletBinding()]
param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot ".."),
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$resolvedProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$unity = Get-Command unity -ErrorAction Stop

function Assert-FileContains {
    param(
        [string]$Path,
        [string]$ExpectedText
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required file is missing: $Path"
    }

    if (-not (Select-String -LiteralPath $Path -SimpleMatch $ExpectedText -Quiet)) {
        throw "Expected '$ExpectedText' in $Path"
    }
}

Assert-FileContains (Join-Path $resolvedProjectPath "ProjectSettings/ProjectVersion.txt") "m_EditorVersion: 6000.5."
Assert-FileContains (Join-Path $resolvedProjectPath "Assets/Bloomdrawn/Engine/Bloomdrawn.Engine.asmdef") '"noEngineReferences": true'
Assert-FileContains (Join-Path $resolvedProjectPath "Assets/Bloomdrawn/Content/Bloomdrawn.Content.asmdef") '"noEngineReferences": true'

foreach ($pureAssemblyRoot in @("Assets/Bloomdrawn/Engine", "Assets/Bloomdrawn/Content")) {
    $matches = Get-ChildItem -LiteralPath (Join-Path $resolvedProjectPath $pureAssemblyRoot) -Filter *.cs -Recurse |
        Select-String -Pattern '(^|\s)UnityEngine\b|(^|\s)UnityEditor\b'
    if ($matches) {
        throw "Pure assembly source references Unity APIs: $($matches.Path -join ', ')"
    }
}

if (-not $SkipTests) {
    $resultsDirectory = Join-Path $resolvedProjectPath "Temp/TestResults"
    New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null

    & $unity.Source test $resolvedProjectPath --mode EditMode --output (Join-Path $resultsDirectory "editmode.xml")
    if ($LASTEXITCODE -ne 0) {
        throw "Edit Mode tests failed with exit code $LASTEXITCODE."
    }

    & $unity.Source test $resolvedProjectPath --mode PlayMode --output (Join-Path $resultsDirectory "playmode.xml")
    if ($LASTEXITCODE -ne 0) {
        throw "Play Mode tests failed with exit code $LASTEXITCODE."
    }
}

Write-Host "M0 validation passed for $resolvedProjectPath."
