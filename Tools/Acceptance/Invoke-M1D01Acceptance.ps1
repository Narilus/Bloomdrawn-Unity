[CmdletBinding()]
param(
    [Parameter()]
    [string]$ProjectPath = (Get-Location).Path,

    [Parameter()]
    [int]$EditorTimeoutSeconds = 180,

    [Parameter()]
    [int]$TestTimeoutSeconds = 900,

    [Parameter()]
    [switch]$RunStatusPollingSelfTest,

    [Parameter()]
    [string]$StatusPollingSelfTestOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

class M1D01AcceptanceFailure : System.Exception {
    [string]$Classification
    [int]$ExitCode
    M1D01AcceptanceFailure([string]$classification, [string]$message, [int]$exitCode) : base($message) {
        $this.Classification = $classification
        $this.ExitCode = $exitCode
    }
}

$project = [System.IO.Path]::GetFullPath($ProjectPath).TrimEnd('\', '/')
$contractPath = Join-Path $project 'Tools\Acceptance\M1-D01-runner-contract.json'
$expectedPath = Join-Path $project 'Tools\Acceptance\M1-D01-expected-values.json'
$lockPath = Join-Path $project 'acceptance\locks\M1-D01-protected.sha256.json'
$acceptanceRoot = Join-Path $project 'Logs\M1-D01\Acceptance'
$runsRoot = Join-Path $acceptanceRoot 'runs'
$runId = [guid]::NewGuid().ToString('N')
$runDirectory = Join-Path $runsRoot $runId
$bridgeDirectory = Join-Path $runDirectory 'bridge'
$editorLog = Join-Path $runDirectory 'Editor.log'
$statusPath = Join-Path $bridgeDirectory 'status.json'
$resultJsonPath = Join-Path $bridgeDirectory 'results.json'
$resultXmlPath = Join-Path $bridgeDirectory 'results.xml'
$runtimeEvidence = Join-Path $bridgeDirectory 'runtime'
$globalRuntimeEvidence = Join-Path $acceptanceRoot 'runtime'
$commandLog = Join-Path $runDirectory 'commands.ndjson'
$lifecycleLog = Join-Path $runDirectory 'lifecycle-observations.ndjson'
$finalResultPath = Join-Path $runDirectory 'acceptance-result.json'

$script:contract = $null
$script:lock = $null
$script:initialHead = $null
$script:preSnapshot = $null
$script:ownedPid = $null
$script:ownedCommandLine = $null
$script:watchPosition = 0L
$script:watchCarry = ''
$script:watchTriggered = $false
$script:watchReason = $null
$script:watchOffset = $null
$script:shutdown = [ordered]@{ attempted = $false; gracefulRequested = $false; forced = $false; pidExited = $null; projectOwnerCount = $null; pipelineAbsent = $null; childPids = @(); childExit = @() }
$script:classification = 'INFRASTRUCTURE_FAILURE'
$script:reason = 'Runner did not reach classification.'
$script:exitCode = 30
$script:bridgeStatus = $null
$script:consoleStartupCursor = 0L
$script:consolePreTestCursor = $null
$script:terminalObservationCount = 0
$script:statusTransientFailureCount = 0
$script:statusTransientRecoveryCount = 0
$script:solutionPath = Join-Path $project 'Bloomdrawn-Unity.slnx'
$script:solutionPre = $null
$script:solutionBackupPath = $null
$script:solutionRestored = $false
$script:solutionObservations = [System.Collections.Generic.List[object]]::new()
$script:watchdogProcess = $null
$script:externalWatchdogTrigger = Join-Path $runDirectory 'watchdog-external-trigger.json'

function Throw-RunFailure {
    param([string]$Classification, [string]$Message, [int]$Code)
    throw [M1D01AcceptanceFailure]::new($Classification, $Message, $Code)
}

function Write-Json {
    param([object]$Value, [string]$Path, [int]$Depth = 16)
    $Value | ConvertTo-Json -Depth $Depth | Set-Content -LiteralPath $Path -Encoding utf8
}

function Write-AtomicJson {
    param([object]$Value, [string]$Path, [int]$Depth = 16)
    $temp = "$Path.tmp-$([guid]::NewGuid().ToString('N'))"
    $Value | ConvertTo-Json -Depth $Depth | Set-Content -LiteralPath $temp -Encoding utf8
    if (Test-Path -LiteralPath $Path) {
        $rollback = "$Path.rollback-$([guid]::NewGuid().ToString('N'))"
        [System.IO.File]::Replace($temp, $Path, $rollback)
        if (-not (Test-Path -LiteralPath $Path -PathType Leaf) -or -not (Test-Path -LiteralPath $rollback -PathType Leaf)) { throw "Atomic JSON replacement verification failed: $Path" }
        Remove-Item -LiteralPath $rollback -Force
    }
    else { [System.IO.File]::Move($temp, $Path) }
}

function Get-Sha256 {
    param([string]$Path)
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash
}

function Get-SolutionFacts {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Owner-managed solution is missing.' 30 }
    $item = Get-Item -LiteralPath $Path -Force
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Owner-managed solution is redirected.' 30 }
    $bytes = [IO.File]::ReadAllBytes($Path)
    $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $offset = if ($bom) { 3 } else { 0 }
    $text = [Text.Encoding]::UTF8.GetString($bytes, $offset, $bytes.Length - $offset)
    return [ordered]@{
        path = [IO.Path]::GetFullPath($Path); bytes = $bytes.Length; sha256 = Get-Sha256 $Path
        utf8Bom = $bom; crlfOnly = ($text -notmatch '(?<!\r)\n' -and $text -notmatch '\r(?!\n)')
        creationUtc = $item.CreationTimeUtc.ToString('o'); writeUtc = $item.LastWriteTimeUtc.ToString('o')
        gitStatus = @(& git -C $project status --porcelain=v1 --untracked-files=all -- Bloomdrawn-Unity.slnx)
        headDiff = @(& git -C $project diff --binary -- Bloomdrawn-Unity.slnx)
    }
}

function Assert-ValidSolutionCandidate {
    param([object]$Facts)
    if (-not [bool]$Facts.utf8Bom -or -not [bool]$Facts.crlfOnly) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Unity solution regeneration changed encoding/newline form.' 30 }
    try { [xml]$xml = [IO.File]::ReadAllText($script:solutionPath, [Text.Encoding]::UTF8) } catch { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Unity solution regeneration is not valid XML.' 30 }
    $paths = @($xml.Solution.Project | ForEach-Object { [string]$_.Path })
    if ($paths.Count -eq 0 -or @($paths | Where-Object { $_ -notmatch '^[^\\/:*?""<>|]+\.csproj$' }).Count -ne 0 -or @($paths | Sort-Object -Unique).Count -ne $paths.Count) {
        Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Unity solution regeneration contains invalid or duplicate project membership.' 30
    }
}

function Capture-SolutionPreRun {
    $script:solutionPre = Get-SolutionFacts $script:solutionPath
    if ($script:solutionPre.sha256 -ne [string]$script:contract.preexistingException.sha256 -or [long]$script:solutionPre.bytes -ne 485) {
        Throw-RunFailure 'ENVIRONMENTAL_BLOCKAGE' 'Owner-managed solution is not in the verified 485-byte owner state.' 20
    }
    Write-Json $script:solutionPre (Join-Path $runDirectory 'slnx-pre-run.json')
    $backupRoot = Join-Path ([IO.Path]::GetTempPath()) 'Bloomdrawn-M1D01-slnx-backups'
    if (-not (Test-Path -LiteralPath $backupRoot -PathType Container)) { [void][IO.Directory]::CreateDirectory($backupRoot) }
    $script:solutionBackupPath = [IO.Path]::GetFullPath((Join-Path $backupRoot ($runId + '.slnx.bin')))
    if ($script:solutionBackupPath.StartsWith($project, [StringComparison]::OrdinalIgnoreCase)) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Solution backup resolved inside the project.' 30 }
    [IO.File]::WriteAllBytes($script:solutionBackupPath, [IO.File]::ReadAllBytes($script:solutionPath))
    $backup = [ordered]@{ runId = $runId; path = $script:solutionBackupPath; bytes = (Get-Item -LiteralPath $script:solutionBackupPath).Length; sha256 = Get-Sha256 $script:solutionBackupPath; verifiedUtc = [DateTime]::UtcNow.ToString('o') }
    if ($backup.sha256 -ne $script:solutionPre.sha256 -or $backup.bytes -ne $script:solutionPre.bytes) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'External solution backup verification failed.' 30 }
    Write-Json $backup (Join-Path $runDirectory 'slnx-backup.json')
}

function Check-SolutionWatcher {
    if ($null -eq $script:solutionPre) { return }
    $facts = Get-SolutionFacts $script:solutionPath
    if ($facts.sha256 -ne $script:solutionPre.sha256) {
        if ($null -eq $script:ownedPid -or -not (Get-Process -Id $script:ownedPid -ErrorAction SilentlyContinue)) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Solution changed outside the exact owned Editor PID lifetime.' 30 }
        $owners = @(Get-ProjectUnityProcesses)
        if ($owners.Count -ne 1 -or [int]$owners[0].ProcessId -ne [int]$script:ownedPid) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Solution mutation occurred with competing or unproven Unity ownership.' 30 }
        Assert-ValidSolutionCandidate $facts
    }
    $last = if ($script:solutionObservations.Count -eq 0) { $null } else { $script:solutionObservations[$script:solutionObservations.Count - 1] }
    if ($null -eq $last -or $last.sha256 -ne $facts.sha256) {
        $script:solutionObservations.Add([pscustomobject][ordered]@{ utc = [DateTime]::UtcNow.ToString('o'); pid = $script:ownedPid; sha256 = $facts.sha256; bytes = $facts.bytes; writeUtc = $facts.writeUtc })
    }
}

function Restore-SolutionAfterShutdown {
    if ($script:solutionRestored -or $null -eq $script:solutionPre) { return }
    if ($null -ne $script:ownedPid -and (Get-Process -Id $script:ownedPid -ErrorAction SilentlyContinue)) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Refusing to restore solution while owned Editor remains alive.' 30 }
    if (@(Get-ProjectUnityProcesses).Count -ne 0) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Refusing to restore solution while a project Unity process exists.' 30 }
    if (-not (Test-Path -LiteralPath $script:solutionBackupPath -PathType Leaf) -or (Get-Sha256 $script:solutionBackupPath) -ne $script:solutionPre.sha256) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'External solution backup is missing or mismatched.' 30 }
    $observed = Get-SolutionFacts $script:solutionPath
    if ($observed.sha256 -ne $script:solutionPre.sha256) { Assert-ValidSolutionCandidate $observed }
    $operationId = [guid]::NewGuid().ToString('N')
    $parent = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($script:solutionPath))
    $temporary = [IO.Path]::GetFullPath((Join-Path $parent ('.M1D01-slnx-restore-' + $operationId + '.tmp')))
    $rollback = [IO.Path]::GetFullPath((Join-Path $parent ('.M1D01-slnx-rollback-' + $operationId + '.bak')))
    if ([string]::IsNullOrWhiteSpace($temporary) -or [string]::IsNullOrWhiteSpace($rollback) -or $parent -ne $project -or $temporary -eq $rollback -or $temporary -eq $script:solutionPath -or $rollback -eq $script:solutionPath -or (Test-Path -LiteralPath $rollback)) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Atomic restoration path preconditions failed.' 30 }
    $bytes = [IO.File]::ReadAllBytes($script:solutionBackupPath)
    $stream = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) } finally { $stream.Dispose() }
    if ((Get-Sha256 $temporary) -ne $script:solutionPre.sha256 -or (Get-Sha256 $script:solutionPath) -ne $observed.sha256) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Atomic restoration pre-replace hashes changed.' 30 }
    [IO.File]::Replace($temporary, $script:solutionPath, $rollback)
    $restored = Get-SolutionFacts $script:solutionPath
    $rollbackHash = Get-Sha256 $rollback
    $sameStatus = (($restored.gitStatus | ConvertTo-Json -Compress) -eq ($script:solutionPre.gitStatus | ConvertTo-Json -Compress))
    $sameDiff = (($restored.headDiff | ConvertTo-Json -Compress) -eq ($script:solutionPre.headDiff | ConvertTo-Json -Compress))
    if ($restored.sha256 -ne $script:solutionPre.sha256 -or $restored.bytes -ne $script:solutionPre.bytes -or $rollbackHash -ne $observed.sha256 -or -not $sameStatus -or -not $sameDiff) {
        Write-Json ([ordered]@{ operationId=$operationId; observed=$observed; restored=$restored; rollback=$rollback; rollbackSha256=$rollbackHash }) (Join-Path $runDirectory 'slnx-restoration-failed.json')
        Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Atomic solution restoration post-check failed.' 30
    }
    Remove-Item -LiteralPath $rollback -Force
    if ((Test-Path -LiteralPath $temporary) -or (Test-Path -LiteralPath $rollback)) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Atomic solution restoration left a residual artifact.' 30 }
    $script:solutionRestored = $true
    Write-Json ([ordered]@{ runId=$runId; operationId=$operationId; temporary=$temporary; rollback=$rollback; fileReplaceCalls=1; pre=$script:solutionPre; observedPost=$observed; restored=$restored; rollbackSha256=$rollbackHash; rollbackDeletedAfterVerification=$true; observations=$script:solutionObservations; completedUtc=[DateTime]::UtcNow.ToString('o') }) (Join-Path $runDirectory 'slnx-restoration.json')
}

