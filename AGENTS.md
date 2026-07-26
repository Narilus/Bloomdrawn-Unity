# Bloomdrawn Agent Instructions

This repository is the authoritative Unity implementation of **Bloomdrawn**. Work as a disciplined implementation agent, not as an autonomous designer.

## 1. Source of truth

Read only as much as the active task requires, but respect this authority order:

1. `docs/DESIGN.md`
2. `plans/design-decisions.md` for approved decisions that explicitly amend or clarify the design
3. `plans/implementation_plan.md`
4. the active task plan under `plans/tasks/`
5. automated tests that correctly encode approved rules
6. engine/application implementation
7. persistence/adapters
8. presentation, scenes, prefabs, VFX, audio, and UI

Companion direction files such as `docs/bloom_identity_pass.md` and `docs/feature_additions.md` provide approved context but do not override `docs/DESIGN.md` or an approved decision.

A lower layer must never silently redefine a higher layer. If implementation exposes a conflict, ambiguity, missing decision, or impossible acceptance criterion, stop the affected work and report it. Do not invent a rule to keep moving.

## 2. Task scope is strict

Implement **exactly one approved task at a time**.

Before editing:

- identify the active task ID and task-plan file;
- read its Objective, In Scope, Non-Goals, required source documents, tests, validation commands, and exit criteria;
- inspect relevant existing code and assets before proposing changes;
- state a concise preflight: current understanding, files/systems likely affected, and any blocking ambiguity.

During implementation:

- do not begin later tasks;
- do not add speculative systems “for future use”;
- do not opportunistically refactor unrelated code;
- do not broaden the threat model, validation contract, or acceptance criteria beyond the active task;
- do not create production content whose schema or design gate belongs to a later task;
- when a useful improvement is out of scope, record it as a follow-up rather than implementing it.

The user is the scope-changing authority. An agent may identify a needed change; it may not silently approve that change itself.

## 3. Unity baseline

- Host engine: **Unity 6.5 (`6000.5.x`)**.
- The exact Editor patch is the version pinned by `ProjectSettings/ProjectVersion.txt` and wins over assumptions in prose.
- Primary development/validation host: Windows 11.
- Language: C# supported by the pinned Unity Editor.
- Use repository Assembly Definitions and dependency boundaries established by M0. Do not collapse assemblies for convenience.
- Runtime presentation uses the Unity technologies approved by `docs/DESIGN.md` and the implementation plan; do not substitute a different UI/input/rendering stack without an approved design change.

Do not assume an API merely because it exists in another Unity release. When Unity behavior is version-sensitive, check the current Unity 6.5 documentation or the package/version actually installed in the project.

## 4. Unity CLI and Pipeline are first-class agent tools

Unity CLI and Unity Pipeline are the preferred machine-facing interface to the running Editor when they can perform or verify the operation cleanly.

The Unity CLI is experimental. **Never rely on remembered CLI syntax when discovery is cheap.** At the start of CLI-heavy work, use the installed tool's help/discovery surface, for example:

```powershell
unity --help
unity command --help
unity pipeline list
unity command
```

Use `unity status` when supported by the installed CLI and useful for disambiguating Editor instances. If multiple Editors/projects are open, target the project explicitly rather than guessing.

For structured automation, prefer JSON/TSV/NDJSON or other machine-readable output exposed by the installed CLI and check process exit codes. Do not scrape progress bars or human-formatted output when structured output exists.

Use live Editor evaluation for **inspection and bounded one-off diagnostics**, not as a substitute for maintainable project code. Because the experimental CLI surface can change, discover the installed eval form before using it. Repeated operations should become project-owned Pipeline/Editor commands with stable `bloom.*` names once the relevant task permits them.

Examples of desirable project commands as they become implemented:

```text
bloom.health
bloom.validate-content
bloom.scene-summary
bloom.load-combat-fixture
bloom.reset-combat-fixture
bloom.dump-combat-state
bloom.validate-combat-layout
```

Do not claim a Unity change works solely because a file write succeeded. Verify that Unity imported/compiled it and that the relevant Editor/test/runtime behavior is correct.


### 4.1 Agent-controlled Editor launch is automated-only

Any Unity Editor instance used for **agent-controlled** Pipeline access, live Editor inspection, Play Mode interaction, scene/prefab authoring, or runtime validation must be launched with the project-required `-automated` Editor argument.

This is a hard operational requirement for agent interactivity with the Editor.

