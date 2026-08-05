# M1-D01 — Restore the Real DD-28 Ordinary-Runtime Card Drag Path

**Status:** FROZEN; Builder implementation is present and behaviorally green; **BLOCKED on protected acceptance infrastructure recovery** until Sections 15–17 are implemented and the recovered gate passes.
**Authority baseline:** `6e910f7811dc3b7f07aa5d30b7ca574d561b45a6` (`fix/m1-dd28-runtime-drag`; source baseline and merge-base both verified at planning time).  
**Planning worktree:** `D:\Dev\Projects\Bloomdrawn-Unity-M1D01`  
**Acceptance manifest:** `acceptance/manifests/M1-D01-dd28-runtime-drag.yaml`

**Owner amendments:** The release-below, protected-runner dirty-state, workforce-v4, repair-accounting, Section 15 acceptance-infrastructure recovery, Section 16 narrow recovery-continuation, and Section 17 restoration-helper corrections were approved after implementation exposed over-constrained acceptance encoding, obsolete workforce requirements, a repeatable Pipeline result-collector failure, two impossible ownership assumptions in the first recovered run, and one invalid `File.Replace` rollback argument. None changes DD-28, the protected behavioral criteria, or the existing Builder implementation.

## 1. Objective and Player-Visible Outcome

Restore the already-approved M1H/DD-28 tactile card interaction in the committed `Assets/Scenes/CombatStage.unity` ordinary runtime. Opening that scene and pressing Play must automatically bootstrap the fixture combat; an actual runtime-spawned card must receive real Unity EventSystem/Input System pointer input, rise on hover/focus, follow an upward drag, arm and disarm at the responsive Play Area boundary, cancel without gameplay mutation, cast a target-complete card once, or stage an explicit-target card until a legal enemy is selected.

This is an M1 runtime repair using generic non-production presentation. It is not M2 content/preview work or M9 interaction polish.

## 2. Authority and Baseline

### Authoritative requirements

Read and obey, in authority order:

1. `AGENTS.md`, especially §§1–12 and the combat hand invariants.
2. `docs/DESIGN.md` §§5.6–5.8, 6.5, 8.3–8.5, 8.9–8.10, and 15.1–15.5.
3. `plans/design-decisions.md` DD-27 and **DD-28 in full** (lines 1664–1747 at the planning baseline).
4. `plans/implementation_plan.md` §§2.2, 2.6, Task M1H, Task M1J, and M1 exit criteria.
5. `plans/tasks/M1H-card-fan-drag-targeting.md`, `plans/tasks/M1J-golden-replay-playmode-gate.md`, and `plans/tasks/M1R-runtime-integration-recovery.md`, each in full.
6. `.agents/skills/bloomdrawn-unity/SKILL.md` and its Unity CLI/Pipeline and verification references.
7. This frozen packet and its acceptance manifest, which refine only this bounded repair and do not override the sources above.

Pinned toolchain at planning time: Unity `6000.5.5f1`, Input System `1.19.0`, uGUI `2.5.0`, Unity Test Framework `1.7.0`, Pipeline `0.4.0-exp.1`, Windows 11.

The Builder must verify that HEAD descends from `6e910f7`, identify all owner-owned changes, and stop if the implementation baseline has diverged in the relevant runtime path. No reset, clean, checkout, rebase, branch change, staging, or commit is authorized.

## 3. Resolved Owner Decisions and Retained Prerequisite

### Resolved decisions

- **Preserve current non-drag compatibility for M1-D01.** A click or number key on a target-complete card continues to submit once; a click or number key on an explicit-target card continues to stage target selection; selecting a legal enemy submits once; Escape/right-click continues to cancel. M1-D01 proves no regression in those paths while restoring drag.
- This preservation is **not** approval of the current one-step path as final UX. `docs/DESIGN.md` §8.5 describes click/select → inspect → Play Area or Enter/Space confirmation. Strict alignment is a separate follow-up discrepancy requiring its own scoped task or decision; do not implement it here.
- **Protected executable acceptance is mandatory.** The manifest alone is not sufficient for this first controlled trial.
- **Disarmed release is complete when drag commitment ends and the authoritative hand presentation is restored.** After normal EventSystem processing, the global interaction state may be `Resting` or `Hovered`. `Hovered` is valid only when the unchanged public pointer genuinely raycasts a real restored runtime card and the hovered card identity matches that real EventSystem raycast target. Product code must not suppress legitimate hover merely to force `Resting`.
- **Workforce v4 owns validation through three layers:** protected executable acceptance during implementation, owner-approved `bloom-sol-specialist` escalation when the repair policy permits it, and independent final `bloom-auditor`. The retired advisory Checker is not part of M1-D01 validation or completion.

### Retained prerequisite (gating, not Builder work)

Before invoking the Builder, the project owner or an external workflow must add and freeze:

- `Assets/Bloomdrawn/Tests/Acceptance/M1D01RuntimeDragAcceptanceTests.cs` and its Unity `.meta`, inheriting the existing Play Mode test assembly unless the owner separately approves another protected harness architecture;
- the test identity `Bloomdrawn.Tests.PlayMode.Acceptance.M1D01RuntimeDragAcceptanceTests` (individual method names may vary but must map unambiguously to every manifest criterion);
- no product implementation, replacement scene/UI, session injection, or expected-result fixture inside that protected file.

The Builder is denied edits to `Assets/Bloomdrawn/Tests/Acceptance/**` and `acceptance/**`. Record the harness commit/hash in the Builder handoff. If the harness is absent, mutable by the Builder, fails against the known baseline for a reason unrelated to the reported drag defect, or requires a contract change, **do not invoke or continue the Builder**; return to the owner/Planner.

The protected C05 test currently asserts that disarmed release must end in exactly `CardInteractionState.Resting`. That assertion is stricter than the approved player-visible contract because normal EventSystem processing may immediately and legitimately hover a restored runtime card beneath the unchanged pointer. The protected Acceptance Engineer must correct C05 to accept `Resting` or a raycast-proven `Hovered` state while retaining every mutation, view-identity, fan-restoration, and public-input assertion. This is correction of erroneous acceptance encoding, not weakening of DD-28. The Builder must not edit the protected test or runner.

## 4. Verified Investigation Evidence

The following are repository/package facts, not an instruction to apply a predetermined patch:

1. `CombatCardView` is runtime-created by `CombatHudView.RebuildHand` and currently implements `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`, and `IPointerClickHandler`. It does not implement runtime hover/focus interfaces; `CardInteractionController.Hover` has no ordinary-runtime caller. Therefore the required real hover/focus rise is presently unwired.
2. `CardDragLayer.IsAbovePlayArea` currently tests `playArea.rect.Contains(local)`. This is bounded rectangle containment, not the DD-28 upward threshold invariant. A card moved beyond the rectangle's upper edge (or outside its horizontal span) becomes disarmed even though it remains above the threshold. Returning below versus continuing above is therefore not represented correctly.
3. On a disarmed release, `ReleaseCardDrag` calls `interaction.Release()` and then `Refresh()`. `Refresh()` rebuilds the authoritative hand while the original dragged view remains parented to the drag layer; that path does not clear or reintegrate the detached view. This creates a concrete duplicate/stale-view risk. Explicit-target staging similarly rebuilds a hand copy while retaining the detached staged view.
4. Existing `CombatStageOrdinaryLaunchPlayModeTests` uses a virtual keyboard for card choice and virtual mouse clicks for targets/End Turn; it never presses, moves beyond the EventSystem drag threshold, and releases on a runtime card. `M1CombatInteractionGatePlayModeTests` and `HandInteractionTests` call the interaction controller directly. No existing test proves a real EventSystem card drag.
5. The pinned Input System `1.19.0` package source (`InputSystemUIInputModule.ProcessPointerButton`) sets `pointerDrag` independently through `GetEventHandler<IDragHandler>`, falls back from a missing pointer-down handler to the click handler for `pointerPress`, and dispatches begin/drag/end to the stored drag object. Current Unity source/documentation also establishes that reparenting an active drag object does not by itself lose that stored reference and that a null `pressEventCamera` is correct for a Screen Space Overlay Canvas.
6. Consequently, **missing `IPointerDownHandler` is not an approved or package-supported root cause of drag dispatch failure by itself**. It may be introduced if the Builder needs an explicit pointer-down lifecycle to satisfy DD-28, but “added the interface” is not sufficient diagnosis or acceptance evidence.
7. Planning inspection verified one committed Canvas, EventSystem with `InputSystemUIInputModule`, Screen Space Overlay canvas, runtime bootstrap, hand container, Play Area, and drag layer wiring. The scene automatically creates five runtime card views in ordinary Play Mode.
8. An automated Editor (`6000.5.5f1`, `-automated`, Pipeline ready) was available during planning. A bounded Pipeline eval confirmed ordinary Play Mode bootstrap and five runtime cards. The eval route did not provide trustworthy cross-frame virtual-pointer evidence and is not acceptance. The project owner's manual observation of absent tactile drag remains the player-facing reproduction.