function Record-Command {
    param([string]$Command, [int]$ExitCode, [object]$Output = $null)
    $record = [ordered]@{
        sequence = @(Get-Content -LiteralPath $commandLog -ErrorAction SilentlyContinue).Count + 1
        utc = [DateTime]::UtcNow.ToString('o')
        command = $Command
        exitCode = $ExitCode
        output = $Output
    }
    ($record | ConvertTo-Json -Depth 8 -Compress) | Add-Content -LiteralPath $commandLog -Encoding utf8
}

function Get-WorkingTreeSnapshot {
    $entries = @()
    foreach ($line in @(& git -C $project -c core.quotepath=false status --porcelain=v1 --untracked-files=all)) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.Length -lt 4) { continue }
        $path = $line.Substring(3).Replace('\', '/')
        if ($path.Contains(' -> ')) { $path = $path.Split(' -> ')[-1] }
        $full = Join-Path $project $path.Replace('/', '\')
        $exists = Test-Path -LiteralPath $full -PathType Leaf
        $entries += [pscustomobject][ordered]@{
            path = $path
            status = $line.Substring(0, 2)
            exists = $exists
            sha256 = if ($exists) { (Get-FileHash -Algorithm SHA256 -LiteralPath $full).Hash } else { $null }
        }
    }
    return @($entries | Sort-Object path)
}

