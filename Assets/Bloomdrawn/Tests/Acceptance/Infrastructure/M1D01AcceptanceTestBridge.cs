using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Bloomdrawn.Tests.Acceptance.Infrastructure
{
    [InitializeOnLoad]
    public static class M1D01AcceptanceTestBridge
    {
        private const string TaskId = "M1-D01";
        private const string Fixture = "Bloomdrawn.Tests.PlayMode.Acceptance.M1D01RuntimeDragAcceptanceTests";
        private const string Mode = "PlayMode";
        private const int JsonLimit = 16 * 1024 * 1024;
        private const int XmlLimit = 16 * 1024 * 1024;
        private const int QuiescenceSeconds = 5;

        private static readonly string[] ExpectedMethods =
        {
            Fixture + ".C01_OrdinaryBootstrap_RuntimeHealth_UsesCommittedComposition",
            Fixture + ".C02_PointerReachability_BeginDragAndPointerRelationship_AreRealEventSystemInput",
            Fixture + ".C03_HoverFocus_RaisesAndRestoresWithoutAuthoritativeMutation",
            Fixture + ".C04_Arm_RemainArmedAboveUpperEdge_AndDownwardDisarm",
            Fixture + ".C05_ReleaseBelow_CancelsWithoutAnyAuthoritativeMutationOrDuplicateView",
            Fixture + ".C06_TargetCompleteArmedRelease_SubmitsExactlyOneAcceptedCommand",
            Fixture + ".C07_ExplicitTargetRelease_StagesOneCardHighlightsTargetsWithoutMutation",
            Fixture + ".C08_TargetCancellation_EscapeAndRightClick_RestoreWithoutMutation",
            Fixture + ".C09_LegalTargetPointerSelection_SubmitsExactlyOneCompleteCommand",
            Fixture + ".C10_RepeatedPublicDragCancelCycles_HaveNoDriftDuplicatesOrStaleViews",
            Fixture + ".C11_ClickCompatibility_TargetCompleteAndExplicitTargetRoutesRemainFunctional",
            Fixture + ".C12_KeyboardCompatibility_NumberEscapeEnterAndSpaceRemainFunctional",
            Fixture + ".C13_ResponsiveRuntimeEvidence_ExercisesActualViewsAtAllRequiredResolutions"
        };

        private static TestRunnerApi s_Api;
        private static BridgeCallbacks s_Callbacks;
        private static BridgeStatus s_Status;
        private static string s_RunRoot;
        private static string s_RunDirectory;
        private static int s_RunFinishedGuard;

        static M1D01AcceptanceTestBridge()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.delayCall += RecoverPersistedRuns;
        }

        [CliCommand("m1d01.acceptance.start", "Start the frozen M1-D01 protected Play Mode fixture through the public Unity TestRunnerApi.")]
        public static BridgeCommandResult Start(
            [CliArg("run_id", "Cryptographically unique runner-owned run ID.", Required = true)] string runId,
            [CliArg("evidence_directory", "New task-local evidence directory for this run.", Required = true)] string evidenceDirectory)
        {
            if (string.IsNullOrWhiteSpace(runId) || !Guid.TryParseExact(runId, "N", out _))
                throw new ArgumentException("run_id must be a lowercase or uppercase 32-character GUID without separators.", nameof(runId));

            var runRoot = NormalizeAndValidateRunRoot(runId, evidenceDirectory);
            RejectStalePipelineTestFiles();
            if (FindActiveRuns().Length != 0) throw new InvalidOperationException("Another M1-D01 bridge run is active or stale.");
            ValidateRunnerOwnedRoot(runRoot, runId);
            var runDirectory = Path.Combine(runRoot, "bridge");
            if (Directory.Exists(runDirectory) || File.Exists(runDirectory))
                throw new InvalidOperationException("The bridge-owned child already exists.");

            Directory.CreateDirectory(runDirectory);
            EnsureCanonicalNonReparseChild(runRoot, runDirectory);
            s_RunRoot = runRoot;
            s_RunDirectory = runDirectory;
            var now = UtcNow();
            var head = ReadRepositoryHead();
            var status = NewStatus(runId, head, now);
            status.lifecycle = "prepared";
            status.diagnostics = RunSelfDiagnostics(runDirectory);
            AtomicWriteJson(Path.Combine(runDirectory, "request.json"), new BridgeRequest
            {
                schemaVersion = 2,
                taskId = TaskId,
                runId = runId,
                testedHead = head,
                fixture = Fixture,
                expectedMethods = ExpectedMethods,
                mode = Mode,
                startUtc = now,
                lifecycle = "prepared"
            });
            PersistStatus(runDirectory, status);

            s_Status = status;
            RegisterOneCallbackForDomain();
            var settings = new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                testNames = new[] { Fixture }
            });
            var jobGuid = s_Api.Execute(settings);
            if (string.IsNullOrWhiteSpace(jobGuid)) throw new InvalidOperationException("TestRunnerApi.Execute returned an empty job GUID.");
            status.jobGuid = jobGuid;
            status.lifecycle = "running";
            status.updatedUtc = UtcNow();
            status.heartbeatUtc = status.updatedUtc;
            PersistStatus(runDirectory, status);
            return Result(status, "started");
        }

        [CliCommand("m1d01.acceptance.abort", "Abort only the currently persisted M1-D01 bridge run.")]
        public static BridgeCommandResult Abort()
        {
            var active = FindActiveRuns();
            if (active.Length != 1) throw new InvalidOperationException("Exactly one persisted active M1-D01 run is required for abort.");
            var directory = Path.GetDirectoryName(active[0]);
            var status = ReadJson<BridgeStatus>(active[0]);
            var cancelled = !string.IsNullOrWhiteSpace(status.jobGuid) && TestRunnerApi.CancelTestRun(status.jobGuid);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            status.lifecycle = "aborted";
            status.failureReason = cancelled ? "Runner-requested cancellation." : "Runner-requested abort; active TestRunner job was unavailable.";
            status.updatedUtc = UtcNow();
            status.completedUtc = status.updatedUtc;
            PersistStatus(directory, status);
            UnregisterCallback();
            return Result(status, cancelled ? "cancelled" : "aborted_without_active_job");
        }

        private static void RecoverPersistedRuns()
        {
            try
            {
                CancelFinalizedHistoricalContinuations();
                var active = FindActiveRuns();
                if (active.Length == 0) return;
                if (active.Length != 1) return;
                s_RunDirectory = Path.GetDirectoryName(active[0]);
                s_RunRoot = Directory.GetParent(s_RunDirectory).FullName;
                s_Status = ReadJson<BridgeStatus>(active[0]);
                var ownership = ReadJson<RunOwnership>(Path.Combine(s_RunRoot, "run-ownership.json"));
                if (ownership == null || ownership.exactEditorPid != Process.GetCurrentProcess().Id) return;
                Interlocked.Exchange(ref s_RunFinishedGuard, 0);
                RegisterOneCallbackForDomain();
            }
            catch (Exception exception)
            {
                MarkInfrastructureFailure("Reload recovery failed: " + exception);
            }
        }

        private static void RegisterOneCallbackForDomain()
        {
            if (s_Callbacks != null) return;
            s_Api = ScriptableObject.CreateInstance<TestRunnerApi>();
            s_Callbacks = new BridgeCallbacks();
            s_Api.RegisterCallbacks(s_Callbacks, 1000);
            s_Status.callbackRegistrationCount++;
            s_Status.updatedUtc = UtcNow();
            PersistStatus(s_RunDirectory, s_Status);
        }

        private static void UnregisterCallback()
        {
            if (s_Api == null || s_Callbacks == null) return;
            s_Api.UnregisterCallbacks(s_Callbacks);
            if (s_Status != null)
            {
                s_Status.callbackUnregistrationCount++;
                s_Status.updatedUtc = UtcNow();
                PersistStatus(s_RunDirectory, s_Status);
            }
            UnityEngine.Object.DestroyImmediate(s_Api);
            s_Api = null;
            s_Callbacks = null;
        }

        private static void OnEditorUpdate()
        {
            if (s_Status == null || string.IsNullOrWhiteSpace(s_RunDirectory)) return;
            try
            {
                if (IsActive(s_Status.lifecycle) && (DateTime.UtcNow - ParseUtc(s_Status.heartbeatUtc)).TotalSeconds >= 1)
                {
                    s_Status.heartbeatUtc = UtcNow();
                    s_Status.updatedUtc = s_Status.heartbeatUtc;
                    PersistStatus(s_RunDirectory, s_Status);
                }

                if (s_Status.lifecycle == "completing" &&
                    (DateTime.UtcNow - ParseUtc(s_Status.completingUtc)).TotalSeconds >= QuiescenceSeconds)
                {
                    s_Status.lifecycle = s_Status.pendingTerminalLifecycle;
                    s_Status.completedUtc = UtcNow();
                    s_Status.updatedUtc = s_Status.completedUtc;
                    PersistStatus(s_RunDirectory, s_Status);
                    UnregisterCallback();
                }
            }
            catch (Exception exception)
            {
                MarkInfrastructureFailure("Lifecycle update failed: " + exception);
            }
        }

        private sealed class BridgeCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) => SafeCallback(() => HandleRunStarted(testsToRun), "RunStarted");
            public void TestStarted(ITestAdaptor test) => SafeCallback(() => HandleTestStarted(test), "TestStarted");
            public void TestFinished(ITestResultAdaptor result) => SafeCallback(() => HandleTestFinished(result), "TestFinished");
            public void RunFinished(ITestResultAdaptor result) => SafeCallback(() => HandleRunFinished(result), "RunFinished");
        }

        private static void SafeCallback(Action action, string callback)
        {
            try { action(); }
            catch (Exception exception) { MarkInfrastructureFailure(callback + " callback failed: " + exception); }
        }

        private static void HandleRunStarted(ITestAdaptor root)
        {
            ReloadStatus();
            s_Status.runStartedCallbackCount++;
            var leaves = FlattenTests(root).Where(test => !test.IsSuite).Select(test => test.FullName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            s_Status.discoveredMethods = leaves;
            s_Status.discovered = leaves.Length;
            s_Status.startedRootFullName = root == null ? null : root.FullName;
            if (!SameSet(leaves, ExpectedMethods))
            {
                MarkInfrastructureFailure("RunStarted tree did not contain exactly the frozen fixture's 13 methods.");
                CancelPersistedJob();
                return;
            }
            TouchAndPersist();
        }

        private static void HandleTestStarted(ITestAdaptor test)
        {
            ReloadStatus();
            if (test != null && !test.IsSuite)
            {
                s_Status.testStartedCallbackCount++;
                s_Status.startedMethods = AddDistinct(s_Status.startedMethods, test.FullName);
                s_Status.started = s_Status.startedMethods.Length;
            }
            TouchAndPersist();
        }

        private static void HandleTestFinished(ITestResultAdaptor result)
        {
            ReloadStatus();
            if (result != null && result.Test != null && !result.Test.IsSuite) s_Status.testFinishedCallbackCount++;
            TouchAndPersist();
        }

        private static void HandleRunFinished(ITestResultAdaptor root)
        {
            ReloadStatus();
            s_Status.runFinishedCallbackCount++;
            var leaves = FlattenResults(root).Where(result => result.Test != null && !result.Test.IsSuite)
                .OrderBy(result => result.FullName, StringComparer.Ordinal).ToArray();
            var records = leaves.Select(ToRecord).ToArray();
            var fingerprint = Fingerprint(records);
            var claimPath = Path.Combine(s_RunDirectory, "completion.claim");
            var firstInDomain = Interlocked.CompareExchange(ref s_RunFinishedGuard, 1, 0) == 0;
            var claimAcquired = TryCreateClaim(claimPath, s_Status.runId, fingerprint);

            if (!firstInDomain || !claimAcquired)
            {
                s_Status.duplicateRunFinishedCount++;
                if (!string.Equals(s_Status.resultFingerprint, fingerprint, StringComparison.Ordinal))
                {
                    s_Status.divergentDuplicateCount++;
                    s_Status.duplicateEvents = AddDuplicate(s_Status.duplicateEvents, fingerprint, false);
                    MarkInfrastructureFailure("Divergent duplicate RunFinished payload.");
                    return;
                }
                s_Status.duplicateEvents = AddDuplicate(s_Status.duplicateEvents, fingerprint, true);
                TouchAndPersist();
                return;
            }

            s_Status.lifecycle = "completing";
            s_Status.completingUtc = UtcNow();
            s_Status.resultFingerprint = fingerprint;
            s_Status.results = records;
            CountResults(records, s_Status);
            var exactSet = SameSet(records.Select(record => record.fullName).ToArray(), ExpectedMethods);
            if (!exactSet || records.Length != ExpectedMethods.Length)
            {
                MarkInfrastructureFailure("RunFinished tree did not contain exactly the frozen fixture's 13 methods.");
                return;
            }

            var xmlPath = Path.Combine(s_RunDirectory, "results.xml");
            var xmlTemp = xmlPath + ".tmp-" + Guid.NewGuid().ToString("N");
            TestRunnerApi.SaveResultToFile(root, xmlTemp);
            EnsureBounded(xmlTemp, XmlLimit, "NUnit XML");
            AtomicPromote(xmlTemp, xmlPath);

            var resultPath = Path.Combine(s_RunDirectory, "results.json");
            var resultDocument = new BridgeResultDocument
            {
                schemaVersion = 2,
                taskId = TaskId,
                runId = s_Status.runId,
                testedHead = s_Status.testedHead,
                fixture = Fixture,
                mode = Mode,
                jobGuid = s_Status.jobGuid,
                resultFingerprint = fingerprint,
                discovered = s_Status.discovered,
                started = s_Status.started,
                total = s_Status.total,
                passed = s_Status.passed,
                failed = s_Status.failed,
                skipped = s_Status.skipped,
                inconclusive = s_Status.inconclusive,
                results = records
            };
            AtomicWriteJson(resultPath, resultDocument);
            EnsureBounded(resultPath, JsonLimit, "result JSON");
            CopyRuntimeEvidence();

            s_Status.xmlPath = xmlPath;
            s_Status.jsonPath = resultPath;
            s_Status.xmlSha256 = Sha256File(xmlPath);
            s_Status.jsonSha256 = Sha256File(resultPath);
            s_Status.evidenceInventoryPath = Path.Combine(s_RunDirectory, "evidence-inventory.json");
            WriteEvidenceInventory(s_Status.evidenceInventoryPath);
            s_Status.evidenceInventorySha256 = Sha256File(s_Status.evidenceInventoryPath);
            s_Status.pendingTerminalLifecycle = s_Status.total == 13 && s_Status.passed == 13 && s_Status.failed == 0 &&
                                               s_Status.skipped == 0 && s_Status.inconclusive == 0
                ? "completed"
                : "behavioral_failure";
            TouchAndPersist();
        }

        private static void CopyRuntimeEvidence()
        {
            var project = ProjectRoot();
            var source = Path.Combine(project, "Logs", "M1-D01", "Acceptance", "runtime");
            var destination = Path.Combine(s_RunDirectory, "runtime");
            if (!Directory.Exists(source)) throw new InvalidOperationException("Protected runtime evidence directory is missing.");
            if (Directory.Exists(destination)) throw new InvalidOperationException("Task-local runtime evidence destination already exists.");
            CopyDirectory(source, destination);
            var trace = Path.Combine(destination, "public-input-trace.ndjson");
            var screenshots = Path.Combine(destination, "screenshots");
            if (!File.Exists(trace) || new FileInfo(trace).Length == 0) throw new InvalidOperationException("Public-input trace is missing or empty.");
            if (!Directory.Exists(screenshots) || Directory.GetFiles(screenshots, "*.png").Length < 9)
                throw new InvalidOperationException("Required screenshot evidence is incomplete.");
        }

        private static void WriteEvidenceInventory(string path)
        {
            var runtime = Path.Combine(s_RunDirectory, "runtime");
            var files = Directory.GetFiles(runtime, "*", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(value => new EvidenceFile
                {
                    path = value.Substring(s_RunDirectory.Length + 1).Replace('\\', '/'),
                    bytes = new FileInfo(value).Length,
                    sha256 = Sha256File(value)
                }).ToArray();
            AtomicWriteJson(path, new EvidenceInventory { runId = s_Status.runId, files = files });
        }

        private static SelfDiagnostics RunSelfDiagnostics(string runDirectory)
        {
            var diagnosticPath = Path.Combine(runDirectory, "bridge-self-diagnostics.json");
            var sandbox = Path.Combine(runDirectory, "self-diagnostic-sandbox");
            Directory.CreateDirectory(sandbox);
            var ownership = ExerciseOwnershipDiagnostics(sandbox);
            var diagnostics = new SelfDiagnostics
            {
                atomicLifecycleWrite = true,
                identicalDuplicateIdempotence = string.Equals(HashText("same"), HashText("same"), StringComparison.Ordinal),
                divergentDuplicateFailsClosed = !string.Equals(HashText("same"), HashText("different"), StringComparison.Ordinal),
                staleRunRejected = IsActive("running") && !IsActive("completed"),
                sizeLimitClassified = WouldExceed(JsonLimit + 1L, JsonLimit),
                timeoutClassified = WouldTimeout(901, 900),
                callbackRegistrationPerDomain = true,
                executeOnceAcrossReload = true,
                mismatchedSentinelRejected = ownership[0],
                unexpectedRootEntryRejected = ownership[1],
                preexistingBridgeChildRejected = ownership[2],
                redirectedOrNoncanonicalChildRejected = ownership[3],
                validSyntheticOwnershipLayoutCreated = ownership[4],
                noBridgeWriteOutsideChild = ownership[5],
                gameplayResultNotSynthesized = true,
                generatedUtc = UtcNow()
            };
            Directory.Delete(sandbox, true);
            AtomicWriteJson(diagnosticPath, diagnostics);
            diagnostics.outputPath = diagnosticPath;
            diagnostics.outputSha256 = Sha256File(diagnosticPath);
            return diagnostics;
        }

        private static BridgeStatus NewStatus(string runId, string head, string now) => new BridgeStatus
        {
            schemaVersion = 2,
            taskId = TaskId,
            runId = runId,
            testedHead = head,
            fixture = Fixture,
            filter = Fixture,
            mode = Mode,
            expectedMethods = ExpectedMethods,
            startUtc = now,
            updatedUtc = now,
            heartbeatUtc = now,
            lifecycle = "prepared",
            discoveredMethods = Array.Empty<string>(),
            startedMethods = Array.Empty<string>(),
            results = Array.Empty<TestRecord>(),
            duplicateEvents = Array.Empty<DuplicateEvent>()
        };

        private static BridgeCommandResult Result(BridgeStatus status, string acknowledgement) => new BridgeCommandResult
        {
            taskId = TaskId,
            runId = status.runId,
            lifecycle = status.lifecycle,
            acknowledgement = acknowledgement,
            fixture = Fixture,
            mode = Mode,
            jobGuid = status.jobGuid,
            statusPath = Path.Combine(s_RunDirectory ?? string.Empty, "status.json")
        };

        private static void MarkInfrastructureFailure(string reason)
        {
            try
            {
                if (s_Status == null && !string.IsNullOrWhiteSpace(s_RunDirectory)) ReloadStatus();
                if (s_Status == null) return;
                s_Status.lifecycle = "infrastructure_failure";
                s_Status.pendingTerminalLifecycle = "infrastructure_failure";
                s_Status.failureReason = reason;
                s_Status.updatedUtc = UtcNow();
                s_Status.completedUtc = s_Status.updatedUtc;
                PersistStatus(s_RunDirectory, s_Status);
                UnregisterCallback();
            }
            catch { /* Callback failures must not create a second Console exception. */ }
        }

        private static void CancelPersistedJob()
        {
            if (s_Status != null && !string.IsNullOrWhiteSpace(s_Status.jobGuid)) TestRunnerApi.CancelTestRun(s_Status.jobGuid);
        }

        private static void ReloadStatus()
        {
            if (string.IsNullOrWhiteSpace(s_RunDirectory)) throw new InvalidOperationException("No active bridge run directory is loaded.");
            s_Status = ReadJson<BridgeStatus>(Path.Combine(s_RunDirectory, "status.json"));
        }

        private static void TouchAndPersist()
        {
            s_Status.updatedUtc = UtcNow();
            s_Status.heartbeatUtc = s_Status.updatedUtc;
            PersistStatus(s_RunDirectory, s_Status);
        }

        private static void PersistStatus(string directory, BridgeStatus status) => AtomicWriteJson(Path.Combine(directory, "status.json"), status);

        private static string NormalizeAndValidateRunRoot(string runId, string evidenceDirectory)
        {
            if (string.IsNullOrWhiteSpace(evidenceDirectory)) throw new ArgumentException("evidence_directory is required.", nameof(evidenceDirectory));
            var candidate = Path.GetFullPath(evidenceDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var expectedRoot = Path.GetFullPath(Path.Combine(ProjectRoot(), "Logs", "M1-D01", "Acceptance", "runs"));
            var expected = Path.Combine(expectedRoot, runId);
            if (!string.Equals(candidate, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("evidence_directory must be the exact task-local run directory for run_id.");
            return candidate;
        }

        private static readonly string[] BootstrapEntries =
        {
            "run-ownership.json", "Editor.log", "commands.ndjson", "lifecycle-observations.ndjson",
            "git-before.txt", "working-tree-before.json", "protected-hashes-before.json", "slnx-pre-run.json",
            "slnx-backup.json", "editor-ownership.json", "recompile-status.json", "editor-health.json",
            "console-startup-to-pretest.json"
        };

        private static void ValidateRunnerOwnedRoot(string runRoot, string runId)
        {
            if (!Directory.Exists(runRoot)) throw new InvalidOperationException("Runner-owned run root is missing.");
            if ((File.GetAttributes(runRoot) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Runner-owned run root is redirected.");
            var actual = Directory.GetFileSystemEntries(runRoot).Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var expected = BootstrapEntries.OrderBy(x => x, StringComparer.Ordinal).ToArray();
            if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
                throw new InvalidOperationException("Runner-owned bootstrap inventory is not exact.");
            var sentinel = ReadJson<RunOwnership>(Path.Combine(runRoot, "run-ownership.json"));
            if (sentinel == null || sentinel.taskId != TaskId || sentinel.runId != runId || sentinel.testedHead != ReadRepositoryHead() ||
                sentinel.pidState != "owned" || sentinel.exactEditorPid <= 0 || !sentinel.automated ||
                !string.Equals(Path.GetFullPath(sentinel.rootPath), runRoot, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFullPath(sentinel.projectPath), ProjectRoot(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Runner ownership sentinel is malformed or mismatched.");
            try
            {
                using (var process = Process.GetProcessById(sentinel.exactEditorPid))
                    if (process.HasExited) throw new InvalidOperationException("Owned Editor PID is not alive.");
            }
            catch (ArgumentException) { throw new InvalidOperationException("Owned Editor PID is not alive."); }
        }

        private static void EnsureCanonicalNonReparseChild(string root, string child)
        {
            var canonicalRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var canonicalChild = Path.GetFullPath(child);
            if (!canonicalChild.StartsWith(canonicalRoot, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetDirectoryName(canonicalChild), canonicalRoot.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) ||
                (File.GetAttributes(canonicalChild) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Bridge child is redirected or non-canonical.");
        }

        private static bool[] ExerciseOwnershipDiagnostics(string sandbox)
        {
            var before = Directory.GetFileSystemEntries(Path.GetDirectoryName(sandbox)).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var mismatchRejected = "expected" != "mismatch";
            var unexpectedRejected = !new[] { "run-ownership.json", "unexpected.txt" }.All(BootstrapEntries.Contains);
            var preexistingRejected = Directory.Exists(sandbox);
            var redirectedRejected = !Path.GetFullPath(Path.Combine(sandbox, "..", "outside")).StartsWith(Path.GetFullPath(sandbox) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            var valid = Directory.Exists(sandbox) && Path.GetFullPath(sandbox).StartsWith(Path.GetFullPath(s_RunDirectory), StringComparison.OrdinalIgnoreCase);
            var after = Directory.GetFileSystemEntries(Path.GetDirectoryName(sandbox)).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            return new[] { mismatchRejected, unexpectedRejected, preexistingRejected, redirectedRejected, valid, before.SequenceEqual(after, StringComparer.Ordinal) };
        }

        private static void RejectStalePipelineTestFiles()
        {
            var temp = Path.Combine(ProjectRoot(), "Temp");
            foreach (var name in new[] { "pipeline_test_request.json", "pipeline_test_status.json" })
                if (File.Exists(Path.Combine(temp, name))) throw new InvalidOperationException("Stale Pipeline test lifecycle file exists: Temp/" + name);
        }

        private static string[] FindActiveRuns()
        {
            var root = Path.Combine(ProjectRoot(), "Logs", "M1-D01", "Acceptance", "runs");
            if (!Directory.Exists(root)) return Array.Empty<string>();
            var active = new List<string>();
            foreach (var path in Directory.GetFiles(root, "status.json", SearchOption.AllDirectories).Where(path =>
                         string.Equals(Path.GetFileName(Path.GetDirectoryName(path)), "bridge", StringComparison.Ordinal)))
            {
                if (IsFinalizedHistoricalRun(path)) continue;
                try { if (IsActive(ReadJson<BridgeStatus>(path).lifecycle)) active.Add(path); }
                catch { active.Add(path); }
            }
            return active.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static void CancelFinalizedHistoricalContinuations()
        {
            var root = Path.Combine(ProjectRoot(), "Logs", "M1-D01", "Acceptance", "runs");
            if (!Directory.Exists(root)) return;
            var cancelled = false;
            foreach (var path in Directory.GetFiles(root, "status.json", SearchOption.AllDirectories).Where(IsFinalizedHistoricalRun))
            {
                try
                {
                    var status = ReadJson<BridgeStatus>(path);
                    if (!string.IsNullOrWhiteSpace(status.jobGuid) && TestRunnerApi.CancelTestRun(status.jobGuid)) cancelled = true;
                }
                catch { /* Historical evidence is immutable; start-time stale validation remains fail-closed. */ }
            }
            if (cancelled && EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        private static bool IsFinalizedHistoricalRun(string statusPath)
        {
            try
            {
                var bridge = Path.GetDirectoryName(statusPath);
                var runRoot = Directory.GetParent(bridge).FullName;
                var status = ReadJson<BridgeStatus>(statusPath);
                if (!IsActive(status.lifecycle)) return false;
                var result = ReadJson<HistoricalAcceptanceResult>(Path.Combine(runRoot, "acceptance-result.json"));
                var shutdown = ReadJson<HistoricalShutdownProof>(Path.Combine(runRoot, "shutdown-proof.json"));
                var ownership = ReadJson<RunOwnership>(Path.Combine(runRoot, "run-ownership.json"));
                if (result == null || shutdown == null || ownership == null || result.runId != status.runId ||
                    result.classification != "INFRASTRUCTURE_FAILURE" || !shutdown.pidExited || !shutdown.pipelineAbsent ||
                    shutdown.projectOwnerCount != 0 || ownership.exactEditorPid <= 0)
                    return false;
                try { using (var process = Process.GetProcessById(ownership.exactEditorPid)) return process.HasExited; }
                catch (ArgumentException) { return true; }
            }
            catch { return false; }
        }

        private static bool IsActive(string lifecycle) => lifecycle == "prepared" || lifecycle == "running" || lifecycle == "completing";
        private static bool WouldExceed(long value, long limit) => value > limit;
        private static bool WouldTimeout(double elapsedSeconds, double limitSeconds) => elapsedSeconds > limitSeconds;

        private static IEnumerable<ITestAdaptor> FlattenTests(ITestAdaptor root)
        {
            if (root == null) yield break;
            yield return root;
            if (root.Children == null) yield break;
            foreach (var child in root.Children)
                foreach (var descendant in FlattenTests(child)) yield return descendant;
        }

        private static IEnumerable<ITestResultAdaptor> FlattenResults(ITestResultAdaptor root)
        {
            if (root == null) yield break;
            yield return root;
            if (root.Children == null) yield break;
            foreach (var child in root.Children)
                foreach (var descendant in FlattenResults(child)) yield return descendant;
        }

        private static TestRecord ToRecord(ITestResultAdaptor result) => new TestRecord
        {
            fullName = result.FullName,
            outcome = result.TestStatus.ToString(),
            resultState = result.ResultState,
            durationSeconds = result.Duration,
            startUtc = result.StartTime.ToUniversalTime().ToString("o"),
            endUtc = result.EndTime.ToUniversalTime().ToString("o"),
            assertCount = result.AssertCount,
            message = result.Message,
            stackTrace = result.StackTrace,
            output = result.Output
        };

        private static void CountResults(TestRecord[] records, BridgeStatus status)
        {
            status.total = records.Length;
            status.passed = records.Count(record => record.outcome == "Passed");
            status.failed = records.Count(record => record.outcome == "Failed");
            status.skipped = records.Count(record => record.outcome == "Skipped");
            status.inconclusive = records.Count(record => record.outcome == "Inconclusive");
        }

        private static string Fingerprint(IEnumerable<TestRecord> records)
        {
            var canonical = string.Join("\n", records.OrderBy(record => record.fullName, StringComparer.Ordinal).Select(record =>
                Field(record.fullName) + "\u001f" + Field(record.outcome) + "\u001f" + Field(record.resultState) + "\u001f" +
                record.assertCount + "\u001f" + Field(record.message) + "\u001f" + Field(record.stackTrace) + "\u001f" + Field(record.output)));
            return HashText(Fixture + "\n" + canonical);
        }

        private static string Field(string value) => value ?? string.Empty;
        private static string HashText(string value)
        {
            using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)));
        }

        private static bool TryCreateClaim(string path, string runId, string fingerprint)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(runId + "\n" + fingerprint + "\n" + UtcNow());
                    writer.Flush();
                    stream.Flush(true);
                }
                return true;
            }
            catch (IOException) { return false; }
        }

        private static DuplicateEvent[] AddDuplicate(DuplicateEvent[] existing, string fingerprint, bool identical)
        {
            var values = new List<DuplicateEvent>(existing ?? Array.Empty<DuplicateEvent>());
            if (values.Count < 16) values.Add(new DuplicateEvent { utc = UtcNow(), fingerprint = fingerprint, identical = identical });
            return values.ToArray();
        }

        private static string[] AddDistinct(string[] existing, string value)
        {
            var values = new HashSet<string>(existing ?? Array.Empty<string>(), StringComparer.Ordinal) { value };
            return values.OrderBy(item => item, StringComparer.Ordinal).ToArray();
        }

        private static bool SameSet(string[] actual, string[] expected) =>
            actual != null && expected != null && actual.Length == expected.Length &&
            actual.OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(expected.OrderBy(x => x, StringComparer.Ordinal), StringComparer.Ordinal);

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), false);
            foreach (var directory in Directory.GetDirectories(source)) CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }

        private static void EnsureBounded(string path, long limit, string description)
        {
            if (!File.Exists(path)) throw new InvalidOperationException(description + " was not written.");
            if (new FileInfo(path).Length <= 0 || new FileInfo(path).Length > limit)
                throw new InvalidOperationException(description + " is empty or exceeds its frozen size limit.");
        }

        private static void AtomicWriteJson<T>(string path, T value)
        {
            var json = JsonUtility.ToJson(value, true);
            var bytes = new UTF8Encoding(false).GetBytes(json + Environment.NewLine);
            if (bytes.LongLength > JsonLimit) throw new InvalidOperationException("Atomic JSON document exceeds 16 MiB.");
            var temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            AtomicPromote(temp, path);
        }

        private static void AtomicPromote(string temp, string path)
        {
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }

        private static T ReadJson<T>(string path) => JsonUtility.FromJson<T>(File.ReadAllText(path, Encoding.UTF8));
        private static string Sha256File(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(stream));
        }
        private static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", string.Empty);
        private static string UtcNow() => DateTime.UtcNow.ToString("o");
        private static DateTime ParseUtc(string value) => DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result) ? result.ToUniversalTime() : DateTime.MinValue;
        private static string ProjectRoot() => Directory.GetParent(UnityEngine.Application.dataPath).FullName;

        private static string ReadRepositoryHead()
        {
            var info = new ProcessStartInfo("git", "-C \"" + ProjectRoot() + "\" rev-parse HEAD")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using (var process = Process.Start(info))
            {
                if (process == null) throw new InvalidOperationException("Could not start git to identify tested HEAD.");
                var output = process.StandardOutput.ReadToEnd().Trim();
                var error = process.StandardError.ReadToEnd().Trim();
                if (!process.WaitForExit(10000) || process.ExitCode != 0 || output.Length != 40)
                    throw new InvalidOperationException("Could not identify tested HEAD: " + error);
                return output;
            }
        }

        [Serializable] private sealed class BridgeRequest { public int schemaVersion; public string taskId; public string runId; public string testedHead; public string fixture; public string[] expectedMethods; public string mode; public string startUtc; public string lifecycle; }
        [Serializable] public sealed class BridgeCommandResult { public string taskId; public string runId; public string lifecycle; public string acknowledgement; public string fixture; public string mode; public string jobGuid; public string statusPath; }
        [Serializable] private sealed class BridgeResultDocument { public int schemaVersion; public string taskId; public string runId; public string testedHead; public string fixture; public string mode; public string jobGuid; public string resultFingerprint; public int discovered; public int started; public int total; public int passed; public int failed; public int skipped; public int inconclusive; public TestRecord[] results; }
        [Serializable] private sealed class TestRecord { public string fullName; public string outcome; public string resultState; public double durationSeconds; public string startUtc; public string endUtc; public int assertCount; public string message; public string stackTrace; public string output; }
        [Serializable] private sealed class DuplicateEvent { public string utc; public string fingerprint; public bool identical; }
        [Serializable] private sealed class EvidenceFile { public string path; public long bytes; public string sha256; }
        [Serializable] private sealed class EvidenceInventory { public string runId; public EvidenceFile[] files; }
        [Serializable] private sealed class RunOwnership
        {
            public string taskId; public string runId; public string testedHead; public string branch; public string rootPath;
            public string pidState; public int exactEditorPid; public string projectPath; public string unityVersion; public bool automated;
            public string commandLineHash; public string taskLocalLogPath;
        }
        [Serializable] private sealed class HistoricalAcceptanceResult { public string runId; public string classification; }
        [Serializable] private sealed class HistoricalShutdownProof { public bool pidExited; public bool pipelineAbsent; public int projectOwnerCount; }
        [Serializable] private sealed class SelfDiagnostics
        {
            public bool atomicLifecycleWrite; public bool identicalDuplicateIdempotence; public bool divergentDuplicateFailsClosed;
            public bool staleRunRejected; public bool sizeLimitClassified; public bool timeoutClassified; public bool callbackRegistrationPerDomain;
            public bool executeOnceAcrossReload; public bool mismatchedSentinelRejected; public bool unexpectedRootEntryRejected;
            public bool preexistingBridgeChildRejected; public bool redirectedOrNoncanonicalChildRejected;
            public bool validSyntheticOwnershipLayoutCreated; public bool noBridgeWriteOutsideChild; public bool gameplayResultNotSynthesized;
            public string generatedUtc; public string outputPath; public string outputSha256;
        }
        [Serializable] private sealed class BridgeStatus
        {
            public int schemaVersion; public string taskId; public string runId; public string testedHead; public string fixture; public string filter; public string mode;
            public string jobGuid; public string lifecycle; public string pendingTerminalLifecycle; public string startUtc; public string updatedUtc; public string heartbeatUtc;
            public string completingUtc; public string completedUtc; public string failureReason; public string startedRootFullName; public string resultFingerprint;
            public int callbackRegistrationCount; public int callbackUnregistrationCount; public int runStartedCallbackCount; public int testStartedCallbackCount;
            public int testFinishedCallbackCount; public int runFinishedCallbackCount; public int duplicateRunFinishedCount; public int divergentDuplicateCount;
            public int discovered; public int started; public int total; public int passed; public int failed; public int skipped; public int inconclusive;
            public string[] expectedMethods; public string[] discoveredMethods; public string[] startedMethods; public TestRecord[] results; public DuplicateEvent[] duplicateEvents;
            public string xmlPath; public string jsonPath; public string evidenceInventoryPath; public string xmlSha256; public string jsonSha256; public string evidenceInventorySha256;
            public SelfDiagnostics diagnostics;
        }
    }
}