- Unity Hub project launch arguments do not automatically protect direct Editor or CLI launches. Never assume `-automated` is present merely because it is configured in Hub.
- Use `Tools/open-automated-editor.ps1` for agent-controlled Editor launches once that wrapper is present. Do not launch `Unity.exe` directly for agent work.
- Do not use `unity open` for agent-controlled Editor launch unless the installed CLI help explicitly proves a supported way to forward the required `-automated` Editor argument. Otherwise use the repository wrapper.
- `-batchmode` is not a substitute for the interactive automation-capable Editor used by Pipeline/live agent workflows.
- Before using Pipeline or live Editor control, verify the exact project path, pinned Unity version, Editor process, and presence of `-automated`. `Tools/get-unity-editor-state.ps1 -RequireAutomated` is the repository-level process check once available.
- If the target project is already open in an Editor that lacks `-automated`, do not attach agent-driven Editor control to that process. Do not silently terminate or restart a user-owned Editor. Report the condition and stop unless the user explicitly authorizes the required restart.
- Human/manual Editor sessions may omit `-automated`; this rule applies specifically to an Editor instance used by the implementation agent.
- A task may not claim successful live Editor, Play Mode, scene, prefab, layout, or interaction validation when the required automation-capable Editor instance was unavailable.

Editor readiness and compilation state must be reported unambiguously. Prefer structured fields such as `editorReady=true`, `compilation.active=false`, and `compileErrors=0` (or equivalent explicit wording). Do not summarize `compilation.active=false` as the ambiguous phrase “not compiling” without separately reporting whether compile errors exist.

### 4.2 Bounded waiting and progress reporting

Waiting for Unity startup, import, compilation, domain reload, test execution, or Pipeline connection must use bounded polling with a task-appropriate timeout.

- Poll internally; do not emit repeated unchanged chat/status messages such as “M1D remains active.”
- Report meaningful state changes, the final successful state, or a concrete timeout/failure.
- A retry must be tied to observable evidence such as process appearance, Pipeline connection, compilation transition, test completion, or command exit status.
- If the timeout expires or repeated checks provide no actionable progress, diagnose the last observed state and stop with the concrete blocker rather than continuing an unproductive loop.
- Never treat repeated status output as progress or as a substitute for repository/tool evidence.

## 5. Prefer safe Unity authoring paths

Prefer, in order when practical:

1. normal C# source/content edits for source-controlled data;
2. project-owned Editor tooling or Pipeline commands for scene/prefab/asset operations;
3. Unity Editor APIs invoked through a bounded diagnostic/authoring command;
4. direct serialized `.unity` / `.prefab` / `.asset` YAML editing only when there is a specific reason, the format is understood, and the result is immediately validated in Unity.

Never edit or fabricate `.meta` GUIDs casually. Preserve asset identity. Moving/renaming Unity assets must preserve their associated `.meta` files or be performed through Unity-aware tooling.

Generated/cache directories such as `Library/`, `Temp/`, `Logs/`, `obj/`, and build output are not source and must not be treated as authoritative project state.

## 6. Deterministic engine boundary

The authoritative Bloomdrawn rules engine is presentation-independent.

Authoritative gameplay must not depend on:

- `GameObject`, `MonoBehaviour`, scene hierarchy, prefab state, Animator state, or frame timing;
- `UnityEngine.Random` or any unseeded randomness;
- `Time.time`, wall-clock time, coroutines, animation completion, or Update order;
- pointer position, Canvas coordinates, camera state, VFX, audio, or presentation-only state;
- named production character/card/enemy branches in generic engine systems.

The intended flow is:

```text
input / gesture
  -> UI-only interaction state
    -> complete validated Game Command
      -> deterministic engine transition
        -> authoritative state + ordered Game Events
          -> application/session adapter
            -> presentation tokens
              -> Unity actors / UI / VFX / audio
```

Presentation may delay what the player sees. It may never delay, reorder, recompute, or redefine authoritative results.

If a gameplay change appears to require a Unity component or scene object inside the engine, re-check the architecture before proceeding.

## 7. Content and hardcoding rules

Production gameplay content is schema-authored and registry-driven.

Forbidden examples include:

- engine or presentation branches keyed to a named production character/card/enemy ID;
- hand-built production encounters hidden in scene objects;
- production reward, banner, Trial, Shop, equipment, growth, or progression tables embedded in code;
- a prefab or scene becoming the only authoritative definition of gameplay behavior;
- special-case UI logic for one authored card when a generic operation/tag/targeting contract should express it.

Dispatching on stable generic kinds—operation kinds, target kinds, status classes, node types, presentation token kinds—is allowed.

### Fixtures

Non-production fixtures are allowed only where the implementation plan permits them. They must:

- live in explicit fixture/test namespaces or registries;
- travel through production-shaped code paths;
- never require engine/UI branches on fixture IDs;
- never silently enter normal player profiles, saves, banners, rosters, rewards, or release content;
- be retired or isolated when the owning milestone requires cutover.

Do not solve a missing production system by hardcoding a temporary production-shaped placeholder.