function Get-ProtectedHashes {
    param([switch]$FailOnMismatch)
    $values = @()
    foreach ($entry in @($script:lock.files)) {
        $relative = [string]$entry.path
        $full = Join-Path $project $relative.Replace('/', '\')
        $actual = if (Test-Path -LiteralPath $full -PathType Leaf) { (Get-FileHash -Algorithm SHA256 -LiteralPath $full).Hash } else { $null }
        $expected = ([string]$entry.sha256).ToUpperInvariant()
        $matches = $null -ne $actual -and $actual -eq $expected
        $values += [pscustomobject][ordered]@{ path = $relative; expectedSha256 = $expected; actualSha256 = $actual; matches = $matches }
        if ($FailOnMismatch -and -not $matches) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' "Protected hash mismatch or missing file: $relative" 30 }
    }
    return $values
}

function Assert-PreservedHashes {
    foreach ($entry in @($script:contract.preservedDirtyPaths)) {
        $full = Join-Path $project ([string]$entry.path).Replace('/', '\')
        if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { Throw-RunFailure 'ENVIRONMENTAL_BLOCKAGE' "Preserved dirty file is missing: $($entry.path)" 20 }
        $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $full).Hash
        if ($actual -ne ([string]$entry.sha256).ToUpperInvariant()) { Throw-RunFailure 'ENVIRONMENTAL_BLOCKAGE' "Preserved dirty hash mismatch: $($entry.path)" 20 }
    }
}

function Assert-AllowedDirtySnapshot {
    param([object[]]$Snapshot)
    $allowed = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($path in @($script:contract.allowedDirtyPaths)) { [void]$allowed.Add([string]$path) }
    foreach ($entry in @($Snapshot)) {
        if (-not $allowed.Contains([string]$entry.path)) { Throw-RunFailure 'ENVIRONMENTAL_BLOCKAGE' "Unexpected dirty path: $($entry.path)" 20 }
    }
}

function Compare-Snapshots {
    param([object[]]$Before, [object[]]$After)
    $beforeJson = @($Before | ConvertTo-Json -Depth 6 -Compress)
    $afterJson = @($After | ConvertTo-Json -Depth 6 -Compress)
    if (($beforeJson -join '') -ne ($afterJson -join '')) { return 'Complete working-tree status/hash snapshot changed during the run.' }
    return $null
}

function Get-ProjectUnityProcesses {
    $matches = @()
    foreach ($process in @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'")) {
        $commandLine = [string]$process.CommandLine
        if (-not [string]::IsNullOrWhiteSpace($commandLine) -and $commandLine.IndexOf($project, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $matches += $process
        }
    }
    return @($matches)
}

function Assert-NoProjectEditor {
    $owners = @(Get-ProjectUnityProcesses)
    if ($owners.Count -ne 0) { Throw-RunFailure 'ENVIRONMENTAL_BLOCKAGE' "A Unity process already owns the project: $($owners.ProcessId -join ',')." 20 }
}

function Get-DescendantPids {
    param([int]$ParentPid)
    $all = @(Get-CimInstance Win32_Process)
    $result = [System.Collections.Generic.List[int]]::new()
    $queue = [System.Collections.Generic.Queue[int]]::new()
    $queue.Enqueue($ParentPid)
    while ($queue.Count -gt 0) {
        $parent = $queue.Dequeue()
        foreach ($child in @($all | Where-Object { [int]$_.ParentProcessId -eq $parent })) {
            if (-not $result.Contains([int]$child.ProcessId)) { $result.Add([int]$child.ProcessId); $queue.Enqueue([int]$child.ProcessId) }
        }
    }
    return @($result)
}

function Get-BoundedLogBytes {
    param([long]$Offset, [int]$Count)
    if (-not (Test-Path -LiteralPath $editorLog)) { return [byte[]]@() }
    $stream = [System.IO.File]::Open($editorLog, 'Open', 'Read', 'ReadWrite')
    try {
        [void]$stream.Seek([Math]::Max(0L, $Offset), [System.IO.SeekOrigin]::Begin)
        $buffer = New-Object byte[] $Count
        $read = $stream.Read($buffer, 0, $Count)
        if ($read -eq $Count) { return $buffer }
        $actual = New-Object byte[] $read
        [Array]::Copy($buffer, $actual, $read)
        return $actual
    }
    finally { $stream.Dispose() }
}

function Write-WatchdogEvidence {
    param([string]$Reason, [long]$Offset, [long]$Size)
    $contextLimit = [int]$script:contract.limitsBytes.watchdogContext
    $start = [Math]::Max(0L, $Offset - [long]($contextLimit / 2))
    [System.IO.File]::WriteAllBytes((Join-Path $runDirectory 'watchdog-first-context.bin'), (Get-BoundedLogBytes $start $contextLimit))
    $tailStart = [Math]::Max(0L, $Size - $contextLimit)
    [System.IO.File]::WriteAllBytes((Join-Path $runDirectory 'watchdog-final-tail.bin'), (Get-BoundedLogBytes $tailStart $contextLimit))
    Write-AtomicJson ([ordered]@{ runId = $runId; utc = [DateTime]::UtcNow.ToString('o'); pid = $script:ownedPid; byteOffset = $Offset; currentSize = $Size; reason = $Reason }) (Join-Path $runDirectory 'watchdog-trigger.json')
}

function Start-ExternalWatchdog {
    if ($null -eq $script:ownedPid) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Cannot start watchdog before PID ownership.' 30 }
    $quotedLog = $editorLog.Replace("'", "''")
    $quotedTrigger = $script:externalWatchdogTrigger.Replace("'", "''")
    $audio = ([string]$script:contract.watchdogSignatures[0]).Replace("'", "''")
    $audio2 = ([string]$script:contract.watchdogSignatures[1]).Replace("'", "''")
    $cap = [long]$script:contract.limitsBytes.editorLog
    $pidValue = [int]$script:ownedPid
    $body = @"
`$ErrorActionPreference='SilentlyContinue';`$log='$quotedLog';`$trigger='$quotedTrigger';`$pidValue=$pidValue;`$cap=$cap;`$position=0L;`$carry='';while(Get-Process -Id `$pidValue -ErrorAction SilentlyContinue){if(Test-Path -LiteralPath `$log){`$size=(Get-Item -LiteralPath `$log).Length;`$reason=`$null;`$offset=`$null;if(`$size-ge`$cap){`$reason='Editor log reached the 64 MiB cap.';`$offset=`$size}elseif(`$size-gt`$position){`$stream=[IO.File]::Open(`$log,'Open','Read','ReadWrite');try{[void]`$stream.Seek(`$position,[IO.SeekOrigin]::Begin);`$count=[int][Math]::Min(1048576L,`$size-`$position);`$buffer=New-Object byte[] `$count;`$read=`$stream.Read(`$buffer,0,`$count)}finally{`$stream.Dispose()};if(`$read-gt0){`$text=`$carry+[Text.Encoding]::UTF8.GetString(`$buffer,0,`$read);foreach(`$signature in @('$audio','$audio2')){`$index=`$text.IndexOf(`$signature,[StringComparison]::Ordinal);if(`$index-ge0){`$reason='Dedicated Editor log signature detected: '+`$signature;`$offset=[Math]::Max(0L,`$position-[Text.Encoding]::UTF8.GetByteCount(`$carry)+`$index);break}};`$position+=`$read;`$carry=if(`$text.Length-gt256){`$text.Substring(`$text.Length-256)}else{`$text}}};if(`$reason){`$record=[ordered]@{utc=[DateTime]::UtcNow.ToString('o');pid=`$pidValue;reason=`$reason;byteOffset=`$offset;currentSize=`$size};[IO.File]::WriteAllText(`$trigger,(`$record|ConvertTo-Json),[Text.UTF8Encoding]::new(`$false));`$p=Get-Process -Id `$pidValue -ErrorAction SilentlyContinue;if(`$p){[void]`$p.CloseMainWindow();Start-Sleep -Seconds 5;if(Get-Process -Id `$pidValue -ErrorAction SilentlyContinue){Stop-Process -Id `$pidValue -Force}};exit 30}};Start-Sleep -Milliseconds 100};exit 0
"@
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($body))
    $script:watchdogProcess = Start-Process -FilePath (Get-Command pwsh).Source -ArgumentList @('-NoLogo','-NoProfile','-NonInteractive','-EncodedCommand',$encoded) -PassThru -WindowStyle Hidden
    if ($null -eq $script:watchdogProcess -or $script:watchdogProcess.HasExited) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Out-of-process watchdog failed to start.' 30 }
}

function Stop-ExternalWatchdog {
    if ($null -eq $script:watchdogProcess) { return }
    try {
        if (-not $script:watchdogProcess.HasExited) {
            if (-not $script:watchdogProcess.WaitForExit(5000)) { Stop-Process -Id $script:watchdogProcess.Id -Force }
        }
        Write-Json ([ordered]@{ pid=$script:watchdogProcess.Id; exited=$script:watchdogProcess.HasExited; exitCode=if($script:watchdogProcess.HasExited){$script:watchdogProcess.ExitCode}else{$null}; triggerPath=$script:externalWatchdogTrigger; triggered=(Test-Path -LiteralPath $script:externalWatchdogTrigger) }) (Join-Path $runDirectory 'watchdog-summary.json')
    } catch { }
}

function Check-Watchdog {
    Check-SolutionWatcher
    if (Test-Path -LiteralPath $script:externalWatchdogTrigger) {
        try { $external = Get-Content -Raw -LiteralPath $script:externalWatchdogTrigger | ConvertFrom-Json; $script:watchReason = [string]$external.reason; $script:watchOffset = [long]$external.byteOffset } catch { $script:watchReason = 'External watchdog trigger was unparseable.'; $script:watchOffset = 0L }
        $script:watchTriggered = $true
    }
    if ($null -eq $script:ownedPid -or -not (Test-Path -LiteralPath $editorLog)) { return }
    $size = (Get-Item -LiteralPath $editorLog).Length
    if ($size -ge [long]$script:contract.limitsBytes.editorLog) {
        $script:watchTriggered = $true; $script:watchReason = 'Editor log reached the 64 MiB cap.'; $script:watchOffset = $size
    }
    elseif ($size -gt $script:watchPosition) {
        $count = [int][Math]::Min(1048576L, $size - $script:watchPosition)
        while ($script:watchPosition -lt $size -and -not $script:watchTriggered) {
            $count = [int][Math]::Min(1048576L, $size - $script:watchPosition)
            $bytes = Get-BoundedLogBytes $script:watchPosition $count
            if ($bytes.Length -eq 0) { break }
            $text = $script:watchCarry + [Text.Encoding]::UTF8.GetString($bytes)
            foreach ($signature in @($script:contract.watchdogSignatures)) {
                $index = $text.IndexOf([string]$signature, [StringComparison]::Ordinal)
                if ($index -ge 0) {
                    $script:watchTriggered = $true
                    $script:watchReason = "Dedicated Editor log signature detected: $signature"
                    $script:watchOffset = [Math]::Max(0L, $script:watchPosition - [Text.Encoding]::UTF8.GetByteCount($script:watchCarry) + $index)
                    break
                }
            }
            $script:watchPosition += $bytes.Length
            $script:watchCarry = if ($text.Length -gt 256) { $text.Substring($text.Length - 256) } else { $text }
        }
    }
    if ($script:watchTriggered) {
        Write-WatchdogEvidence $script:watchReason ([long]$script:watchOffset) $size
        Invoke-BridgeAbort -Quiet
        Stop-OwnedEditor -AbortContainment
        Throw-RunFailure 'INFRASTRUCTURE_FAILURE' $script:watchReason 30
    }
}

function Invoke-UnityCommand {
    param([string]$Name, [string[]]$Arguments = @(), [switch]$Quiet)
    Check-Watchdog
    $display = "unity --json command --project-path <project> $Name" + $(if ($Arguments.Count) { ' ' + ($Arguments -join ' ') } else { '' })
    $output = & unity --json command --project-path $project $Name @Arguments 2>&1
    $exit = $LASTEXITCODE
    Record-Command $display $exit $(if ($Quiet) { $null } else { @($output) })
    Check-Watchdog
    return [pscustomobject]@{ ExitCode = $exit; Text = ($output -join [Environment]::NewLine); Lines = @($output) }
}

function Invoke-BridgeAbort {
    param([switch]$Quiet)
    if ($null -eq $script:ownedPid -or -not (Get-Process -Id $script:ownedPid -ErrorAction SilentlyContinue)) { return }
    try { [void](Invoke-UnityCommand ([string]$script:contract.bridge.abortCommand) @() -Quiet:$Quiet) } catch { }
}