### Remaining uncertainty the Builder must close

- The exact first failed public-input event/transition in the pinned runtime has not yet been captured. It may coexist with the verified hover, threshold, and view-lifecycle defects above.
- Whether explicit pointer-down/up handlers are useful for the corrected lifecycle is Builder discretion; their absence is not presumed causal.
- The smallest safe view-reconciliation strategy is not prescribed. It must preserve one active runtime view per authoritative hand card, a visibly staged explicit-target card, and deterministic resting layout.

Before the first product edit, the Builder must reproduce the defect through the protected harness or an equivalent temporary public-input diagnostic and record: pointer raycast target, pointer-down/begin-drag/drag/end-drag delivery, interaction-state transitions, card parent/instance identity, pointer and local coordinates, and arming predicate. Temporary instrumentation must be removed before handoff.

## 5. Scope

### In scope

- Real EventSystem/Input System pointer delivery to runtime-spawned `CombatCardView` instances.
- Runtime hover/focus rise and restoration without authoritative hand reordering.
- One UI-only drag session, coherent Screen Space Overlay coordinate conversion, and stable drag-layer reparenting.
- Responsive upward threshold semantics, visible non-colour-only arm cue, downward disarm, and release behavior.
- Mutation-free disarmed release and explicit-target cancellation.
- Exactly-once target-complete submission and explicit-target stage/highlight/select submission.
- Reconciliation of runtime card views so repeated cycles cannot create drift, duplicates, stale references, destroyed active views, jumps, or off-screen cards.
- New Builder-owned developer tests that use real public pointer/keyboard input against the committed ordinary scene.
- A scene/authoring/validator correction only if runtime evidence proves scene composition is part of the defect.

### Explicitly excluded

- Engine rules, command shape, target legality, Mana/pile/RNG/event rules, replay/golden data, or application session semantics.
- M2 mechanics, authoritative previews, formulas in UI, production cards/art/content, or registry/schema changes.
- M9 visual polish, new animation/VFX/audio systems, controller redesign, touch redesign, or strict §8.5 click/keyboard alignment.
- Persistence/save changes, packages, project settings, assembly-direction changes, new dependencies, scene reconstruction, alternate combat scenes, replacement card UI, or fixture changes.
- Refactoring unrelated presentation/runtime code or changing existing test expectations.

## 6. Contracts and Architecture Boundaries

### Contracts consumed

- Existing automatic `CombatStageRuntimeBootstrap` → `CombatRuntimeFlow` → `CombatSession` path.
- Existing `CardInteractionController`, `ICompleteCardCommandSink`, complete `PlayCardCommand`, authoritative state/events, and presentation-token sequence.
- Existing runtime fixture artifact and stable runtime card/owner/enemy IDs.
- Existing uGUI Canvas, EventSystem/Input System module, hand, drag layer, responsive Play Area, independent enemy targets, and ordinary click/keyboard routes.

### Contract introduced/restored

- One public-pointer gesture owns one runtime card view from pointer-down/drag start through cancel, accepted cast, target staging, or rejection resync.
- `armed` means the pointer/card has crossed upward past the responsive Play Area threshold and remains above it; moving below disarms. It is not limited to being inside a bounded rectangle.
- Hover, focus, drag, armed/disarmed, and target staging remain presentation-only. Only one complete command reaching the existing sink may mutate authoritative state.
- Runtime view reconciliation preserves stable card identity and exactly one active visual representation per authoritative hand card, except that a staged card may be detached from the fan but must not also have a duplicate interactive hand view.
- Successful disarmed release returns the dragged card to its recalculated authoritative fan position, leaves exactly one active view for every authoritative hand card, accepts no command, and changes no authoritative Mana, pile, gameplay-event, canonical-state, resource, or named-RNG-stream state. The interaction must no longer be dragging, armed, disarmed, or in target selection. A final global state of `Resting` is valid; `Hovered` is also valid only when the unchanged public pointer genuinely raycasts the matching restored runtime card.

No new public gameplay command, engine/content/application schema, persistence contract, assembly dependency, or production content is permitted.

## 7. Allowed Implementation Areas

The Builder may modify only the smallest necessary subset of:

- `Assets/Bloomdrawn/Presentation/CombatCardView.cs`
- `Assets/Bloomdrawn/Presentation/CardDragLayer.cs`
- `Assets/Bloomdrawn/Presentation/CombatStageRuntimeBootstrap.cs`
- `Assets/Bloomdrawn/Presentation/CombatHudView.cs`
- `Assets/Bloomdrawn/Presentation/HandInteraction.cs`
- new task-specific developer test source and `.meta` under `Assets/Bloomdrawn/Tests/PlayMode/`, **excluding** `Assets/Bloomdrawn/Tests/Acceptance/**`

Conditionally allowed only with runtime evidence and explanation in the handoff:

- `Assets/Bloomdrawn/Editor/Tooling/CombatStageAuthoring.cs`
- `Assets/Bloomdrawn/Editor/Tooling/CombatStageSceneValidator.cs`
- `Assets/Scenes/CombatStage.unity` and its existing `.meta`, authored through `CombatStageAuthoring`/Unity-aware tooling rather than direct YAML editing.

Any other product file requires owner/Planner approval before edit.

### Protected and forbidden

- `AGENTS.md`, `.agents/**`, `.opencode/**`, `opencode.json*`
- `agent-tasks/**`, `acceptance/**`, `Assets/Bloomdrawn/Tests/Acceptance/**`
- `docs/**`, `plans/**`, `worklog.md`
- all existing test files and assertions
- `Assets/Bloomdrawn/Engine/**`, `Content/**`, `Application/**`, `RuntimeData/**`
- `Packages/**`, `ProjectSettings/**`, assembly definitions, solution files
- all other scenes, prefabs, production assets, fixture artifacts, and generated/cache directories

## 8. Builder Discretion

The Builder owns private implementation details such as pointer-handler composition, hover interpolation, state/view reconciliation helpers, drag-session bookkeeping, and test-helper organization, provided all frozen contracts pass. The Builder may retain the current controller shape or refine presentation-private APIs within the Presentation assembly.

Do not prescribe or assume `IPointerDownHandler` as the fix. Do not hardcode card/enemy fixture IDs, raw desktop Y thresholds, one reference resolution, or named production behavior. Do not add a second Canvas/EventSystem/input module or move authority into scene objects.

## 9. Ordinary Runtime Entrypoint and Black-Box Acceptance

The only acceptance entrypoint is:

`committed Assets/Scenes/CombatStage.unity` → open scene → ordinary Play Mode → automatic runtime bootstrap → runtime-spawned card view → public Unity EventSystem/Input System events.

The protected harness must use virtual pointer/keyboard devices through public Input System state events and allow normal PlayerLoop/EventSystem processing. It may read public/runtime state and view properties for assertions. It must not:

- call `CardInteractionController` methods;
- call Unity event-handler methods (`OnPointerDown`, `OnBeginDrag`, `OnDrag`, `OnEndDrag`, click handlers) directly;
- call `BeginCardDrag`, `UpdateCardDrag`, `ReleaseCardDrag`, `ClickCard`, `SelectEnemy`, or equivalent controller/bootstrap shortcuts;
- create replacement card UI, reconstruct/repair the scene, add a test EventSystem, or manually bind a session;
- call `CombatSession.Submit`, `CombatRuntimeFlow.Play`, or any engine submission method;
- invoke `bloom.load-combat-fixture`, inject/reset a fixture session, or manually advance/complete the presenter;
- rely on direct transform mutation as a substitute for pointer movement;
- weaken, skip, rewrite, or conditionalize an approved assertion.

The manifest contains the complete black-box criteria. Every criterion is mandatory.

### Protected runner dirty-state policy

Protected acceptance must be executable against in-progress Builder work and must never require product files to be committed merely to run the gate.

For every run, the owner-controlled runner must:

- record the complete pre-run dirty path set and SHA-256 hashes;
- verify all protected acceptance hashes before and after execution;
- allow only the owner-declared `Bloomdrawn-Unity.slnx` exception, protected acceptance maintenance owned outside the Builder, and the exact active M1-D01 implementation path allowlist;
- for the current Builder iteration, recognize exactly these product paths as the active implementation allowlist: `Assets/Bloomdrawn/Presentation/CombatCardView.cs`, `Assets/Bloomdrawn/Presentation/CardDragLayer.cs`, `Assets/Bloomdrawn/Presentation/CombatHudView.cs`, `Assets/Bloomdrawn/Presentation/CombatStageRuntimeBootstrap.cs`, and `Assets/Bloomdrawn/Presentation/HandInteraction.cs`;
- fail closed on any dirty path outside those declared categories;
- record and compare complete pre-run and post-run Git state and hashes so Unity-created files, newly dirty files, or incidental modifications still fail closed.

The runner may not infer permission from a broad directory alone. Any later task-authorized developer test or conditionally allowed scene/tooling path must be named in the exact run allowlist after its Section 7 precondition is satisfied. Protected acceptance maintenance remains external to Builder ownership and must remain hash-controlled.

## 10. Developer Validation and Evidence

### Approved Editor/Pipeline workflow

1. Discover the installed CLI with `unity --help`, `unity command --help`, `unity pipeline list`, and command discovery; do not rely on remembered syntax.
2. Run `Tools\get-unity-editor-state.ps1 -RequireAutomated`. If no Editor exists, launch only with `Tools\open-automated-editor.ps1` and poll for at most 180 seconds.
3. If a project Editor is open without `-automated`, do not attach, terminate, or restart it; stop for owner action.
4. Before live work and final evidence, report exact project/version/PID, `automated=true`, Pipeline availability, `editorReady=true`, `compilation.active=false`, and `compileErrors=0` (or explicit failure fields).
5. Use Pipeline/live eval only for bounded inspection/diagnosis. Do not use `bloom.load-combat-fixture` for acceptance or hide gameplay/test logic in eval.
6. Poll startup/import/compilation/tests with bounded timeouts and stop with the last observed state on timeout.

### Required validation commands

Run from the repository root, using installed discovered syntax:

1. focused new Builder developer Play Mode test(s);
2. protected `M1D01RuntimeDragAcceptanceTests` through the Section 15 hardened task-local bridge (Builder may run the owner-frozen runner but may not edit the test, bridge, runner, contract, or lock);
3. `Tools\validate.ps1`;
4. complete Edit Mode suite to `Logs\M1-D01-EditMode-results.xml`;
5. complete Play Mode suite to `Logs\M1-D01-PlayMode-results.xml`;
6. `unity command . bloom.health` (or discovered project-path equivalent);
7. `unity command . bloom.validate-content`;
8. `unity command . bloom.validate-combat-stage`;
9. `unity command . bloom.scene-summary` with committed `CombatStage` active;
10. `unity command . bloom.validate-combat-layout` only as supplementary layout evidence, never as fixture injection or drag acceptance;
11. `Tools\build-smoke.ps1`;
12. `git diff --check`, final `git status --short --branch`, and diff against `6e910f7` plus the recorded protected-harness commit.

Do not invoke `bloom.load-combat-fixture` in the acceptance run.

### Builder evidence

Place transient Builder evidence under `Logs/M1-D01/Builder/` (not source-controlled): focused/full XML, command outputs, compile/Console state, public-input diagnostic trace, state/event/RNG deltas, and screenshots at 1920×1080, 1920×1200, and 3440×1440 showing at least resting/hover, armed non-colour cue, and explicit-target staging/highlights. Label each screenshot with resolution, state, and tested commit in an adjacent index/log; do not alter gameplay UI solely to label evidence.

## 11. Workforce-v4 Validation Ownership

M1-D01 uses the installed workforce-v4 validation model:

1. the protected executable acceptance gate runs during Builder implementation against the exact in-progress implementation allowlist;
2. after two materially distinct failed attempts on one genuine protected blocker, the Builder may request owner approval for `bloom-sol-specialist` under Section 12;
3. the independent read-only `bloom-auditor` performs final verification and is the only role that may issue `PASS`.

No advisory Checker invocation, verdict, artifact, or finding disposition is required.

## 12. Repair Budget, Stop Conditions, and Escalation

### Finite repair budget

- At most **three** Builder repair cycles total after the first protected acceptance run.
- A cycle must contain a materially new hypothesis, a bounded change, and new observed evidence.
- After **two materially distinct failed attempts on the same narrow blocker**, the Builder may request owner approval for `bloom-sol-specialist` using the structured handoff required by the Builder role.
- After three materially similar failures, no new evidence, or exhaustion of the total budget, stop `BLOCKED`; do not loop or weaken acceptance.

Attempts directed solely at forcing C05 from a valid, pointer-driven `Hovered` state into exact `Resting` were attempts against an invalid over-constrained assertion. They do not justify further product changes and do not count as evidence that the approved player-visible release-below contract is unsatisfied. This accounting correction does not reset or broaden the general repair policy. After the protected C05 test and runner are corrected by the Acceptance Engineer, Luna must rerun the protected gate and continue only for genuine remaining behavioural failures.

### Immediate stop conditions

- Protected harness absent, changed, or requiring Builder edits; acceptance ownership/hash unclear.
- Need to change DD-28, current click/keyboard compatibility, command/engine/application authority, assembly direction, scene bootstrap/content delivery, or persistence/schema.
- Need for M2 preview/content, M9 polish, production assets/content, named-content branching, package/project-setting changes, or a second UI/input stack.
- Only apparent proof uses direct event/controller/session calls, test-created UI/session, scene reconstruction, fixture CLI injection, manual presenter driving, or transform teleportation.
- Existing approved test conflicts with the intended change, any assertion would need weakening, or protected acceptance appears incorrect.
- Unexpected owner-owned changes overlap an allowed file; automation-capable Editor unavailable; compile/import timeout; unexplained Console errors/missing scripts; required aspect ratio cannot be exercised.
- Public pointer input still cannot be observed after bounded diagnosis, or source/package behavior conflicts with the frozen contract.

Implementation problems remain with the Builder within budget. Specification, architecture, acceptance, or scope conflicts return to the project owner/Planner.

## 13. Auditor Requirements

The read-only Auditor must:

1. verify the authority baseline, protected-harness commit/hash, complete diff, allowed-file compliance, and no protected/existing-test modification;
2. inspect the protected and developer tests for every forbidden bypass before trusting results;
3. independently rerun the protected harness and required gates in a verified `-automated` Editor;
4. independently start from the committed `CombatStage` ordinary entrypoint—no fixture CLI/session injection—and capture protected evidence under `Logs/M1-D01/Auditor/`;
5. verify real EventSystem pointer delivery to an actual runtime-spawned card and all manifest criteria at 16:9, 16:10, and 3440×1440;
6. compare pre/post canonical state, Mana, piles, event history/count, command acceptance count, and all named RNG stream states for cancel/staging paths; for disarmed release, accept `Hovered` only when the unchanged public pointer's real EventSystem raycast target matches the reported restored runtime card;
7. inspect screenshot evidence for real hover rise, non-colour-only arm cue, staged-card uniqueness, legal target highlights, bounds/readability, and no drift/jump/duplicate;
8. verify click/keyboard current behavior remains functional while recording strict §8.5 alignment as an out-of-scope follow-up, not a failure;
9. verify no missing scripts, unexpected Console errors, Editor-only runtime dependency, fixture leak, or build failure;
10. return exactly `PASS`, `FAIL`, or `BLOCKED`. Only `PASS` certifies the task; the Auditor never repairs.

## 14. Completion and Handoff Format

The Builder must not claim acceptance or commit. Return:

- task ID and tested HEAD;
- protected-harness path and commit/hash;
- concise verified root cause, including the first failed public-input transition and disposition of the IPointerDown hypothesis;
- behavior restored and any unchanged/out-of-scope discrepancy;
- exact files changed, separated into product, developer tests, and conditional scene/tooling changes;
- Editor readiness/compilation fields and every command/test result with pass/fail counts and artifact paths;
- ordinary-runtime public-input trace and three-resolution screenshot index;
- cancel/stage/accept state, event, command-count, and RNG evidence;
- repair cycles used and any Specialist handoff/result;
- final Git status/diff summary, unresolved risks, and the strict §8.5 click/keyboard follow-up;
- explicit statement: “Not self-certified; awaiting Bloom Auditor verdict.”

Task completion requires Auditor `PASS`. Git staging/commit/push remains owner/Git Steward work outside this packet.

## 15. Frozen Acceptance-Infrastructure Recovery Amendment

### 15.1 Purpose, baseline, and non-effect on behavior

This amendment repairs only the executable transport and containment around the already-frozen protected acceptance. It does **not** authorize product work, a DD-28 change, a behavioral-test edit, a criterion change, acceptance by historical result, or a reset of the Builder repair budget.

Recovery planning baseline is `4faaac04f70b7f2b5d8df496429409fba8bd2943` on `fix/m1-dd28-runtime-drag`, tracking `origin/fix/m1-dd28-runtime-drag` at `0/0`. At that baseline the focused Builder tests report `2 passed, 0 failed`; the protected behavior report records `13 passed, 0 failed`; protected hashes and pre/post working-tree hashes are intact; and ordinary-runtime public-input traces and three-resolution screenshots exist. These are investigation facts, not a substitute for a fresh recovered protected run.

Observed infrastructure evidence is:

- Pipeline `0.4.0-exp.1` `TestResultCollector.RunFinished` calls `TaskCompletionSource<ITestResultAdaptor>.SetResult` unconditionally at package source line 88; duplicate completion throws `InvalidOperationException` after the report has recorded `13 passed, 0 failed`.
- The failure has recurred across clean Editor sessions. The protected runner correctly classified the affected run `INFRASTRUCTURE_FAILURE` because its Console boundary was not clean.
- A later native assertion, `Access version should be odd when acquiring lock`, became continuously repetitive and is identified during shutdown as `Modules/Audio/Public/Utilities/CriticalSection.h:56`; the project log reached 20.893 GiB. Test completion precedes assertion onset in log order, but causality is unproven.
- Unity is stopped. Planning must not restart it.

Section 15 supersedes only Section 10's former use of Pipeline `run_tests` / `test_status` for this protected fixture. All ordinary-runtime, forbidden-bypass, evidence, hash, dirty-state, classification, Auditor, and completion requirements remain in force.

### 15.2 Owner decisions and exact ownership

1. The protected source `Assets/Bloomdrawn/Tests/Acceptance/M1D01RuntimeDragAcceptanceTests.cs`, its `.meta`, its 13 methods, and every assertion remain byte-for-byte frozen.
2. The recovery uses Unity Test Framework `1.7.0` public `TestRunnerApi.Execute`, `RegisterCallbacks` / `UnregisterCallbacks`, result adaptors, `CancelTestRun`, and `SaveResultToFile`. It must not copy, patch, reflect into, or write `Library/PackageCache`, and must not change `Packages/manifest.json`, Unity `6000.5.5f1`, or any package/version.
3. Pipeline remains only the command transport used to invoke the task-local start/abort bridge and ordinary health/Console commands. The recovered protected run must never call Pipeline `run_tests`, `test_status`, `cancel_tests`, or `PipelineTestRunner`.
4. Recovery implementation and refreeze belong exclusively to the protected Acceptance Engineer. The Builder must not edit, repair, or own these files. The Auditor remains read-only and is the only final certifier.
5. No infrastructure attempt counts as a Builder repair cycle, resets that budget, or permits additional implementation paths.

### 15.3 Exact authorized infrastructure path set

The Acceptance Engineer may create or modify **only** these paths:

- `Tools/Acceptance/Invoke-M1D01Acceptance.ps1`
- `Tools/Acceptance/M1-D01-runner-contract.json`
- `acceptance/locks/M1-D01-protected.sha256.json`
- `Assets/Bloomdrawn/Tests/Acceptance/Infrastructure.meta`
- `Assets/Bloomdrawn/Tests/Acceptance/Infrastructure/Bloomdrawn.M1D01.AcceptanceInfrastructure.asmdef`
- `Assets/Bloomdrawn/Tests/Acceptance/Infrastructure/Bloomdrawn.M1D01.AcceptanceInfrastructure.asmdef.meta`
- `Assets/Bloomdrawn/Tests/Acceptance/Infrastructure/M1D01AcceptanceTestBridge.cs`
- `Assets/Bloomdrawn/Tests/Acceptance/Infrastructure/M1D01AcceptanceTestBridge.cs.meta`

The new assembly must be Editor-only and may reference only `Unity.Pipeline`, `UnityEditor.TestRunner`, `UnityEngine.TestRunner`, and normal Unity/System assemblies required for file/result serialization. It may not reference Bloomdrawn Engine, Application, Presentation, Content, runtime test implementation, or product assemblies. No existing assembly definition may change. `Tools/Acceptance/M1-D01-expected-values.json` remains protected and unchanged unless a later owner amendment explicitly authorizes it.

Any need for another path—including product/Builder files, the protected behavioral test, `Packages/**`, `ProjectSettings/**`, a shared launcher, an existing asmdef, or PackageCache—is an immediate stop and return to the owner/Planner.

### 15.4 Hardened bridge contract

The dedicated assembly exposes exactly two task-local commands: a start command for the exact protected fixture and an abort command for the active run. It must not expose an arbitrary test name, assembly, category, scene, or expected-result input.

The start command must:

1. accept a runner-generated cryptographically unique run ID and task-local evidence directory only;
2. fail if another run is active, if a stale Pipeline `Temp/pipeline_test_request.json` or `Temp/pipeline_test_status.json` exists, or if its output directory/status already exists;
3. atomically persist a request containing task ID, run ID, tested HEAD, exact fixture identity, exact expected method identities, start UTC, and lifecycle state before execution;
4. register one callback instance per domain, execute Play Mode in the Editor through `TestRunnerApi.Execute`, persist the returned job GUID, and rely on Unity Test Framework's normal Play Mode/domain-reload continuation;
5. after domain reload, re-register only against the matching nonterminal request and never call `Execute` a second time;
6. verify the started/result tree belongs exactly to the frozen fixture and contains the contract's 13 expected test method identities; an unrelated, concurrent, missing, extra, or zero-test tree is infrastructure failure;
7. reconstruct leaf results from the authoritative root result tree, save NUnit XML through `TestRunnerApi.SaveResultToFile`, and save a bounded JSON summary/result list; and
8. use same-volume temporary files plus atomic replace/move so pollers never observe partial JSON or XML.

Lifecycle states are monotonic: `prepared -> running -> completing -> completed|behavioral_failure|infrastructure_failure|aborted`. The outer runner polls the task-local atomic status file directly every 250 ms; it does not use Pipeline test status. Loss of the start HTTP response is not itself success or failure: the matching durable status must prove whether the exact run started.

The abort command may call `TestRunnerApi.CancelTestRun` only for the persisted active job GUID, request Play Mode exit, mark the run aborted, and return. It may not suppress results or turn a failure into success.

### 15.5 Duplicate completion policy

The bridge must assume `RunFinished` can be delivered more than once. Completion ownership is acquired with both an in-domain atomic guard and a same-volume create-new completion claim keyed by run ID.

- The first delivery freezes a fingerprint over fixture identity, all leaf full names/statuses, summary counts, duration-independent result content, messages, and stack traces; then it writes XML/JSON and the terminal state.
- A later delivery must never call `SetResult`, overwrite the first result, repeat `SaveResultToFile`, or throw. It records a bounded duplicate-delivery event.
- An identical fingerprint is accepted as duplicate framework delivery only after a five-second post-completion quiescence window; its count and fingerprint remain evidence.
- A different fingerprint, changed test set, changed status, or inability to compare/write the duplicate record is `INFRASTRUCTURE_FAILURE`.

Idempotence is not an error allowlist. A failing, skipped, inconclusive, missing, or extra protected test remains non-passing. The runner requires exactly `13 total, 13 passed, 0 failed, 0 skipped, 0 inconclusive` from freshly written XML and JSON.

### 15.6 Console and log policy

There is no accepted Console-error allowlist for the recovered gate. From dedicated Editor launch through confirmed shutdown, any Unity Console or dedicated log `Error`, `Exception`, or `Assert` entry is infrastructure failure unless it is an assertion intentionally expected and consumed inside a protected test through Unity Test Framework; the frozen M1-D01 fixture currently expects none. The runner must not ignore `InvalidOperationException`, `TestResultCollector.RunFinished`, the audio-lock assertion, or errors merely because all tests passed.

The runner must not clear the Console. It records a startup boundary, a pre-test cursor, and the complete bounded post-test error/assert/exception evidence. A dropped/truncated Console query, parse failure, unavailable cursor, or mismatch between Console and dedicated-log detection fails closed. A valid behavioral failure is reported `BEHAVIORAL_FAILURE` only when result integrity is complete and no infrastructure/Console/log failure occurred; infrastructure corruption takes precedence when both are present.

### 15.7 Audio-lock containment and finite limits

The recovered runner requires no project Editor at entry. It launches one fresh, task-owned `-automated` Editor through `Tools/open-automated-editor.ps1 -AdditionalArguments`, supplying `-logFile` with a unique path under `Logs/M1-D01/Acceptance/runs/<run-id>/Editor.log`. It records the exact launched PID and must never attach this gate to or terminate a pre-existing Editor.

An out-of-process watchdog starts immediately after launch and scans appended bytes, preserving pattern matches across chunk boundaries. Either of these conditions triggers immediate abort and `INFRASTRUCTURE_FAILURE`:

- first exact occurrence of `Access version should be odd when acquiring lock`;
- dedicated Editor log reaches 64 MiB.

On trigger, the watchdog writes run ID, UTC, PID, byte offset, current size, reason, and bounded context; requests the bridge abort when reachable; requests graceful close of the exact task-owned PID; waits at most five seconds; then may force-stop **only that recorded task-owned PID**. It captures at most 256 KiB around the first trigger and the final 256 KiB. It never automatically retries.

Other limits are: 180 seconds for startup/Pipeline readiness; 180 seconds for import/compile health; 30 seconds for start acknowledgement or durable `running`; 900 seconds for the exact protected test run; five seconds post-completion quiescence; 30 seconds for normal graceful Editor shutdown; and five seconds after an abort before exact-PID force containment. XML and JSON are each capped at 16 MiB, public-input trace at 4 MiB, each screenshot at 16 MiB, screenshots combined at 128 MiB, and non-log run evidence combined at 256 MiB. Exceeding a limit fails closed.

Each attempt uses a new run directory and never truncates or overwrites prior evidence. No more than three task-local Editor logs or 192 MiB of retained task-local Editor logs may exist before launch; the runner then stops for owner-controlled archival rather than deleting evidence.

### 15.8 Fresh-Editor validation sequence

No Editor may execute more than one Unity test suite for this recovery. Compilation, health, bridge self-diagnostics, and non-test validators may share the fresh process used for one suite. Before another focused, protected, complete Edit Mode, or complete Play Mode suite, the prior task-owned Editor must exit and process shutdown must be verified. Batch tools that launch and own their own Editor already satisfy a separate-process boundary when their exact PID/log is recorded.

The Acceptance Engineer validates in this order:

1. static diff/path review; unchanged protected behavioral-test and Builder hashes; updated contract schema; no forbidden API/bypass/package edits;
2. refreeze candidate hashes, but do not declare them final until runtime validation succeeds;
3. ensure no project Editor or stale Pipeline test request/status exists;
4. launch one fresh task-owned automated Editor with the unique bounded log and watchdog;
5. require Unity `6000.5.5f1`, Pipeline/Editor ready, compilation inactive, compile errors zero, and successful import of the dedicated bridge assembly;
6. run a bounded bridge self-diagnostic that proves atomic lifecycle transitions, identical duplicate idempotence, divergent duplicate failure, stale-run rejection, and size/timeout classification without synthesizing a gameplay result;
7. establish Console/log boundaries without clearing them;
8. invoke the bridge start command once and poll only its atomic status file;
9. require fresh XML/JSON for the exact run ID and HEAD with `13/13`, all exact method identities, no skips/inconclusives, all required screenshots/traces/state evidence, zero unexpected Console/log entries, no audio-lock marker, and intact pre/post Git/protected hashes;
10. wait the five-second duplicate quiescence window, gracefully close the task-owned Editor, verify PID/children/Pipeline are gone, then classify the gate;
11. run static script checks and `git diff --check` for the protected infrastructure diff; and
12. finalize and record exact protected hashes only after the successful run, then perform one clean confirmation run from another fresh Editor against that frozen set. The confirmation run is the result handed back to the Builder/owner.

The prior `13/13` artifact is retained as diagnosis only. It cannot satisfy steps 8-12.

### 15.9 Recovered runner acceptance and Auditor anti-façade proof

The recovered runner returns `PASS` only when every original criterion and all infrastructure conditions pass in one fresh run. It retains existing classification exit codes and dirty-state integrity behavior. Pre/post snapshots include every dirty/untracked path and hash, the tested HEAD, exact protected hashes, task-local bridge hashes, runner command transcript, job/run IDs, Editor PID/version/arguments, Console cursors, dedicated-log hash/size, XML/JSON hashes, evidence inventory, duplicate-delivery count/fingerprints, and shutdown verification.

The Auditor must independently:

