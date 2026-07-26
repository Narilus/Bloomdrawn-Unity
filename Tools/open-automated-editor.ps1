[CmdletBinding()]
param(
    [Parameter()]
    [string]$ProjectPath = (Get-Location).Path,

    [Parameter()]
    [string]$UnityPath,

    [Parameter()]
    [string[]]$AdditionalArguments = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Normalize-ProjectPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $resolved = [System.IO.Path]::GetFullPath($Path)
    return $resolved.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
}

function Get-PinnedUnityVersion {
    param([Parameter(Mandatory = $true)][string]$Project)

    $versionFile = Join-Path $Project 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path -LiteralPath $versionFile)) {
        throw "ProjectVersion.txt was not found at '$versionFile'."
    }

    $versionLine = Select-String -LiteralPath $versionFile -Pattern '^m_EditorVersion:\s*(.+)$' | Select-Object -First 1
    if ($null -eq $versionLine) {
        throw "Could not read m_EditorVersion from '$versionFile'."
    }

    return $versionLine.Matches[0].Groups[1].Value.Trim()
}

function Resolve-UnityEditorPath {
    param(
        [Parameter(Mandatory = $true)][string]$PinnedVersion,
        [AllowNull()][string]$ExplicitPath
    )

    $candidates = @()

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        $candidates += $ExplicitPath
    }

    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EDITOR_PATH)) {
        $candidates += $env:UNITY_EDITOR_PATH
    }

    $candidates += "C:\Program Files\Unity\Hub\Editor\$PinnedVersion\Editor\Unity.exe"
    $candidates += "C:\Program Files\Unity\$PinnedVersion\Editor\Unity.exe"

    $unityCli = Get-Command unity -ErrorAction SilentlyContinue
    if ($null -ne $unityCli) {
        $installDirectory = (& $unityCli.Source editors path $PinnedVersion 2>$null | Select-Object -Last 1)
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($installDirectory)) {
            $candidates += (Join-Path $installDirectory.Trim() 'Editor\Unity.exe')
        }
    }

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    throw "Could not locate Unity $PinnedVersion. Pass -UnityPath or set UNITY_EDITOR_PATH to the pinned Editor executable."
}

function Get-ProjectUnityProcesses {
    param([Parameter(Mandatory = $true)][string]$Project)

    $matches = @()
    $unityProcesses = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'")

    foreach ($process in $unityProcesses) {
        $commandLine = [string]$process.CommandLine
        if ([string]::IsNullOrWhiteSpace($commandLine)) {
            continue
        }

        $projectMatch = [regex]::Match(
            $commandLine,
            '(?i)(?:^|\s)-projectPath\s+(?:"([^"]+)"|(\S+))'
        )

        $processProject = $null
        if ($projectMatch.Success) {
            $raw = if ($projectMatch.Groups[1].Success) { $projectMatch.Groups[1].Value } else { $projectMatch.Groups[2].Value }
            try {
                $processProject = Normalize-ProjectPath -Path $raw
            }
            catch {
                $processProject = $raw
            }
        }

        $sameProject = if (-not [string]::IsNullOrWhiteSpace($processProject)) {
            [string]::Equals($processProject, $Project, [System.StringComparison]::OrdinalIgnoreCase)
        }
        else {
            $commandLine.IndexOf($Project, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
        }

        if (-not $sameProject) {
            continue
        }

        $automated = [regex]::IsMatch($commandLine, '(?i)(?:^|\s)-automated(?:\s|$)')
        $matches += [pscustomobject]@{
            processId = [int]$process.ProcessId
            automated = $automated
        }
    }

    return $matches
}

$project = Normalize-ProjectPath -Path $ProjectPath
$pinnedVersion = Get-PinnedUnityVersion -Project $project
$editorPath = Resolve-UnityEditorPath -PinnedVersion $pinnedVersion -ExplicitPath $UnityPath

$existing = @(Get-ProjectUnityProcesses -Project $project)
if ($existing.Count -gt 1) {
    throw "Multiple Unity Editor processes are already associated with '$project'. Refusing to guess which process is safe for agent control."
}

if ($existing.Count -eq 1) {
    if (-not $existing[0].automated) {
        throw "Unity is already open for '$project' without -automated (PID $($existing[0].processId)). Close/restart it with explicit user approval before agent-controlled Editor work. This script will not terminate it."
    }

    [pscustomobject]@{
        status             = 'already_running_automated'
        projectPath        = $project
        pinnedUnityVersion = $pinnedVersion
        unityPath          = $editorPath
        processId          = $existing[0].processId
        automated          = $true
    } | ConvertTo-Json -Depth 4
    exit 0
}

$arguments = @(
    '-projectPath',
    ('"{0}"' -f $project),
    '-automated'
)

foreach ($argument in $AdditionalArguments) {
    if ([string]::IsNullOrWhiteSpace($argument)) {
        continue
    }

    if ($argument -match '^(?i)-batchmode$') {
        throw '-batchmode is not permitted through the interactive automated Editor launcher.'
    }

    if ($argument -match '^(?i)-projectPath$') {
        throw '-projectPath is owned by this launcher and may not be overridden through -AdditionalArguments.'
    }

    $arguments += $argument
}

$process = Start-Process -FilePath $editorPath -ArgumentList $arguments -PassThru

[pscustomobject]@{
    status             = 'launched'
    projectPath        = $project
    pinnedUnityVersion = $pinnedVersion
    unityPath          = $editorPath
    processId          = $process.Id
    automated          = $true
    batchMode          = $false
} | ConvertTo-Json -Depth 4

exit 0