function Stop-OwnedEditor {
    param([switch]$AbortContainment)
    if ($null -eq $script:ownedPid) { return }
    $script:shutdown.attempted = $true
    $script:shutdown.childPids = @(Get-DescendantPids -ParentPid $script:ownedPid)
    $process = Get-Process -Id $script:ownedPid -ErrorAction SilentlyContinue
    $shutdownDeadline = $null
    if ($null -ne $process) {
        $script:shutdown.gracefulRequested = $true
        [void]$process.CloseMainWindow()
        $seconds = if ($AbortContainment) { [int]$script:contract.timeoutsSeconds.abortShutdownBeforeForce } else { [int]$script:contract.timeoutsSeconds.normalShutdown }
        $deadline = (Get-Date).AddSeconds($seconds)
        $shutdownDeadline = $deadline
        while ((Get-Date) -lt $deadline -and (Get-Process -Id $script:ownedPid -ErrorAction SilentlyContinue)) { Start-Sleep -Milliseconds 250 }
        if (Get-Process -Id $script:ownedPid -ErrorAction SilentlyContinue) {
            Stop-Process -Id $script:ownedPid -Force
            $script:shutdown.forced = $true
            $deadline = (Get-Date).AddSeconds(5)
            while ((Get-Date) -lt $deadline -and (Get-Process -Id $script:ownedPid -ErrorAction SilentlyContinue)) { Start-Sleep -Milliseconds 100 }
        }
    }
    if ($null -ne $shutdownDeadline) {
        while ((Get-Date) -lt $shutdownDeadline) {
            $liveChildren = @($script:shutdown.childPids | Where-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue })
            if ($liveChildren.Count -eq 0 -and @(Get-ProjectUnityProcesses).Count -eq 0) { break }
            Start-Sleep -Milliseconds 250
        }
    }
    $script:shutdown.pidExited = $null -eq (Get-Process -Id $script:ownedPid -ErrorAction SilentlyContinue)
    $script:shutdown.projectOwnerCount = @(Get-ProjectUnityProcesses).Count
    $script:shutdown.pipelineAbsent = $script:shutdown.projectOwnerCount -eq 0
    $childResults = @()
    foreach ($childPid in @($script:shutdown.childPids)) { $childResults += [ordered]@{ pid = $childPid; exited = $null -eq (Get-Process -Id $childPid -ErrorAction SilentlyContinue) } }
    $script:shutdown.childExit = $childResults
    Write-Json $script:shutdown (Join-Path $runDirectory 'shutdown-proof.json')
    Stop-ExternalWatchdog
}

function Wait-ForPipelineReady {
    $deadline = (Get-Date).AddSeconds($EditorTimeoutSeconds)
    do {
        Check-Watchdog
        if (-not (Get-Process -Id $script:ownedPid -ErrorAction SilentlyContinue)) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Owned Unity exited before Pipeline readiness.' 30 }
        $raw = & unity --json status --project-path $project 2>$null
        $exit = $LASTEXITCODE
        if ($exit -eq 0) {
            try {
                $value = $raw | ConvertFrom-Json
                if (@($value.data.instances).Count -eq 1 -and $value.data.instances[0].state -eq 'ready') {
                    Record-Command 'unity --json status --project-path <project> (bounded readiness)' 0 $value
                    return $value
                }
            } catch { }
        }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)
    Throw-RunFailure 'ENVIRONMENTAL_BLOCKAGE' 'Owned automated Editor/Pipeline did not become ready within the startup timeout.' 20
}

function Wait-ForCompilation {
    $start = Invoke-UnityCommand 'recompile'
    if ($start.ExitCode -ne 0) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Could not request bridge import/compilation.' 30 }
    $deadline = (Get-Date).AddSeconds([int]$script:contract.timeoutsSeconds.importCompile)
    do {
        Start-Sleep -Milliseconds 500
        $poll = Invoke-UnityCommand 'recompile_status' @() -Quiet
        if ($poll.ExitCode -eq 0) {
            try {
                $envelope = $poll.Text | ConvertFrom-Json
                $report = ([string]$envelope.data.result) | ConvertFrom-Json
                if ($report.status -in @('completed', 'up_to_date')) {
                    Write-Json $report (Join-Path $runDirectory 'recompile-status.json')
                    if ([bool]$report.failed) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Bridge compilation completed with errors.' 30 }
                    return
                }
            } catch { if ($_.Exception -is [M1D01AcceptanceFailure]) { throw } }
        }
    } while ((Get-Date) -lt $deadline)
    Throw-RunFailure 'ENVIRONMENTAL_BLOCKAGE' 'Bridge compilation did not complete within the import/compile timeout.' 20
}