- verify the behavioral test source/meta hashes are unchanged and inspect all bridge/runner source for bypasses;
- prove command history contains bridge start/abort only and no Pipeline `run_tests`, `test_status`, fixture injection, direct handler/controller/session call, result synthesis, or transformed historical artifact;
- parse NUnit XML independently, compare the 13 full method identities and statuses to the frozen source/contract, and cross-check JSON, screenshots, trace sequences, timestamps, run ID, HEAD, and state/event/command/RNG evidence;
- verify the bridge is Editor-only, has no product-assembly reference, and does not alter scene/runtime composition;
- exercise or inspect evidence for identical-duplicate idempotence and divergent-duplicate fail-closed behavior;
- verify the watchdog, 64 MiB cap, exact-PID ownership, bounded evidence, fresh-process boundary, and clean shutdown; and
- independently rerun the recovered protected gate in a fresh automated Editor before any `PASS` verdict.

Fresh evidence and independent XML/source correlation are mandatory; a bridge that writes expected `13/13` values without a genuine TestRunnerApi root result is a façade and immediate `FAIL`.

### 15.10 Rollback, stop conditions, and handoff

On timeout, audio assertion, log cap, duplicate mismatch, Console/log error, missing/corrupt result, behavioral failure, repository mutation, or unhealthy Editor, the runner attempts active-job cancellation, exits Play Mode, contains its exact PID, completes bounded integrity evidence, and stops. It must not retry automatically, weaken classification, delete logs, or repair product code.

There is no automatic source rollback. If recovery is rejected or cannot compile, the Acceptance Engineer stops with the exact diff and hashes; owner/Git Steward may restore only the Section 15.3 infrastructure paths from the recorded pre-recovery commit/hash. Builder-owned and product paths are never rollback targets.

Immediate return to owner/Planner is required if public TestRunnerApi cannot support the bridge without package/version/manifest/PackageCache/shared-assembly changes; if the frozen test or any behavior criterion would need editing; if command transport cannot start the exact run without Pipeline's collector; if the watchdog cannot own/contain the launched PID; or if one clean confirmation attempt reproduces the native audio assertion. Such outcomes do not consume or reset Builder repair budget.

**Implementation handoff role:** protected `bloom-acceptance-engineer`, not Builder or Sol. Its return must list exact files/hashes, public APIs used, lifecycle/duplicate strategy, self-diagnostic results, both fresh-run IDs/PIDs/logs, confirmation classification, XML/JSON/evidence paths, Console/log/audio-watch results, shutdown proof, complete Git integrity, and any stop condition.

**Acceptance-engineering handoff:** after one successful validation run and one successful confirmation run are hash-frozen, return control to the owner/Planner. The owner may then resume the Builder only for the packet's remaining validation/handoff—without implementation repair—and may invoke the Auditor only after all required gates are complete. Never invoke either role automatically.

## 16. Frozen Narrow Follow-up — Solution Lifecycle and Run-directory Ownership

### 16.1 Purpose, authority, baseline, and retained evidence

This owner-approved follow-up corrects exactly two infrastructure contracts exposed by the first Section 15 validation attempt: the owner-managed solution lifecycle and the runner/bridge run-directory collision. It supersedes only contrary Section 9 or Section 15 wording that requires `Bloomdrawn-Unity.slnx` to remain unchanged while the task-owned Editor is alive, treats that file as a general dirty-path exception, requires the bridge output directory not to exist when the runner must already own it, or places bridge files directly in the runner-owned run root. Every other Section 15 requirement remains frozen.

Continuation authority baseline is commit `5b35f3f95699a41d2be01af502304e54cdbc9dd3` on `fix/m1-dd28-runtime-drag`, tracking `origin/fix/m1-dd28-runtime-drag` at `0/0`, with an empty index. Unity is stopped. The existing product, Builder-test, and protected-test hashes recorded by the failed run remain unchanged. Planning must not restore the solution, edit implementation, or start Unity.

Failed run `d1cb15d2baa243a1bcaf0e026ff29c3b` and Editor PID `24420` remain permanent diagnostic evidence under `Logs/M1-D01/Acceptance/runs/d1cb15d2baa243a1bcaf0e026ff29c3b`. It classified `INFRASTRUCTURE_FAILURE`; Unity `6000.5.5f1` imported with zero compile errors, emitted no audio-lock signature, wrote a 41,507-byte task-local log, and exited gracefully. The fixture never started, so this attempt is neither of the two required successful fresh runs and supplies no behavioral result.

### 16.2 Exact `.slnx` diff classification and one-time continuation precondition

The recorded pre-run owner state was an existing unstaged `Bloomdrawn-Unity.slnx`, SHA-256 `045AD0C2BEAE7D3B93CC4DEDECEC765BB340583999D0C4EA53A7F769BE8AA5B4`. It was UTF-8 with BOM, CRLF-only, 485 bytes. The stopped post-run file is 556 bytes, SHA-256 `16E5DA57CB8654CE08F7BFE7CA72622926DA2E888CC67D7D3266AA7A08ABFB35`, and differs from the pre-run bytes by exactly this one 71-byte CRLF-terminated line between the Presentation and Application entries:

```diff
   <Project Path="Bloomdrawn.Presentation.csproj" />
+  <Project Path="Bloomdrawn.M1D01.AcceptanceInfrastructure.csproj" />
   <Project Path="Bloomdrawn.Application.csproj" />
```

Removing exactly that line from the current bytes reproduces the recorded 485-byte pre-run hash. BOM, CRLF encoding, all existing entries, and their order are otherwise identical. Therefore the failed-run delta is classified as **Unity-generated solution/project membership regeneration for the newly imported acceptance-infrastructure assembly**, not newline normalization, arbitrary formatting, or a semantic owner edit.

Against committed HEAD, the complete current diff also reorders existing project entries and includes `Assembly-CSharp.csproj`; those differences were already present in the owner-managed pre-run state and are not attributable to PID `24420`. The continuation must preserve that owner state rather than restore the committed blob.

Before any new Editor launch, the Acceptance Engineer is authorized and required to reconstruct the exact recorded pre-run bytes by removing only the identified infrastructure-project line, verify the resulting length/encoding/SHA-256 above, restore those bytes atomically, and prove the resulting Git status is the same pre-run `.M` status. This is restoration of owner bytes under this lifecycle, not an implementation edit. Any mismatch or need to infer other owner content is an immediate stop.

### 16.3 Frozen owner-managed solution lifecycle

`Bloomdrawn-Unity.slnx` is not an implementation path and must never be staged, committed, retained in Unity-regenerated form, normalized, or restored from HEAD. It is the sole operational lifecycle exception; no other dirty file gains backup, restoration, hiding, normalization, or rollback permission.

For each validation and confirmation run, the runner must:

1. Before launch, record exact existence, regular-file status, bytes, SHA-256, length, BOM/newline facts, creation/write timestamps, Git status, and exact diff against HEAD. Copy the byte-exact backup to an OS temporary location outside the project and outside every Unity-authored evidence/cache path; record and re-verify the backup path, length, and hash.
2. Start a byte/hash/metadata watcher before launching Unity. Until the task-owned PID is established, any solution change fails closed. While that exact owned Unity PID is alive, mutation is tolerated only as a candidate Unity regeneration; the runner never writes the solution during this interval and continuously rejects another Unity process owning the project.
3. Attribute a tolerated candidate to Unity regeneration only when every observed change occurs inside the exact owned-PID lifetime, the path remains the expected regular `.slnx` file, its XML is valid, it contains only unique relative `.csproj` project-membership entries, and its additions/removals/reordering correspond to Unity-generated project/assembly membership present for that import. If operating-system writer identity evidence identifies any writer other than the exact owned Unity PID or its verified Unity-owned regeneration child, fail closed. Any other XML/content change, missing interval evidence, mutation outside that lifetime, competing project-owning Unity process, unexplained writer evidence, deletion, reparse/redirected path, or unexpected replacement also fails closed.
4. On every success, failure, timeout, watchdog trigger, or abort, first contain only the task-owned PID under Section 15 and prove that PID, recorded children, and project Pipeline ownership are gone. Never restore while Unity or an uncontained child may still write the file.
5. After proven process exit and before final repository comparison, capture the observed post-run bytes, SHA-256, exact pre/post diff, length, BOM/newline facts, creation/write timestamps, and last watcher observation. Preserve those facts in the runner-owned run evidence even when no mutation occurred.
6. Restore from the verified external backup by writing the backup bytes to one uniquely named same-directory temporary file, flushing it durably, atomically replacing/moving it over `Bloomdrawn-Unity.slnx`, and removing any residual temporary file. This temporary is an operational atomic-write artifact only, not an authorized implementation path; it must never survive finalization or appear in final Git state.
7. Re-read and verify byte-for-byte equality, SHA-256, length, metadata facts required by the contract, exact diff against HEAD, and Git status against the pre-run record. Final integrity comparison occurs only after this restoration. Restoration failure, a missing backup, residual temporary, or any final bytes/status/diff mismatch is `INFRASTRUCTURE_FAILURE`.

