# M1-D01 — Restore the Real DD-28 Ordinary-Runtime Card Drag Path

**Status:** FROZEN FOR OWNER REVIEW; **BLOCKED before Builder invocation** until the protected external acceptance harness named below exists.  
**Authority baseline:** `6e910f7811dc3b7f07aa5d30b7ca574d561b45a6` (`fix/m1-dd28-runtime-drag`; source baseline and merge-base both verified at planning time).  
**Planning worktree:** `D:\Dev\Projects\Bloomdrawn-Unity-M1D01`  
**Acceptance manifest:** `acceptance/manifests/M1-D01-dd28-runtime-drag.yaml`

**Owner amendment:** The release-below, protected-runner dirty-state, workforce-v4, and repair-accounting corrections below were approved after implementation exposed over-constrained acceptance encoding and obsolete workforce requirements. These corrections do not weaken DD-28 or broaden M1-D01 scope.

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
2. protected `M1D01RuntimeDragAcceptanceTests` (Builder may run but not edit it);
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