function Get-ConsoleEvidence {
    param([long]$Since, [string]$OutputName)
    $response = Invoke-UnityCommand 'console' @('--level', 'error', '--tail', '5000', '--since', [string]$Since) -Quiet
    if ($response.ExitCode -ne 0) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Console query failed.' 30 }
    try { $envelope = $response.Text | ConvertFrom-Json } catch { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Console response was unavailable or unparseable.' 30 }
    Write-Json $envelope (Join-Path $runDirectory $OutputName)
    if ($null -eq $envelope.data.result.cursor) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Console cursor was unavailable.' 30 }
    if (@($envelope.data.result.entries).Count -gt 0) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Unexpected Unity Console error/assert/exception entry detected.' 30 }
    return [long]$envelope.data.result.cursor
}

function Observe-Lifecycle {
    param([object]$Status)
    $terminal = [string]$Status.lifecycle -in @($script:contract.bridge.terminalLifecycle)
    if ($terminal) { $script:terminalObservationCount++ }
    ([ordered]@{ utc = [DateTime]::UtcNow.ToString('o'); lifecycle = [string]$Status.lifecycle; heartbeatUtc = [string]$Status.heartbeatUtc; runFinishedCallbacks = [int]$Status.runFinishedCallbackCount; duplicates = [int]$Status.duplicateRunFinishedCount; divergent = [int]$Status.divergentDuplicateCount } | ConvertTo-Json -Compress) | Add-Content -LiteralPath $lifecycleLog -Encoding utf8
}

function Write-StatusPollingEvent {
    param([string]$Event, [string]$Detail, [int]$Attempt, [double]$ElapsedMilliseconds)
    if ($Event -eq 'transient_status_read_failure') { $script:statusTransientFailureCount++ }
    elseif ($Event -eq 'transient_status_read_recovered') { $script:statusTransientRecoveryCount++ }
    if (-not [string]::IsNullOrWhiteSpace($lifecycleLog) -and (Test-Path -LiteralPath ([IO.Path]::GetDirectoryName($lifecycleLog)) -PathType Container)) {
        ([ordered]@{ utc = [DateTime]::UtcNow.ToString('o'); event = $Event; detail = $Detail; attempt = $Attempt; elapsedMilliseconds = [Math]::Round($ElapsedMilliseconds, 3) } | ConvertTo-Json -Compress) | Add-Content -LiteralPath $lifecycleLog -Encoding utf8
    }
}

function Get-TerminalStatusSignature {
    param([object]$Status)
    return ([ordered]@{
        schemaVersion = [int]$Status.schemaVersion; taskId = [string]$Status.taskId; runId = [string]$Status.runId
        testedHead = [string]$Status.testedHead; fixture = [string]$Status.fixture; filter = [string]$Status.filter; mode = [string]$Status.mode
        lifecycle = [string]$Status.lifecycle; pendingTerminalLifecycle = [string]$Status.pendingTerminalLifecycle
        resultFingerprint = [string]$Status.resultFingerprint; total = [int]$Status.total; passed = [int]$Status.passed
        failed = [int]$Status.failed; skipped = [int]$Status.skipped; inconclusive = [int]$Status.inconclusive
        runFinishedCallbackCount = [int]$Status.runFinishedCallbackCount; duplicateRunFinishedCount = [int]$Status.duplicateRunFinishedCount
        divergentDuplicateCount = [int]$Status.divergentDuplicateCount; xmlSha256 = [string]$Status.xmlSha256
        jsonSha256 = [string]$Status.jsonSha256; failureReason = [string]$Status.failureReason
    } | ConvertTo-Json -Compress)
}

function Assert-CurrentBridgeStatus {
    param([object]$Status, [object]$LastValidStatus)
    if ($null -eq $Status) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Bridge status JSON resolved to null.' 30 }
    if ([int]$Status.schemaVersion -ne 2 -or [string]$Status.taskId -ne 'M1-D01' -or
        [string]$Status.runId -ne $runId -or [string]$Status.testedHead -ne $script:initialHead -or
        [string]$Status.fixture -ne [string]$script:contract.testFixture -or [string]$Status.filter -ne [string]$script:contract.testFixture -or
        [string]$Status.mode -ne [string]$script:contract.testMode) {
        Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Bridge status schema/task/run/HEAD/fixture/filter/mode identity is invalid.' 30
    }
    $lifecycle = [string]$Status.lifecycle
    if ($lifecycle -notin @($script:contract.bridge.lifecycle)) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' "Bridge status lifecycle is invalid: $lifecycle" 30 }
    $expectedMethods = @($script:contract.expectedMethods | ForEach-Object { [string]$_ } | Sort-Object)
    $statusMethods = @($Status.expectedMethods | ForEach-Object { [string]$_ } | Sort-Object)
    if (($expectedMethods -join "`n") -ne ($statusMethods -join "`n")) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Bridge status expected-method identity is invalid.' 30 }

    $rank = @{ prepared = 0; running = 1; completing = 2; completed = 3; behavioral_failure = 3; infrastructure_failure = 3; aborted = 3 }
    if ($null -ne $LastValidStatus) {
        $lastLifecycle = [string]$LastValidStatus.lifecycle
        if ([int]$rank[$lifecycle] -lt [int]$rank[$lastLifecycle]) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' "Bridge lifecycle regressed from $lastLifecycle to $lifecycle." 30 }
        if ($lastLifecycle -in @($script:contract.bridge.terminalLifecycle)) {
            if ($lifecycle -notin @($script:contract.bridge.terminalLifecycle) -or (Get-TerminalStatusSignature $Status) -ne (Get-TerminalStatusSignature $LastValidStatus)) {
                Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Divergent terminal bridge status payload was observed.' 30
            }
        }
    }
    if ($lifecycle -in @('prepared', 'running', 'completing')) {
        $heartbeat = [DateTimeOffset]::MinValue
        if (-not [DateTimeOffset]::TryParse([string]$Status.heartbeatUtc, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind, [ref]$heartbeat)) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Active bridge status heartbeat is invalid.' 30 }
        if (([DateTime]::UtcNow - $heartbeat.UtcDateTime).TotalSeconds -gt [double]$script:contract.statusPolling.heartbeatTimeoutSeconds) {
            Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Active bridge status heartbeat exceeded its bounded timeout.' 30
        }
    }
}

function Read-BridgeStatusWithGrace {
    param(
        [string]$Path,
        [DateTime]$OverallDeadline,
        [object]$LastValidStatus,
        [scriptblock]$AttemptReader = $null,
        [int]$GraceMilliseconds = 0,
        [int]$RetryIntervalMilliseconds = 0
    )
    if ($GraceMilliseconds -le 0) { $GraceMilliseconds = [int]$script:contract.statusPolling.transientReadGraceMilliseconds }
    if ($RetryIntervalMilliseconds -le 0) { $RetryIntervalMilliseconds = [int]$script:contract.statusPolling.transientReadRetryIntervalMilliseconds }
    $started = [DateTime]::UtcNow
    $attempt = 0
    $lastFailure = $null
    while ($true) {
        if ([DateTime]::UtcNow -ge $OverallDeadline) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Bridge status did not become durably readable before the authoritative lifecycle deadline.' 30 }
        $attempt++
        try {
            $raw = if ($null -ne $AttemptReader) { & $AttemptReader $Path } else { [IO.File]::ReadAllText($Path, [Text.Encoding]::UTF8) }
            if ([string]::IsNullOrWhiteSpace([string]$raw)) { throw [IO.InvalidDataException]::new('Bridge status is empty or incomplete.') }
            $status = [string]$raw | ConvertFrom-Json -DateKind String
        }
        catch {
            if ($_.Exception -is [M1D01AcceptanceFailure]) { throw }
            $lastFailure = $_.Exception.GetType().FullName + ': ' + $_.Exception.Message
            $elapsed = ([DateTime]::UtcNow - $started).TotalMilliseconds
            Write-StatusPollingEvent 'transient_status_read_failure' $lastFailure $attempt $elapsed
            if ($elapsed -ge $GraceMilliseconds) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' "Bridge status remained missing, empty, locked, or malformed beyond the bounded read grace: $lastFailure" 30 }
            $remainingGrace = [Math]::Max(1, $GraceMilliseconds - [int]$elapsed)
            $remainingOverall = [Math]::Max(1, [int]($OverallDeadline - [DateTime]::UtcNow).TotalMilliseconds)
            Start-Sleep -Milliseconds ([Math]::Min($RetryIntervalMilliseconds, [Math]::Min($remainingGrace, $remainingOverall)))
            continue
        }
        Assert-CurrentBridgeStatus $status $LastValidStatus
        if ($null -ne $lastFailure) { Write-StatusPollingEvent 'transient_status_read_recovered' $lastFailure $attempt (([DateTime]::UtcNow - $started).TotalMilliseconds) }
        return $status
    }
}

function Invoke-StatusPollingSelfTests {
    param([string]$OutputPath)
    if ([string]::IsNullOrWhiteSpace($OutputPath)) { throw 'StatusPollingSelfTestOutput is required.' }
    $output = [IO.Path]::GetFullPath($OutputPath)
    if ($output.StartsWith($project + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Status polling self-test diagnostics must be outside the repository.' }
    $parent = [IO.Path]::GetDirectoryName($output)
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) { throw "Status polling self-test output parent does not exist: $parent" }
    $script:contract = Get-Content -Raw -LiteralPath $contractPath | ConvertFrom-Json
    $script:initialHead = '1111111111111111111111111111111111111111'
    $script:statusTransientFailureCount = 0
    $script:statusTransientRecoveryCount = 0
    $script:lifecycleLog = [IO.Path]::ChangeExtension($output, '.ndjson')
    [IO.File]::WriteAllText($script:lifecycleLog, '', [Text.UTF8Encoding]::new($false))
    Set-Variable -Name runId -Scope Script -Value 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
    function New-SelfTestStatus([string]$Lifecycle = 'running', [string]$Run = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa', [string]$Fingerprint = '') {
        return [ordered]@{ schemaVersion=2; taskId='M1-D01'; runId=$Run; testedHead=$script:initialHead; fixture=[string]$script:contract.testFixture; filter=[string]$script:contract.testFixture; mode=[string]$script:contract.testMode; lifecycle=$Lifecycle; pendingTerminalLifecycle=$(if($Lifecycle -eq 'completed'){'completed'}else{''}); heartbeatUtc=[DateTime]::UtcNow.ToString('o'); expectedMethods=@($script:contract.expectedMethods); resultFingerprint=$Fingerprint; total=$(if($Lifecycle -eq 'completed'){13}else{0}); passed=$(if($Lifecycle -eq 'completed'){13}else{0}); failed=0; skipped=0; inconclusive=0; runFinishedCallbackCount=$(if($Lifecycle -eq 'completed'){1}else{0}); duplicateRunFinishedCount=0; divergentDuplicateCount=0; xmlSha256=$(if($Lifecycle -eq 'completed'){'A'}else{''}); jsonSha256=$(if($Lifecycle -eq 'completed'){'B'}else{''}); failureReason='' }
    }
    function New-QueueReader([object[]]$Values, [object]$Fallback = $null) {
        $queue = [Collections.Generic.Queue[object]]::new()
        foreach ($value in $Values) { $queue.Enqueue($value) }
        return { param($ignored) $value = if($queue.Count -gt 0){$queue.Dequeue()}else{$Fallback}; if($value -is [Exception]){throw $value}; return [string]$value }.GetNewClosure()
    }
    $valid = (New-SelfTestStatus | ConvertTo-Json -Depth 8 -Compress)
    $deadline = { [DateTime]::UtcNow.AddSeconds(2) }
    $results = [Collections.Generic.List[object]]::new()
    foreach ($case in @(
        @{name='missing_then_valid'; values=@([IO.FileNotFoundException]::new('missing'),$valid); recover=$true},
        @{name='zero_length_then_valid'; values=@('',$valid); recover=$true},
        @{name='malformed_then_valid'; values=@('{',$valid); recover=$true},
        @{name='sharing_violation_then_valid'; values=@([IO.IOException]::new('simulated sharing violation'),$valid); recover=$true},
        @{name='persistent_missing'; values=@([IO.FileNotFoundException]::new('missing')); fallback=[IO.FileNotFoundException]::new('missing'); recover=$false},
        @{name='persistent_malformed'; values=@('{'); fallback='{'; recover=$false}
    )) {
        $passed = $false; $message = $null
        $fallback = if ($case.ContainsKey('fallback')) { $case.fallback } else { $null }
        try { [void](Read-BridgeStatusWithGrace '<synthetic>' (&$deadline) $null (New-QueueReader $case.values $fallback) 75 5); $passed = [bool]$case.recover; if(-not $case.recover){$message='unexpected recovery'} }
        catch [M1D01AcceptanceFailure] { $passed = -not [bool]$case.recover; $message = $_.Exception.Message }
        $results.Add([pscustomobject]@{ name=$case.name; passed=$passed; diagnostic=$message })
    }
    foreach ($case in @(
        @{name='wrong_run_id'; status=(New-SelfTestStatus -Run 'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb')},
        @{name='divergent_terminal'; status=(New-SelfTestStatus -Lifecycle 'completed' -Fingerprint 'second'); last=(New-SelfTestStatus -Lifecycle 'completed' -Fingerprint 'first')}
    )) {
        $passed = $false; $message = $null
        $last = if ($case.ContainsKey('last')) { $case.last } else { $null }
        try { $reader = New-QueueReader @(($case.status | ConvertTo-Json -Depth 8 -Compress)); [void](Read-BridgeStatusWithGrace '<synthetic>' (&$deadline) $last $reader 75 5); $message='unexpected acceptance' }
        catch [M1D01AcceptanceFailure] { $passed = $true; $message = $_.Exception.Message }
        $results.Add([pscustomobject]@{ name=$case.name; passed=$passed; diagnostic=$message })
    }
    $document = [ordered]@{ generatedUtc=[DateTime]::UtcNow.ToString('o'); allPassed=@($results | Where-Object {-not $_.passed}).Count -eq 0; transientFailures=$script:statusTransientFailureCount; transientRecoveries=$script:statusTransientRecoveryCount; eventLog=$script:lifecycleLog; cases=$results }
    [IO.File]::WriteAllText($output, ($document | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
    if (-not $document.allPassed) { throw 'One or more status polling self-tests failed.' }
    return $document
}

function Wait-ForBridgeTerminal {
    $ackDeadline = (Get-Date).AddSeconds([int]$script:contract.timeoutsSeconds.startAcknowledgement)
    $runDeadline = (Get-Date).AddSeconds($TestTimeoutSeconds)
    $sawRunning = $false
    $lastLifecycle = $null
    $lastValidStatus = $null
    do {
        Check-Watchdog
        if (-not (Get-Process -Id $script:ownedPid -ErrorAction SilentlyContinue)) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Owned Unity exited while protected tests were active.' 30 }
        $readDeadline = if (-not $sawRunning -and $ackDeadline -lt $runDeadline) { $ackDeadline } else { $runDeadline }
        $status = Read-BridgeStatusWithGrace $statusPath $readDeadline $lastValidStatus
        if ([string]$status.lifecycle -ne $lastLifecycle) { Observe-Lifecycle $status; $lastLifecycle = [string]$status.lifecycle }
        if ([string]$status.lifecycle -in @('running', 'completing')) { $sawRunning = $true }
        $lastValidStatus = $status
        if ([string]$status.lifecycle -in @($script:contract.bridge.terminalLifecycle)) {
            $script:bridgeStatus = $status
            Write-Json ([ordered]@{ runId=$runId; transientFailureCount=$script:statusTransientFailureCount; transientRecoveryCount=$script:statusTransientRecoveryCount; graceMilliseconds=[int]$script:contract.statusPolling.transientReadGraceMilliseconds; retryIntervalMilliseconds=[int]$script:contract.statusPolling.transientReadRetryIntervalMilliseconds; finalLifecycle=[string]$status.lifecycle; completedUtc=[DateTime]::UtcNow.ToString('o') }) (Join-Path $runDirectory 'status-polling-summary.json')
            return
        }
        if (-not $sawRunning -and (Get-Date) -ge $ackDeadline) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Bridge did not durably acknowledge running within 30 seconds.' 30 }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $runDeadline)
    Invoke-BridgeAbort -Quiet
    Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Protected bridge run exceeded its bounded timeout.' 30
}

function Assert-BridgeResults {
    $status = $script:bridgeStatus
    if ([string]$status.lifecycle -eq 'infrastructure_failure' -or [string]$status.lifecycle -eq 'aborted') { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' "Bridge terminal state $($status.lifecycle): $($status.failureReason)" 30 }
    if (-not (Test-Path -LiteralPath $resultJsonPath) -or -not (Test-Path -LiteralPath $resultXmlPath)) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Fresh JSON or XML result artifact is missing.' 30 }
    foreach ($pair in @(@($resultJsonPath, [long]$script:contract.limitsBytes.json), @($resultXmlPath, [long]$script:contract.limitsBytes.xml))) {
        $size = (Get-Item -LiteralPath $pair[0]).Length
        if ($size -le 0 -or $size -gt $pair[1]) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' "Result artifact is empty or oversized: $($pair[0])" 30 }
    }
    try { $json = Get-Content -Raw -LiteralPath $resultJsonPath | ConvertFrom-Json } catch { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Fresh bridge result JSON is invalid.' 30 }
    try { [xml]$xml = Get-Content -Raw -LiteralPath $resultXmlPath } catch { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Fresh NUnit XML is invalid.' 30 }
    if ([string]$json.runId -ne $runId -or [string]$json.testedHead -ne $script:initialHead -or [string]$json.fixture -ne [string]$script:contract.testFixture) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Result identity does not match this run.' 30 }
    foreach ($name in @('total','passed','failed','skipped','inconclusive')) {
        if ([int]$json.$name -ne [int]$script:contract.expectedCounts.$name -or [int]$status.$name -ne [int]$script:contract.expectedCounts.$name) {
            if ([string]$status.lifecycle -eq 'behavioral_failure' -and $name -in @('passed','failed','skipped','inconclusive')) { Throw-RunFailure 'BEHAVIORAL_FAILURE' 'Protected ordinary-runtime fixture reported a genuine non-passing result.' 10 }
            Throw-RunFailure 'INFRASTRUCTURE_FAILURE' "Result count mismatch for $name." 30
        }
    }
    $jsonNames = @($json.results | ForEach-Object { [string]$_.fullName } | Sort-Object)
    $xmlCases = @($xml.SelectNodes('//test-case'))
    $xmlNames = @($xmlCases | ForEach-Object { [string]$_.fullname } | Sort-Object)
    $expectedNames = @($script:contract.expectedMethods | ForEach-Object { [string]$_ } | Sort-Object)
    if (($jsonNames -join "`n") -ne ($expectedNames -join "`n") -or ($xmlNames -join "`n") -ne ($expectedNames -join "`n")) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'XML/JSON method identities do not exactly match the frozen 13 methods.' 30 }
    if (@($xmlCases | Where-Object { [string]$_.result -ne 'Passed' }).Count -ne 0) { Throw-RunFailure 'BEHAVIORAL_FAILURE' 'NUnit XML contains a non-passing protected test.' 10 }
    if ([int]$status.divergentDuplicateCount -ne 0) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Divergent duplicate RunFinished payload was recorded.' 30 }
    if ([int]$status.runFinishedCallbackCount -lt 1 -or -not (Test-Path -LiteralPath (Join-Path $bridgeDirectory 'completion.claim'))) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Completion callback/claim evidence is incomplete.' 30 }
    $diagnostics = $status.diagnostics
    foreach ($field in @('atomicLifecycleWrite','identicalDuplicateIdempotence','divergentDuplicateFailsClosed','staleRunRejected','sizeLimitClassified','timeoutClassified','callbackRegistrationPerDomain','executeOnceAcrossReload','mismatchedSentinelRejected','unexpectedRootEntryRejected','preexistingBridgeChildRejected','redirectedOrNoncanonicalChildRejected','validSyntheticOwnershipLayoutCreated','noBridgeWriteOutsideChild','gameplayResultNotSynthesized')) {
        if (-not [bool]$diagnostics.$field) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' "Bridge self-diagnostic failed: $field" 30 }
    }
    if (-not (Test-Path -LiteralPath ([string]$diagnostics.outputPath)) -or (Get-FileHash -Algorithm SHA256 -LiteralPath ([string]$diagnostics.outputPath)).Hash -ne [string]$diagnostics.outputSha256) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Bridge self-diagnostic output hash mismatch.' 30 }
    if ((Get-FileHash -Algorithm SHA256 -LiteralPath $resultJsonPath).Hash -ne [string]$status.jsonSha256 -or (Get-FileHash -Algorithm SHA256 -LiteralPath $resultXmlPath).Hash -ne [string]$status.xmlSha256) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Bridge output hashes do not match durable artifacts.' 30 }
}

function Assert-RuntimeEvidence {
    $trace = Join-Path $runtimeEvidence 'public-input-trace.ndjson'
    if (-not (Test-Path -LiteralPath $trace) -or (Get-Item -LiteralPath $trace).Length -le 0 -or (Get-Item -LiteralPath $trace).Length -gt [long]$script:contract.limitsBytes.publicInputTrace) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Public-input trace is missing, empty, or oversized.' 30 }
    $screenshotsDirectory = Join-Path $runtimeEvidence 'screenshots'
    $screenshots = @(Get-ChildItem -LiteralPath $screenshotsDirectory -File -Filter '*.png' -ErrorAction SilentlyContinue)
    if ($screenshots.Count -lt 9) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Required screenshot evidence is incomplete.' 30 }
    $combined = 0L
    foreach ($screenshot in $screenshots) {
        if ($screenshot.Length -le 0 -or $screenshot.Length -gt [long]$script:contract.limitsBytes.screenshotEach) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' "Screenshot is empty or oversized: $($screenshot.Name)" 30 }
        $combined += $screenshot.Length
    }
    if ($combined -gt [long]$script:contract.limitsBytes.screenshotsCombined) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Combined screenshots exceed the frozen limit.' 30 }
    foreach ($resolution in @($expected.resolutions)) {
        foreach ($suffix in @($expected.requiredScreenshotSuffixesPerResolution)) {
            $name = "$($resolution.width)x$($resolution.height)-$suffix"
            if (-not (Test-Path -LiteralPath (Join-Path $screenshotsDirectory $name))) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' "Required screenshot is missing: $name" 30 }
        }
    }
    $nonLogBytes = 0L
    foreach ($file in @(Get-ChildItem -LiteralPath $runDirectory -File -Recurse | Where-Object { $_.FullName -ne $editorLog })) { $nonLogBytes += $file.Length }
    if ($nonLogBytes -gt [long]$script:contract.limitsBytes.nonLogEvidenceCombined) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Combined non-log evidence exceeds the frozen limit.' 30 }
}

function Assert-DedicatedLogClean {
    Check-Watchdog
    if (-not (Test-Path -LiteralPath $editorLog)) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Dedicated Editor log is missing.' 30 }
    $size = (Get-Item -LiteralPath $editorLog).Length
    if ($size -ge [long]$script:contract.limitsBytes.editorLog) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Dedicated Editor log reached the 64 MiB cap.' 30 }
    $pattern = [regex]::new('"severity":"(Error|Assert|Exception)"|Access version should be odd when acquiring lock|Audio/CriticalSection\.h:56|(?:Unhandled Exception|NullReferenceException|InvalidOperationException)', [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    $matches = [Collections.Generic.List[string]]::new()
    $lineNumber = 0
    foreach ($line in [IO.File]::ReadLines($editorLog, [Text.Encoding]::UTF8)) {
        $lineNumber++
        if ($pattern.IsMatch($line) -and $matches.Count -lt 5000) { $matches.Add("${lineNumber}:$line") }
    }
    $matches | Set-Content -LiteralPath (Join-Path $runDirectory 'dedicated-log-errors.txt') -Encoding utf8
    if ($matches.Count -gt 0) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Unexpected Error/Exception/Assert evidence exists in the dedicated Editor log.' 30 }
    Write-Json ([ordered]@{ path = $editorLog; bytes = $size; sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $editorLog).Hash; audioLockDetected = $false; unexpectedEntryCount = 0 }) (Join-Path $runDirectory 'dedicated-log-summary.json')
}

function Complete-IntegrityEvidence {
    $post = Get-WorkingTreeSnapshot
    Write-Json $post (Join-Path $runDirectory 'working-tree-after.json')
    (& git -C $project status --porcelain=v2 --branch --untracked-files=all) | Set-Content -LiteralPath (Join-Path $runDirectory 'git-after.txt') -Encoding utf8
    $postProtected = Get-ProtectedHashes
    Write-Json $postProtected (Join-Path $runDirectory 'protected-hashes-after.json')
    $failure = $null
    if ($null -ne $script:initialHead -and (& git -C $project rev-parse HEAD).Trim() -ne $script:initialHead) { $failure = 'Repository HEAD changed during acceptance.' }
    elseif (@($postProtected | Where-Object { -not $_.matches }).Count -ne 0) { $failure = 'Protected hashes changed during acceptance.' }
    elseif ($null -ne $script:preSnapshot) { $failure = Compare-Snapshots $script:preSnapshot $post }
    try { Assert-PreservedHashes } catch { if ($null -eq $failure) { $failure = $_.Exception.Message } }
    return $failure
}

function Write-FinalResult {
    param([string]$Classification, [string]$Reason, [int]$Code)
    $result = [ordered]@{
        schemaVersion = 2; taskId = 'M1-D01'; infrastructureValidationOnly = $true
        classification = $Classification; reason = $Reason; exitCode = $Code
        runId = $runId; repositoryHead = $script:initialHead; branch = (& git -C $project branch --show-current).Trim()
        unityVersion = [string]$script:contract.unityVersion; scene = [string]$script:contract.scene; entrypoint = [string]$script:contract.entrypoint
        editorPid = $script:ownedPid; editorCommandLine = $script:ownedCommandLine; editorLog = $editorLog
        bridgeStatus = $statusPath; resultJson = $resultJsonPath; resultXml = $resultXmlPath
        publicInputTrace = (Join-Path $runtimeEvidence 'public-input-trace.ndjson')
        screenshots = if (Test-Path -LiteralPath (Join-Path $runtimeEvidence 'screenshots')) { @((Get-ChildItem -LiteralPath (Join-Path $runtimeEvidence 'screenshots') -File -Filter '*.png').FullName) } else { @() }
        commandTranscript = $commandLog; lifecycleObservations = $lifecycleLog; shutdownProof = (Join-Path $runDirectory 'shutdown-proof.json')
        watchdogTriggered = $script:watchTriggered; watchdogReason = $script:watchReason; shutdown = $script:shutdown
        completedUtc = [DateTime]::UtcNow.ToString('o')
    }
    Write-AtomicJson $result $finalResultPath
    $result | ConvertTo-Json -Depth 12
}

if (-not (Test-Path -LiteralPath $project -PathType Container)) { throw "Project path does not exist: $project" }
if ($RunStatusPollingSelfTest) {
    Invoke-StatusPollingSelfTests $StatusPollingSelfTestOutput | ConvertTo-Json -Depth 8
    exit 0
}
if (-not (Test-Path -LiteralPath $acceptanceRoot -PathType Container)) { New-Item -ItemType Directory -Path $acceptanceRoot -Force | Out-Null }
if (-not (Test-Path -LiteralPath $runsRoot -PathType Container)) { New-Item -ItemType Directory -Path $runsRoot -Force | Out-Null }
New-Item -ItemType Directory -Path $runDirectory | Out-Null
Set-Content -LiteralPath $commandLog -Value '' -Encoding utf8
Set-Content -LiteralPath $lifecycleLog -Value '' -Encoding utf8
[IO.File]::WriteAllBytes($editorLog, [byte[]]@())

try {
    foreach ($required in @($contractPath, $expectedPath, $lockPath)) { if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' "Required protected runner input missing: $required" 30 } }
    $script:contract = Get-Content -Raw -LiteralPath $contractPath | ConvertFrom-Json
    $expected = Get-Content -Raw -LiteralPath $expectedPath | ConvertFrom-Json
    $script:lock = Get-Content -Raw -LiteralPath $lockPath | ConvertFrom-Json
    if ([int]$script:contract.schemaVersion -ne 2) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Runner contract schema version is not 2.' 30 }

    $existingLogs = @(Get-ChildItem -LiteralPath $runsRoot -File -Filter 'Editor.log' -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.FullName -ne $editorLog -and $_.Length -gt 2 })
    $retainedBytes = 0L
    foreach ($existingLog in $existingLogs) { $retainedBytes += [long]$existingLog.Length }
    if ($existingLogs.Count -gt [int]$script:contract.retention.maximumEditorLogs -or [long]$retainedBytes -ge [long]$script:contract.limitsBytes.retainedEditorLogs) { Throw-RunFailure 'ENVIRONMENTAL_BLOCKAGE' 'Task-local Editor-log retention limit reached; owner archival is required.' 20 }

    $protectedBefore = Get-ProtectedHashes -FailOnMismatch
    Write-Json $protectedBefore (Join-Path $runDirectory 'protected-hashes-before.json')

    $script:initialHead = (& git -C $project rev-parse HEAD).Trim()
    $branch = (& git -C $project branch --show-current).Trim()
    $upstream = (& git -C $project rev-parse --abbrev-ref --symbolic-full-name '@{upstream}').Trim()
    $divergence = (& git -C $project rev-list --left-right --count 'HEAD...@{upstream}').Trim()
    if ($branch -ne [string]$script:contract.branch -or $upstream -ne "origin/$($script:contract.branch)" -or $divergence -ne "0`t0") { Throw-RunFailure 'ENVIRONMENTAL_BLOCKAGE' "Git branch/upstream mismatch: branch=$branch upstream=$upstream divergence=$divergence" 20 }
    if (@(& git -C $project diff --cached --name-only).Count -ne 0) { Throw-RunFailure 'ENVIRONMENTAL_BLOCKAGE' 'Git index is not empty.' 20 }
    (& git -C $project status --porcelain=v2 --branch --untracked-files=all) | Set-Content -LiteralPath (Join-Path $runDirectory 'git-before.txt') -Encoding utf8
    $script:preSnapshot = Get-WorkingTreeSnapshot
    Write-Json $script:preSnapshot (Join-Path $runDirectory 'working-tree-before.json')
    Assert-AllowedDirtySnapshot $script:preSnapshot
    Assert-PreservedHashes
    Write-AtomicJson ([ordered]@{ schemaVersion=1; taskId='M1-D01'; runId=$runId; testedHead=$script:initialHead; branch=$branch; rootPath=[IO.Path]::GetFullPath($runDirectory); creationUtc=[DateTime]::UtcNow.ToString('o'); pidState='unassigned'; exactEditorPid=0; projectPath=$project; unityVersion=[string]$script:contract.unityVersion; automated=$false; commandLineHash=$null; taskLocalLogPath=$editorLog }) (Join-Path $runDirectory 'run-ownership.json')
    Capture-SolutionPreRun
    Check-SolutionWatcher

    $behaviorSource = Join-Path $project 'Assets\Bloomdrawn\Tests\Acceptance\M1D01RuntimeDragAcceptanceTests.cs'
    $bridgeSource = Join-Path $project 'Assets\Bloomdrawn\Tests\Acceptance\Infrastructure\M1D01AcceptanceTestBridge.cs'
    $behaviorText = Get-Content -Raw -LiteralPath $behaviorSource
    if (@([regex]::Matches($behaviorText, '\[UnityTest')).Count -ne 13) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Frozen behavioral source no longer contains exactly 13 UnityTest methods.' 30 }
    $bridgeText = Get-Content -Raw -LiteralPath $bridgeSource
    foreach ($forbidden in @('PipelineTestRunner', 'CliCommand\s*\(\s*"(?:run_tests|test_status|cancel_tests)"', 'CardInteractionController', 'CombatSession.Submit', 'OnPointerDown\s*\(', 'BeginCardDrag\s*\(')) {
        if ($bridgeText -match $forbidden) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' "Forbidden bridge bypass/API detected: $forbidden" 30 }
    }

    Assert-NoProjectEditor
    foreach ($stale in @('Temp\pipeline_test_request.json', 'Temp\pipeline_test_status.json')) { if (Test-Path -LiteralPath (Join-Path $project $stale)) { Throw-RunFailure 'ENVIRONMENTAL_BLOCKAGE' "Stale Pipeline lifecycle file exists: $stale" 20 } }
    if (Test-Path -LiteralPath $globalRuntimeEvidence) { Remove-Item -LiteralPath $globalRuntimeEvidence -Recurse -Force }

    $launchArgs = @('-logFile', ('"{0}"' -f $editorLog))
    $launchRaw = & (Join-Path $project 'Tools\open-automated-editor.ps1') -ProjectPath $project -AdditionalArguments $launchArgs
    $launchExit = $LASTEXITCODE
    Record-Command 'Tools/open-automated-editor.ps1 -AdditionalArguments -logFile <unique-task-log>' $launchExit @($launchRaw)
    if ($launchExit -ne 0) { Throw-RunFailure 'ENVIRONMENTAL_BLOCKAGE' 'Fresh automated Unity Editor could not be launched.' 20 }
    $launch = $launchRaw | ConvertFrom-Json
    if ([string]$launch.status -ne 'launched' -or -not [bool]$launch.automated -or [string]$launch.pinnedUnityVersion -ne [string]$script:contract.unityVersion) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Launcher did not prove a fresh owned automated pinned Editor.' 30 }
    $script:ownedPid = [int]$launch.processId
    Start-Sleep -Milliseconds 100
    $ownedCim = Get-CimInstance Win32_Process -Filter "ProcessId = $($script:ownedPid)"
    if ($null -eq $ownedCim) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Owned Unity process disappeared immediately after launch.' 30 }
    $script:ownedCommandLine = [string]$ownedCim.CommandLine
    if ($script:ownedCommandLine -notmatch '(?i)(?:^|\s)-automated(?:\s|$)' -or $script:ownedCommandLine.IndexOf($project, [StringComparison]::OrdinalIgnoreCase) -lt 0 -or $script:ownedCommandLine.IndexOf($editorLog, [StringComparison]::OrdinalIgnoreCase) -lt 0) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Owned PID command line does not prove project, -automated, and unique -logFile ownership.' 30 }
    Write-Json ([ordered]@{ runId = $runId; pid = $script:ownedPid; commandLine = $script:ownedCommandLine; project = $project; unityVersion = $launch.pinnedUnityVersion; automated = $true; log = $editorLog }) (Join-Path $runDirectory 'editor-ownership.json')
    $commandLineHash = [BitConverter]::ToString(([Security.Cryptography.SHA256]::Create()).ComputeHash([Text.Encoding]::UTF8.GetBytes($script:ownedCommandLine))).Replace('-','')
    Write-AtomicJson ([ordered]@{ schemaVersion=1; taskId='M1-D01'; runId=$runId; testedHead=$script:initialHead; branch=$branch; rootPath=[IO.Path]::GetFullPath($runDirectory); creationUtc=[DateTime]::UtcNow.ToString('o'); pidState='owned'; exactEditorPid=$script:ownedPid; projectPath=$project; unityVersion=[string]$script:contract.unityVersion; automated=$true; commandLineHash=$commandLineHash; taskLocalLogPath=[IO.Path]::GetFullPath($editorLog) }) (Join-Path $runDirectory 'run-ownership.json')
    Start-ExternalWatchdog
    Check-SolutionWatcher

    [void](Wait-ForPipelineReady)
    Wait-ForCompilation
    $healthResponse = Invoke-UnityCommand 'bloom.health'
    if ($healthResponse.ExitCode -ne 0) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'bloom.health failed.' 30 }
    try { $health = $healthResponse.Text | ConvertFrom-Json } catch { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'bloom.health response is invalid.' 30 }
    Write-Json $health (Join-Path $runDirectory 'editor-health.json')
    $healthResult = $health.data.result
    if ([string]$healthResult.EditorVersion -ne [string]$script:contract.unityVersion -or -not [bool]$healthResult.PipelineReady -or -not [bool]$healthResult.EditorReady -or [bool]$healthResult.CompilationActive -or [bool]$healthResult.CompileFailed -or -not [bool]$healthResult.CompileSucceeded) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Pinned Editor readiness/compilation health failed.' 30 }

    $script:consolePreTestCursor = Get-ConsoleEvidence -Since $script:consoleStartupCursor -OutputName 'console-startup-to-pretest.json'
    $startResponse = Invoke-UnityCommand ([string]$script:contract.bridge.startCommand) @('--run_id', $runId, '--evidence_directory', $runDirectory)
    if ($startResponse.ExitCode -ne 0 -and -not (Test-Path -LiteralPath $statusPath)) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Bridge start command failed without durable matching status.' 30 }
    Wait-ForBridgeTerminal
    if ([string]$script:bridgeStatus.lifecycle -eq 'completed') {
        $elapsed = ([DateTime]::UtcNow - [DateTime]::Parse([string]$script:bridgeStatus.completingUtc).ToUniversalTime()).TotalSeconds
        if ($elapsed -lt [double]$script:contract.timeoutsSeconds.postCompletionQuiescence) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Bridge reported completion before the duplicate quiescence window.' 30 }
    }
    Assert-BridgeResults
    Assert-RuntimeEvidence
    [void](Get-ConsoleEvidence -Since ([long]$script:consolePreTestCursor) -OutputName 'console-pretest-to-posttest.json')
    Check-Watchdog

    Stop-OwnedEditor
    if (-not [bool]$script:shutdown.pidExited -or [int]$script:shutdown.projectOwnerCount -ne 0 -or -not [bool]$script:shutdown.pipelineAbsent -or [bool]$script:shutdown.forced) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' 'Owned Editor did not shut down cleanly with complete project/Pipeline absence.' 30 }
    Restore-SolutionAfterShutdown
    Assert-DedicatedLogClean

    $integrityFailure = Complete-IntegrityEvidence
    if ($null -ne $integrityFailure) { Throw-RunFailure 'INFRASTRUCTURE_FAILURE' $integrityFailure 30 }
    $script:classification = 'PASS'; $script:reason = 'Fresh protected bridge run produced exact 13/13 results and complete clean infrastructure evidence.'; $script:exitCode = 0
}
catch [M1D01AcceptanceFailure] {
    $script:classification = $_.Exception.Classification
    $script:reason = $_.Exception.Message
    $script:exitCode = $_.Exception.ExitCode
}
catch {
    $script:classification = 'INFRASTRUCTURE_FAILURE'
    $script:reason = $_.Exception.GetType().FullName + ': ' + $_.Exception.Message
    $script:exitCode = 30
}
finally {
    if ($null -ne $script:ownedPid -and (Get-Process -Id $script:ownedPid -ErrorAction SilentlyContinue)) {
        try { Invoke-BridgeAbort -Quiet } catch { }
        try { Stop-OwnedEditor -AbortContainment } catch { $script:reason += " Containment error: $($_.Exception.Message)"; $script:classification = 'INFRASTRUCTURE_FAILURE'; $script:exitCode = 30 }
    }
    if ($null -ne $script:solutionPre -and -not $script:solutionRestored -and ($null -eq $script:ownedPid -or -not (Get-Process -Id $script:ownedPid -ErrorAction SilentlyContinue))) {
        try { Restore-SolutionAfterShutdown } catch { $script:reason += " Solution restoration error: $($_.Exception.Message)"; $script:classification = 'INFRASTRUCTURE_FAILURE'; $script:exitCode = 30 }
    }
    if ($null -ne $script:contract -and (Test-Path -LiteralPath $runDirectory)) {
        try {
            Check-Watchdog
            if (Test-Path -LiteralPath $editorLog) {
                $size = (Get-Item -LiteralPath $editorLog).Length
                if ($size -ge [long]$script:contract.limitsBytes.editorLog) { $script:classification = 'INFRASTRUCTURE_FAILURE'; $script:reason += ' Dedicated log cap reached.'; $script:exitCode = 30 }
            }
        } catch { $script:classification = 'INFRASTRUCTURE_FAILURE'; $script:reason += " Watchdog finalization: $($_.Exception.Message)"; $script:exitCode = 30 }
        try {
            $integrityFailure = Complete-IntegrityEvidence
            if ($null -ne $integrityFailure) { $script:classification = 'INFRASTRUCTURE_FAILURE'; $script:reason += " Integrity failure: $integrityFailure"; $script:exitCode = 30 }
        } catch { $script:classification = 'INFRASTRUCTURE_FAILURE'; $script:reason += " Integrity evidence error: $($_.Exception.Message)"; $script:exitCode = 30 }
    }
    if (Test-Path -LiteralPath $runDirectory) { Write-FinalResult $script:classification $script:reason $script:exitCode }
}

exit $script:exitCode