The lifecycle evidence must identify run ID, HEAD, branch, task-owned PID, backup and restoration timestamps, pre/observed-post/restored hashes and lengths, exact observed mutation diff, atomic-restore outcome, and final Git comparison. A run cannot return `PASS` until this evidence is durable. If the owned PID cannot be proven exited, the runner must not race it by restoring; it contains as allowed, reports the unresolved state, and stops for the owner.

### 16.4 Frozen run-root and bridge-child ownership

Each attempt uses a fresh unique root:

`Logs/M1-D01/Acceptance/runs/<run-id>/`

The outer runner exclusively creates and owns that root, the task-local `Editor.log`, all process/Git/Console/watchdog/solution-lifecycle/final-classification evidence, and an atomic `run-ownership.json` sentinel. The sentinel is created before launch with task ID, run ID, tested HEAD, branch, root path, creation UTC, and expected PID state `unassigned`; immediately after launch ownership is proven, the runner atomically updates it to `owned` with exact Editor PID, project path, Unity version, `automated=true`, command-line hash, and log path. The bridge may start only from the `owned` state while that PID is alive and matches the requested project/run.

Before bridge start, the root may contain only this exact runner-owned bootstrap set:

- `run-ownership.json`
- `Editor.log`
- `commands.ndjson`
- `lifecycle-observations.ndjson`
- `git-before.txt`
- `working-tree-before.json`
- `protected-hashes-before.json`
- `slnx-pre-run.json`
- `slnx-backup.json`
- `editor-ownership.json`
- `recompile-status.json`
- `editor-health.json`
- `console-startup-to-pretest.json`

The runner contract may rename a listed evidence file only if the packet and manifest identity remain unambiguous and the contract enumerates the exact replacement; it may not accept globs, arbitrary files, or a broad pre-existing-directory allowlist. Any other pre-existing root entry, mismatched/malformed sentinel, wrong run ID/HEAD/branch/PID, pre-existing `bridge` child, symlink/reparse redirection, or non-empty unrecognized location fails closed.

The bridge receives the validated run root and run ID, validates the sentinel and exact bootstrap inventory, then exclusively creates and owns:

`Logs/M1-D01/Acceptance/runs/<run-id>/bridge/`

All bridge request/status/heartbeat/lifecycle state, completion claim, callback and duplicate diagnostics, NUnit XML, result JSON, copied runtime trace/screenshots, evidence inventory, and bridge self-diagnostics live under that child. The runner never pre-creates or writes the child; after creation it reads it only for polling and validation. The bridge never writes the run root or any sibling except through its existing protected test's frozen runtime-evidence source contract. Atomic bridge temporary files remain inside the child and must not survive terminal completion. Path normalization must prove the child is the canonical non-reparse descendant of the matching root.

Bridge self-diagnostics must additionally prove rejection of a mismatched sentinel, an unexpected root entry, a pre-existing bridge child, and an redirected/non-canonical child; successful creation in a valid synthetic ownership layout; and no bridge write outside that child. These diagnostics may exercise file-lifecycle validation only and must not synthesize or infer a gameplay result.

Validation and confirmation require distinct run IDs, roots, bridge children, sentinels, task-local logs, Editor PIDs, lifecycle files, and artifacts. Neither may reuse, truncate, overwrite, or transform failed-run or earlier successful evidence.

### 16.5 Unchanged implementation scope and non-goals

The Acceptance Engineer's implementation path set remains exactly Section 15.3: modify only the runner, runner contract, and protected lock; create/maintain only the five listed acceptance-infrastructure asset/meta paths. No product, Builder test, protected behavioral test, package, project setting, scene, solution, existing assembly definition, or additional source path is authorized. `Bloomdrawn-Unity.slnx` restoration and its ephemeral atomic temporary are operational lifecycle actions only and are excluded from the implementation diff/commit set.

The protected source/meta, all 13 methods/assertions, ordinary committed `CombatStage` entrypoint, real Play Mode, public Input System/EventSystem delivery, screenshots, trace, zero-error policy, result integrity, duplicate policy, watchdog, limits, exact-PID containment, classifications, and Auditor ownership remain unchanged. Failed-run evidence must be preserved.

### 16.6 Continuation validation, repair budget, and revised stop conditions

The Acceptance Engineer has one narrow continuation repair cycle: correct only the two contracts above within the existing eight infrastructure paths, perform static validation, then run one fresh validation followed—only after it passes—by one fresh confirmation. There is no automatic retry or third run. This continuation never invokes Builder, Sol Specialist, Auditor, or Git Steward; there is no Sol escalation trigger for protected acceptance engineering. Any implementation/specification conflict returns directly to the owner/Planner.

Before the first corrected run, verify the failed evidence remains intact, perform the one-time solution restoration in §16.2, and re-verify every protected, product, Builder-test, owner-managed, and infrastructure hash. Then require the original Section 15 static checks and all original bridge/behavioral evidence for each successful run. For each run independently prove captured Unity solution mutation or an explicit no-mutation observation, post-shutdown byte-exact owner restoration, matching final Git status/hash/diff, no other dirty-file change, clean task-owned shutdown, exact `13 passed, 0 failed, 0 skipped, 0 inconclusive`, complete XML/JSON/trace/screenshots, zero unexpected Console/log errors, completed-once lifecycle, and no divergent duplicate. Compare both successful runs as Section 15 requires.

Stop immediately without scope expansion or automatic retry when any existing Section 15 stop condition occurs, or when:

- the current solution cannot be restored exactly to the verified 485-byte pre-run owner form before launch;
- the external backup cannot be created/read/hash-verified, the watcher starts late, or writer/lifetime attribution is incomplete;
- the solution changes before owned-PID establishment, after owned-PID exit but before runner restoration, or while another project-owning Unity process exists;
- the observed solution delta is not valid and explainable Unity project-membership regeneration;
- the solution disappears, redirects, is unexpectedly replaced, or cannot be restored atomically after proven shutdown;
- final solution bytes, SHA-256, Git status, or diff differ from that run's pre-run record, or an atomic temporary remains;
- the run root/sentinel/bootstrap inventory is invalid, the bridge child pre-exists, either owner writes outside its frozen area, or any unexpected path appears;
- the first corrected run is a genuine behavioral failure, reproduces either audio-lock signature, reaches the log cap, has incomplete evidence, or otherwise fails; or
- the required confirmation differs materially, reuses any first-run identity/artifact/process, or fails any gate.

### 16.7 Acceptance Engineer continuation handoff