## 8. Generated art is allowed

AI/generated artwork is permitted at every development stage and may be final release-quality art.

Generation method is provenance, **not** placeholder status.

Treat generated assets exactly like other art assets with respect to:

- visual quality;
- character continuity and correct design details;
- gameplay-scale readability;
- transparent backgrounds/layer separation where required;
- targetable actor separation;
- asset naming and logical presentation IDs;
- content-warning metadata where relevant;
- technical import settings and performance.

Do not replace or downgrade an asset merely because it was generated. Do not accept an asset merely because generation produced something usable-looking.

## 9. Combat presentation invariants

The combat scene is especially sensitive to layout and interaction regressions. Treat these as structural requirements, not optional polish.

### Battlefield actors

- Each party member is an independent presentation actor.
- Each enemy is an independent presentation actor and targetable entity.
- Do not flatten an entire party or enemy formation into one composite render.
- Each actor must use the generic actor/presentation contracts defined by the current milestone rather than named-character code branches.
- Actor visuals, selection/target anchors, status/UI anchors, VFX anchors, and interaction regions must remain distinct where the active task requires them.

### Hand and card feel

The ordinary hand is bottom-centred and fans around a stable centre point. It must read as a conventional roguelike-deckbuilder hand.

- Resting position/rotation/overlap/depth are recalculated from authoritative hand order; dragged transforms never become new resting positions.
- Hover/focus may lift and scale a card without changing authoritative hand order.
- A dragged card enters a dedicated interaction layer while preserving correct pointer-to-card positioning.
- Screen/local/world coordinate spaces must not be mixed. Use the approved Canvas and tested coordinate conversion path.
- Dragging upward across the defined Play Area threshold **arms** the card and presents a clear non-colour-only ready-to-play cue.
- Moving the card back below the threshold disarms it.
- Releasing while disarmed cancels and returns the card to the recalculated hand fan.
- Releasing while armed immediately resolves cards that need no explicit target.
- Releasing while armed on a card that needs an explicit target transitions to target-selection state; the card remains visibly staged and the player then selects a legal target.
- Cancelling target selection returns the card to the hand with no gameplay cost.
- Hovering, dragging, arming, cancelling, and choosing a target are presentation state only. Mana, RNG, piles, resources, and gameplay events change only when the complete command is accepted by the engine.
- Card views must never drift cumulatively, duplicate during reparenting, jump because of coordinate conversion, or become irrecoverably off-screen.

When modifying combat layout/card interaction, validate at all aspect ratios required by the active task rather than one convenient Game view size.

## 10. Testing and verification

Use the smallest test set that proves the active change, then run the broader gate required by the task plan.

Expected categories include as applicable:

- pure engine/unit tests;
- deterministic golden/replay tests;
- property/invariant tests;
- content/schema validation;
- Edit Mode tests;
- Play Mode tests;
- scene/layout validation commands;
- manual or computer-use interaction checks when visual/gesture behavior cannot be proven headlessly.

For interaction-sensitive work, tests should cover cancellation and rejection paths, not only successful play.

A task is not complete until:

- Unity compiles/imports successfully;
- required tests/validators pass;
- the task's exit criteria are satisfied;
- relevant deterministic behavior remains deterministic;
- presentation changes are checked in the actual Unity runtime/Editor where required;
- no out-of-scope files or systems were changed without justification.

Do not weaken, delete, skip, or rewrite a failing test merely to make the gate green. If a test is wrong relative to the approved design, raise the conflict explicitly.

## 11. Git and change hygiene

Before editing, inspect repository state. Do not overwrite unrelated user changes.

Keep diffs task-focused. Do not mass-reformat unrelated files, regenerate assets unnecessarily, or commit caches/build products.

Do not create commits, tags, branches, push, reset, rebase, clean, or discard changes unless the user or active task explicitly authorizes that Git action.

After implementation, summarize:

- task completed;
- behavior implemented;
- files changed;
- tests/validation executed and results;
- remaining concerns or approved follow-ups;
- any source-of-truth ambiguity discovered.

Update `worklog.md` only according to the active task plan's Worklog Entry Requirements.

## 12. Documentation and dependency changes

Do not edit `docs/DESIGN.md`, `plans/implementation_plan.md`, approved decision records, or `AGENTS.md` as a side effect of an implementation task unless that task explicitly authorizes documentation/governance changes.

Before adding or upgrading a Unity package or other production dependency:

- confirm it is in scope;
- confirm the pinned Unity 6.5/project package compatibility;
- prefer Unity first-party functionality where it adequately satisfies the requirement;
- record the reason when the dependency materially changes architecture or production workflow.

When Unity or Unity CLI behavior is uncertain, consult current official documentation rather than guessing.
