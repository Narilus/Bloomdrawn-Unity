[CmdletBinding()]
param(
    [Parameter()]
    [string]$ProjectPath = (Get-Location).Path,

    [Parameter()]
    [switch]$RequireAutomated
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Normalize-ProjectPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $resolved = [System.IO.Path]::GetFullPath($Path)
    return $resolved.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
}

function Get-ProjectPathFromCommandLine {
    param([AllowNull()][string]$CommandLine)

    if ([string]::IsNullOrWhiteSpace($CommandLine)) {
        return $null
    }

    $match = [regex]::Match(
        $CommandLine,
        '(?i)(?:^|\s)-projectPath\s+(?:"([^"]+)"|(\S+))'
    )

    if (-not $match.Success) {
        return $null
    }

    $value = if ($match.Groups[1].Success) { $match.Groups[1].Value } else { $match.Groups[2].Value }
    try {
        return Normalize-ProjectPath -Path $value
    }
    catch {
        return $value
    }
}

function Has-CommandLineFlag {
    param(
        [AllowNull()][string]$CommandLine,
        [Parameter(Mandatory = $true)][string]$Flag
    )

    if ([string]::IsNullOrWhiteSpace($CommandLine)) {
        return $false
    }

    $escaped = [regex]::Escape($Flag)
    return [regex]::IsMatch($CommandLine, "(?i)(?:^|\s)$escaped(?:\s|$)")
}

$project = Normalize-ProjectPath -Path $ProjectPath
$versionFile = Join-Path $project 'ProjectSettings\ProjectVersion.txt'
$pinnedVersion = $null

if (Test-Path -LiteralPath $versionFile) {
    $versionLine = Select-String -LiteralPath $versionFile -Pattern '^m_EditorVersion:\s*(.+)$' | Select-Object -First 1
    if ($null -ne $versionLine) {
        $pinnedVersion = $versionLine.Matches[0].Groups[1].Value.Trim()
    }
}

$instances = @()
$unityProcesses = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'")

foreach ($process in $unityProcesses) {
    $commandLine = [string]$process.CommandLine
    $processProject = Get-ProjectPathFromCommandLine -CommandLine $commandLine

    $matchesProject = $false
    $matchMethod = $null

    if (-not [string]::IsNullOrWhiteSpace($processProject)) {
        $matchesProject = [string]::Equals($processProject, $project, [System.StringComparison]::OrdinalIgnoreCase)
        if ($matchesProject) {
            $matchMethod = 'projectPathArgument'
        }
    }
    elseif (-not [string]::IsNullOrWhiteSpace($commandLine)) {
        $matchesProject = $commandLine.IndexOf($project, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
        if ($matchesProject) {
            $matchMethod = 'commandLineSubstring'
        }
    }

    if (-not $matchesProject) {
        continue
    }

    $instances += [pscustomobject]@{
        processId      = [int]$process.ProcessId
        executablePath = [string]$process.ExecutablePath
        projectPath    = $processProject
        projectMatch   = $matchMethod
        automated      = Has-CommandLineFlag -CommandLine $commandLine -Flag '-automated'
        batchMode      = Has-CommandLineFlag -CommandLine $commandLine -Flag '-batchmode'
    }
}

$automatedCount = @($instances | Where-Object { $_.automated }).Count
$nonAutomatedCount = @($instances | Where-Object { -not $_.automated }).Count

$status = if ($instances.Count -eq 0) {
    'not_running'
}
elseif ($instances.Count -gt 1) {
    'multiple_instances'
}
elseif ($automatedCount -eq 1) {
    'automation_ready_process'
}
else {
    'running_without_automated'
}

$result = [pscustomobject]@{
    projectPath       = $project
    pinnedUnityVersion = $pinnedVersion
    status            = $status
    instanceCount     = $instances.Count
    automatedCount    = $automatedCount
    nonAutomatedCount = $nonAutomatedCount
    instances         = $instances
}

$result | ConvertTo-Json -Depth 6

if ($RequireAutomated) {
    if ($instances.Count -eq 0) {
        exit 3
    }
    if ($instances.Count -gt 1) {
        exit 4
    }
    if ($nonAutomatedCount -gt 0) {
        exit 5
    }
}

exit 0