Return the complete Section 15.10 handoff plus: exact failed-run preservation path; the pre-run/current `.slnx` classification and hashes; one-time restoration proof; for both successful runs the solution pre/observed-post/restored hashes, exact captured diff and timestamps, external backup verification, atomic restoration record, final Git equality, run-root sentinel and inventory, bridge-child inventory, run IDs, PIDs, logs, lifecycle files, shutdown proof, and cross-run comparison. List final hashes for all eight authorized infrastructure files and the protected source/meta before/after hashes. Confirm no non-authorized implementation path changed and state explicitly: **infrastructure validation only; this does not certify M1-D01 product completion.** Stop after handoff and never invoke the Builder or Auditor automatically.

## 17. Frozen Minimal Restoration-helper Correction

### 17.1 Purpose, baseline, and exact root cause

This amendment corrects only the helper used to perform the already-authorized one-time and per-run `Bloomdrawn-Unity.slnx` restoration. It does not change the solution lifecycle, bridge architecture, run-directory ownership, protected acceptance, product behavior, implementation path set, validation criteria, or any other requirement frozen in Sections 15–16.

Restoration-helper planning baseline is commit `8d33c334fcb713a8495fe6a2f631f3b62fafb023` on `fix/m1-dd28-runtime-drag`, tracking `origin/fix/m1-dd28-runtime-drag` at `0/0`, with an empty index and no project-owning Unity process. The current solution remains unmodified at 556 bytes and SHA-256 `16E5DA57CB8654CE08F7BFE7CA72622926DA2E888CC67D7D3266AA7A08ABFB35`; the reconstructed owner bytes remain verified in memory at 485 bytes and SHA-256 `045AD0C2BEAE7D3B93CC4DEDECEC765BB340583999D0C4EA53A7F769BE8AA5B4`. No Unity launch or infrastructure correction occurred.

The failed helper called the three-string `System.IO.File.Replace(sourceFileName, destinationFileName, destinationBackupFileName)` overload with a non-empty same-directory temporary source, the validated absolute solution target, and `String.Empty` (`""`) as `destinationBackupFileName`. The target had been resolved to `D:\Dev\Projects\Bloomdrawn-Unity\Bloomdrawn-Unity.slnx`; the rollback argument had not been constructed or normalized to a path. Because an empty string is non-null, .NET attempted to normalize it with `Path.GetFullPath("")` and threw `The path is empty. (Parameter 'path')` before the operating-system replacement. The empty third rollback argument—not a relative target, empty target parent, null backup, missing target, or general unreliability of `File.Replace`—was the exact root cause. The target remained 556 bytes at its original hash, no replacement occurred, and no restoration temporary or rollback file remained.

The failed-run evidence remains frozen at:

- `Logs/M1-D01/Acceptance/runs/d1cb15d2baa243a1bcaf0e026ff29c3b`
- `Logs/M1-D01/Acceptance/solution-recovery-d1cb15d2baa243a1bcaf0e026ff29c3b/`

### 17.2 Frozen absolute-path and rollback-path protocol

Before any restoration write, the Acceptance Engineer must:

1. Resolve the repository root with an existing canonical absolute path and derive `Bloomdrawn-Unity.slnx` from that root. Resolve the target, its parent, the unique temporary path, and the unique rollback path to explicit absolute paths.
2. Reject any root, target, temporary, or rollback value that is null, empty, whitespace-only, relative, non-canonical, rooted outside the verified repository root, or associated with a nonexistent parent. Reject a reparse/symlink redirect or root mismatch. The target parent must be exactly the resolved repository root.
3. Create both restoration artifact names in the target's directory. The temporary and rollback names must include a cryptographically unique operation ID and must be explicit non-empty paths. The rollback argument passed to `File.Replace` may never be `null`, `String.Empty`, whitespace, an omitted placeholder, or a source/target alias.
4. Log the operation ID and resolved absolute target, temporary, and rollback paths before replacement. Path evidence may be written to the applicable external or runner-owned evidence location, but solution bytes must not be duplicated into repository-controlled source paths.

Immediately before the single replacement call, verify and durably record:

- the temporary file exists and hashes to owner SHA-256 `045AD0C2BEAE7D3B93CC4DEDECEC765BB340583999D0C4EA53A7F769BE8AA5B4`;
- the target exists, remains 556 bytes for the one-time restoration, and hashes to regenerated SHA-256 `16E5DA57CB8654CE08F7BFE7CA72622926DA2E888CC67D7D3266AA7A08ABFB35`; for later per-run restoration, it instead matches the captured transient hash whose restoration has already passed Section 16 content/lifetime checks;
- target, temporary, and rollback are three distinct absolute paths under the same exact parent directory;
- no rollback file already exists, and the temporary/rollback names cannot collide with a prior operation; and
- the temporary bytes have been flushed durably and the target has not changed since the final precondition snapshot.

The helper then performs exactly one `System.IO.File.Replace(temporaryAbsolutePath, targetAbsolutePath, rollbackAbsolutePath)` call. It must not substitute an empty backup argument, perform a preliminary destructive move, or automatically fall back to a non-atomic overwrite.

After replacement, before deleting rollback evidence, verify the target immediately and durably:

- length is exactly 485 bytes;
- SHA-256 is exactly `045AD0C2BEAE7D3B93CC4DEDECEC765BB340583999D0C4EA53A7F769BE8AA5B4`;
- UTF-8 BOM and CRLF facts remain correct;
- Git status and exact diff against HEAD equal the verified owner state; and
- the rollback file exists and contains the exact pre-replacement target bytes/hash.

Only after all restoration checks succeed may the helper delete the rollback file. It then proves that no operation temporary or rollback file remains and records completion atomically. The later Section 16 per-run helper follows the same protocol using that run's verified pre-run backup as the temporary content and retaining the replaced transient form as rollback evidence until final equality succeeds.

On any validation, write, replacement, or post-replacement failure, do not launch Unity, do not call `File.Replace` a second time, do not perform fallback restoration, and do not delete available target, temporary, rollback, path, hash, or exception evidence. Return directly to the owner/Planner. If replacement succeeded but post-checks failed, the rollback remains evidence; no automatic rollback is authorized.

### 17.3 Scope, correction allowance, and continuation

The authorized implementation scope remains exactly Section 15.3's eight paths: modify only the runner, runner contract, and protected lock; maintain only the five acceptance-infrastructure asset/meta paths. `Bloomdrawn-Unity.slnx` remains an owner-managed transient restoration target, not an implementation or commit path. No product, Builder test, protected test, package, scene, project setting, solution source, shared tooling, or additional path is authorized.

The failed helper invocation consumed the Section 16 continuation attempt before any correction or Unity launch. The owner now authorizes exactly **one additional Acceptance Engineer correction cycle**. That single cycle may correct this restoration helper and then, after byte-exact one-time restoration succeeds, complete only the already-frozen solution lifecycle and run-directory ownership corrections, protected-hash refreeze, one fresh validation run, and one independent fresh confirmation run. There is no automatic helper retry, replacement retry, third validation attempt, repair-budget reset for the Builder, or Sol escalation.

Every Section 15–16 stop condition remains in force. The additional cycle stops immediately if any path precondition is invalid, the one replacement fails, post-replacement owner equality fails, evidence cannot be retained, an unauthorized path/hash changes, or either subsequent fresh run fails any frozen condition.

### 17.4 Revised Acceptance Engineer handoff

In addition to Sections 15.10 and 16.7, report the exact prior empty-rollback root cause; the additional cycle used; the operation ID; resolved absolute root/target/temporary/rollback paths; every precondition and pre-replacement hash/length result; the single `File.Replace` outcome; restored target and rollback hashes; Git status/HEAD-diff equality; rollback deletion only after verification; proof that no helper artifacts remain; and any retained evidence on failure. Then provide the unchanged Section 16 correction and two-run handoff. State explicitly: **Infrastructure validation only; M1-D01 product completion remains uncertified.** Stop after handoff and never invoke another role automatically.
