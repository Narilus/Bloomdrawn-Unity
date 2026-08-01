[CmdletBinding()]
param(
    [Parameter()]
    [string]$ProjectPath = (Get-Location).Path,

    [Parameter()]
    [int]$EditorTimeoutSeconds = 180,

    [Parameter()]
    [int]$TestTimeoutSeconds = 900
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$project = [System.IO.Path]::GetFullPath($ProjectPath).TrimEnd('\', '/')
$contractPath = Join-Path $project 'Tools\Acceptance\M1-D01-runner-contract.json'
$expectedPath = Join-Path $project 'Tools\Acceptance\M1-D01-expected-values.json'
$lockPath = Join-Path $project 'acceptance\locks\M1-D01-protected.sha256.json'
$evidenceRoot = Join-Path $project 'Logs\M1-D01\Acceptance'
$runtimeEvidence = Join-Path $evidenceRoot 'runtime'
$resultPath = Join-Path $evidenceRoot 'acceptance-result.json'
$commandLog = Join-Path $evidenceRoot 'commands.ndjson'
$testOutput = Join-Path $evidenceRoot 'protected-tests.json'
$healthOutput = Join-Path $evidenceRoot 'editor-health.json'
$editorStateOutput = Join-Path $evidenceRoot 'editor-process-state.json'
$consoleOutput = Join-Path $evidenceRoot 'console-errors.json'
$gitBeforeOutput = Join-Path $evidenceRoot 'git-before.txt'
$gitAfterOutput = Join-Path $evidenceRoot 'git-after.txt'

function Exit-WithResult {
    param(
        [Parameter(Mandatory = $true)][string]$Classification,
        [Parameter(Mandatory = $true)][string]$Reason,
        [int]$Code
    )
    $result = [ordered]@{
        schemaVersion = 1
        taskId = 'M1-D01'
        classification = $Classification
        reason = $Reason
        repositoryHead = (& git -C $project rev-parse HEAD).Trim()
        branch = (& git -C $project branch --show-current).Trim()
        unityVersion = if (Test-Path -LiteralPath $healthOutput) { ((Get-Content -Raw -LiteralPath $healthOutput | ConvertFrom-Json).data.result.EditorVersion) } else { $null }
        scene = 'Assets/Scenes/CombatStage.unity'
        entrypoint = 'ordinary Play Mode automatic CombatStageRuntimeBootstrap'
        testOutput = $testOutput
        healthOutput = $healthOutput
        editorStateOutput = $editorStateOutput
        consoleOutput = $consoleOutput
        publicInputTrace = (Join-Path $runtimeEvidence 'public-input-trace.ndjson')
        screenshots = if (Test-Path -LiteralPath (Join-Path $runtimeEvidence 'screenshots')) { @((Get-ChildItem -LiteralPath (Join-Path $runtimeEvidence 'screenshots') -File -Filter '*.png').FullName) } else { @() }
        gitBefore = $gitBeforeOutput
        gitAfter = $gitAfterOutput
        completedUtc = [DateTime]::UtcNow.ToString('o')
    }
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $resultPath -Encoding utf8
    $result | ConvertTo-Json -Depth 8
    exit $Code
}

function Record-Command {
    param([string]$Command, [int]$ExitCode)
    [ordered]@{ sequence = (@(Get-Content -LiteralPath $commandLog -ErrorAction SilentlyContinue).Count + 1); command = $Command; exitCode = $ExitCode; utc = [DateTime]::UtcNow.ToString('o') } |
        ConvertTo-Json -Compress | Add-Content -LiteralPath $commandLog -Encoding utf8
}

function Get-DirtyPaths {
    $lines = @(& git -C $project status --porcelain=v1 --untracked-files=all)
    return @($lines | ForEach-Object { if ($_.Length -ge 4) { $_.Substring(3).Trim('"') } })
}

function Test-AllowedDirtyPath {
    param([string]$Path)
    if ($Path -eq 'Bloomdrawn-Unity.slnx') { return $true }
    if ($Path -eq 'Assets/Bloomdrawn/Tests/Acceptance.meta') { return $true }
    return $Path.StartsWith('Assets/Bloomdrawn/Tests/Acceptance/', [StringComparison]::Ordinal) -or
           $Path.StartsWith('Tools/Acceptance/', [StringComparison]::Ordinal) -or
           $Path.StartsWith('acceptance/locks/', [StringComparison]::Ordinal)
}

New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
if (Test-Path -LiteralPath $runtimeEvidence) { Remove-Item -LiteralPath $runtimeEvidence -Recurse -Force }
New-Item -ItemType Directory -Path $runtimeEvidence -Force | Out-Null
Set-Content -LiteralPath $commandLog -Value '' -Encoding utf8

try {
    if (-not (Test-Path -LiteralPath $contractPath) -or -not (Test-Path -LiteralPath $expectedPath) -or -not (Test-Path -LiteralPath $lockPath)) {
        Exit-WithResult 'INFRASTRUCTURE_FAILURE' 'Protected runner contract, expected values, or hash lock is missing.' 30
    }
    $contract = Get-Content -Raw -LiteralPath $contractPath | ConvertFrom-Json
    $expected = Get-Content -Raw -LiteralPath $expectedPath | ConvertFrom-Json
    $lock = Get-Content -Raw -LiteralPath $lockPath | ConvertFrom-Json

    foreach ($entry in $lock.files) {
        $path = Join-Path $project ([string]$entry.path).Replace('/', '\')
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Exit-WithResult 'INFRASTRUCTURE_FAILURE' "Protected file missing: $($entry.path)" 30 }
        $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash
        if ($actual -ne ([string]$entry.sha256).ToUpperInvariant()) { Exit-WithResult 'INFRASTRUCTURE_FAILURE' "Protected hash mismatch: $($entry.path)" 30 }
    }

    $branch = (& git -C $project branch --show-current).Trim()
    $head = (& git -C $project rev-parse HEAD).Trim()
    $upstream = (& git -C $project rev-parse --abbrev-ref --symbolic-full-name '@{upstream}').Trim()
    $divergence = (& git -C $project rev-list --left-right --count "HEAD...@{upstream}").Trim()
    (& git -C $project status --porcelain=v2 --branch) | Set-Content -LiteralPath $gitBeforeOutput -Encoding utf8
    if ($branch -ne $contract.branch -or $upstream -ne "origin/$($contract.branch)" -or $divergence -ne "0`t0") {
        Exit-WithResult 'ENVIRONMENTAL_BLOCKAGE' "Git branch/upstream mismatch: branch=$branch upstream=$upstream divergence=$divergence" 20
    }
    foreach ($dirty in Get-DirtyPaths) { if (-not (Test-AllowedDirtyPath $dirty)) { Exit-WithResult 'ENVIRONMENTAL_BLOCKAGE' "Unexpected pre-run working-tree path: $dirty" 20 } }
    $solutionHash = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $project $contract.preexistingException.path)).Hash
    if ($solutionHash -ne $contract.preexistingException.sha256) { Exit-WithResult 'ENVIRONMENTAL_BLOCKAGE' 'Bloomdrawn-Unity.slnx hash differs from the preserved owner baseline.' 20 }

    $acceptanceSource = Get-Content -Raw -LiteralPath (Join-Path $project 'Assets\Bloomdrawn\Tests\Acceptance\M1D01RuntimeDragAcceptanceTests.cs')
    $prohibitedPatterns = @(
        '\.BeginCardDrag\s*\(', '\.UpdateCardDrag\s*\(', '\.ReleaseCardDrag\s*\(', '\.ClickCard\s*\(', '\.SelectEnemy\s*\(',
        '\.OnPointerDown\s*\(', '\.OnBeginDrag\s*\(', '\.OnDrag\s*\(', '\.OnEndDrag\s*\(', '\.OnPointerClick\s*\(',
        'CombatSession\s*\(', '\.Submit\s*\(', '\.Play\s*\(', 'load-combat-fixture', 'reset-combat-fixture', 'CompletePresentation\s*\(',
        'new\s+GameObject\s*\(', 'new\s+EventSystem\s*\(', 'SetParent\s*\(', '\.anchoredPosition\s*='
    )
    foreach ($pattern in $prohibitedPatterns) { if ($acceptanceSource -match $pattern) { Exit-WithResult 'INFRASTRUCTURE_FAILURE' "Prohibited acceptance shortcut pattern detected: $pattern" 30 } }

    $stateCommand = "Tools/get-unity-editor-state.ps1 -RequireAutomated"
    $stateJson = & (Join-Path $project 'Tools\get-unity-editor-state.ps1') -ProjectPath $project -RequireAutomated 2>$null
    $stateExit = $LASTEXITCODE
    Record-Command $stateCommand $stateExit
    if ($stateExit -eq 3) {
        $launchJson = & (Join-Path $project 'Tools\open-automated-editor.ps1') -ProjectPath $project
        Record-Command 'Tools/open-automated-editor.ps1' $LASTEXITCODE
        $deadline = (Get-Date).AddSeconds($EditorTimeoutSeconds)
        do {
            Start-Sleep -Seconds 3
            $stateJson = & (Join-Path $project 'Tools\get-unity-editor-state.ps1') -ProjectPath $project -RequireAutomated 2>$null
            $stateExit = $LASTEXITCODE
            $statusJson = & unity --json status --project-path $project 2>$null
        } while ((Get-Date) -lt $deadline -and ($stateExit -ne 0 -or $LASTEXITCODE -ne 0))
    }
    elseif ($stateExit -ne 0) {
        Exit-WithResult 'ENVIRONMENTAL_BLOCKAGE' 'A project Editor exists but is not a single approved -automated instance.' 20
    }
    $stateJson | Set-Content -LiteralPath $editorStateOutput -Encoding utf8

    $status = & unity --json status --project-path $project
    $statusExit = $LASTEXITCODE
    Record-Command 'unity --json status --project-path <project>' $statusExit
    $statusObject = if ($statusExit -eq 0) { $status | ConvertFrom-Json } else { $null }
    if ($statusExit -ne 0 -or $null -eq $statusObject -or @($statusObject.data.instances).Count -ne 1 -or $statusObject.data.instances[0].state -ne 'ready') {
        Exit-WithResult 'ENVIRONMENTAL_BLOCKAGE' 'Automated Editor/Pipeline did not reach ready state within the bounded timeout.' 20
    }

    $health = & unity --json command --project-path $project bloom.health
    $healthExit = $LASTEXITCODE
    $health | Set-Content -LiteralPath $healthOutput -Encoding utf8
    Record-Command 'unity --json command --project-path <project> bloom.health' $healthExit
    if ($healthExit -ne 0) { Exit-WithResult 'INFRASTRUCTURE_FAILURE' 'bloom.health command failed.' 30 }
    $healthObject = $health | ConvertFrom-Json
    $healthResult = $healthObject.data.result
    if ($healthResult.EditorVersion -ne $contract.unityVersion -or -not $healthResult.PipelineReady -or -not $healthResult.EditorReady -or
        $healthResult.CompilationActive -or $healthResult.CompileFailed -or -not $healthResult.CompileSucceeded) {
        Exit-WithResult 'INFRASTRUCTURE_FAILURE' 'Editor version/readiness/compilation precondition failed.' 30
    }

    $clearConsole = & unity --json command --project-path $project clear_console
    Record-Command 'unity --json command --project-path <project> clear_console' $LASTEXITCODE
    if ($LASTEXITCODE -ne 0) { Exit-WithResult 'INFRASTRUCTURE_FAILURE' 'Could not establish a clean Console evidence boundary.' 30 }
    $consoleBoundaryRaw = & unity --json command --project-path $project console --level error --tail 1
    $consoleBoundaryExit = $LASTEXITCODE
    Record-Command 'unity --json command --project-path <project> console --level error --tail 1 (boundary)' $consoleBoundaryExit
    if ($consoleBoundaryExit -ne 0) { Exit-WithResult 'INFRASTRUCTURE_FAILURE' 'Could not record the pre-run Console cursor.' 30 }
    $consoleBoundary = $consoleBoundaryRaw | ConvertFrom-Json
    $consoleCursor = [long]$consoleBoundary.data.result.cursor

    $testCommandText = "unity --json command --project-path <project> run_tests --mode playmode --filter $($contract.testFilter) --filter_type testName --async_tests true --timeout $TestTimeoutSeconds"
    $testLaunch = & unity --json command --project-path $project run_tests --mode playmode --filter $contract.testFilter --filter_type testName --async_tests true --timeout $TestTimeoutSeconds
    $testExit = $LASTEXITCODE
    Record-Command $testCommandText $testExit
    $testLaunchObject = if ($testExit -eq 0) { $testLaunch | ConvertFrom-Json } else { $null }
    if ($testExit -ne 0 -or $null -eq $testLaunchObject -or $testLaunchObject.data.result.result -ne 'running') {
        $testLaunch | Set-Content -LiteralPath $testOutput -Encoding utf8
        Exit-WithResult 'INFRASTRUCTURE_FAILURE' 'Protected Play Mode tests did not start asynchronously.' 30
    }
    $testDeadline = (Get-Date).AddSeconds($TestTimeoutSeconds)
    $testRunning = $true
    do {
        Start-Sleep -Seconds 3
        $tests = & unity --json command --project-path $project test_status 2>$null
        $testStatusExit = $LASTEXITCODE
        if ($testStatusExit -eq 0) {
            try {
                $pollEnvelope = $tests | ConvertFrom-Json
                $pollReport = ([string]$pollEnvelope.data.result) | ConvertFrom-Json
                $testRunning = [string]$pollReport.status -eq 'running'
            }
            catch {
                $testRunning = $true
            }
        }
    } while ((Get-Date) -lt $testDeadline -and ($testStatusExit -ne 0 -or $testRunning))
    $tests | Set-Content -LiteralPath $testOutput -Encoding utf8
    Record-Command 'unity --json command --project-path <project> test_status (bounded poll)' $testStatusExit
    if ($testStatusExit -ne 0 -or $testRunning) { Exit-WithResult 'INFRASTRUCTURE_FAILURE' 'Protected Play Mode tests timed out or test_status was unavailable.' 30 }

    $console = & unity --json command --project-path $project console --level error --tail 500 --since $consoleCursor
    $consoleExit = $LASTEXITCODE
    $console | Set-Content -LiteralPath $consoleOutput -Encoding utf8
    Record-Command 'unity --json command --project-path <project> console --level error --tail 500 --since <pre-run-cursor>' $consoleExit
    if ($consoleExit -ne 0) { Exit-WithResult 'INFRASTRUCTURE_FAILURE' 'Could not read post-run Console evidence.' 30 }
    $consoleEnvelope = $console | ConvertFrom-Json
    if (@($consoleEnvelope.data.result.entries).Count -gt 0) { Exit-WithResult 'INFRASTRUCTURE_FAILURE' 'Unexpected Console error/exception occurred during the protected run.' 30 }

    (& git -C $project status --porcelain=v2 --branch) | Set-Content -LiteralPath $gitAfterOutput -Encoding utf8
    foreach ($dirty in Get-DirtyPaths) { if (-not (Test-AllowedDirtyPath $dirty)) { Exit-WithResult 'INFRASTRUCTURE_FAILURE' "Unexpected post-run working-tree mutation: $dirty" 30 } }
    $solutionHashAfter = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $project $contract.preexistingException.path)).Hash
    if ($solutionHashAfter -ne $contract.preexistingException.sha256) { Exit-WithResult 'INFRASTRUCTURE_FAILURE' 'Bloomdrawn-Unity.slnx changed during acceptance.' 30 }

    $trace = Join-Path $runtimeEvidence 'public-input-trace.ndjson'
    if (-not (Test-Path -LiteralPath $testOutput) -or -not (Test-Path -LiteralPath $trace) -or (Get-Item -LiteralPath $trace).Length -eq 0) {
        Exit-WithResult 'INFRASTRUCTURE_FAILURE' 'Required protected test output or public-input trace is missing.' 30
    }
    $screenshots = @(Get-ChildItem -LiteralPath (Join-Path $runtimeEvidence 'screenshots') -File -Filter '*.png' -ErrorAction SilentlyContinue)
    if ($screenshots.Count -lt 9) { Exit-WithResult 'INFRASTRUCTURE_FAILURE' 'Required three-state, three-resolution screenshot evidence is missing.' 30 }
    foreach ($resolution in $expected.resolutions) {
        foreach ($suffix in $expected.requiredScreenshotSuffixesPerResolution) {
            $requiredName = "$($resolution.width)x$($resolution.height)-$suffix"
            if (-not (Test-Path -LiteralPath (Join-Path $runtimeEvidence "screenshots\$requiredName"))) {
                Exit-WithResult 'INFRASTRUCTURE_FAILURE' "Required screenshot evidence is missing: $requiredName" 30
            }
        }
    }

    $rawTests = Get-Content -Raw -LiteralPath $testOutput
    $testEnvelope = $rawTests | ConvertFrom-Json
    $testReport = ([string]$testEnvelope.data.result) | ConvertFrom-Json
    $hasKnownBehavioralMarker = $false
    foreach ($marker in $expected.baselineBehavioralFailureMarkers) { if ($rawTests.Contains([string]$marker)) { $hasKnownBehavioralMarker = $true; break } }
    $reportsFailure = [int]$testReport.summary.failed -gt 0

    if ([int]$testReport.summary.total -le 0) { Exit-WithResult 'INFRASTRUCTURE_FAILURE' 'Protected test filter discovered zero tests.' 30 }
    if ($reportsFailure -and $hasKnownBehavioralMarker) { Exit-WithResult 'BEHAVIORAL_FAILURE' 'Protected ordinary-runtime test failed an approved DD-28 behavioral criterion.' 10 }
    if ($reportsFailure) { Exit-WithResult 'INFRASTRUCTURE_FAILURE' 'Protected test failed without an approved baseline behavioral marker.' 30 }
    Exit-WithResult 'PASS' 'All protected M1-D01 criteria passed; this runner result is not an acceptance declaration.' 0
}
catch {
    Exit-WithResult 'INFRASTRUCTURE_FAILURE' ($_.Exception.GetType().FullName + ': ' + $_.Exception.Message) 30
}
