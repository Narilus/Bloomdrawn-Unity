# Bloomdrawn Worklog

This file is the append-only implementation journal for the Unity project. It records integrated work, validation evidence, approved deviations, and save/schema/content-version impact.

The file intentionally begins with no implementation-history entries. Repository state is established by the current files, Git history, tests, and future entries rather than by inherited completion claims.

## Protocol

- The orchestrating agent writes the authoritative entry after integration and final validation for a task or explicitly logged planning activity.
- Subagents return handoff reports; they do not concurrently edit this file.
- Add entries newest-last.
- Never rewrite earlier entries to conceal an error, failed attempt, revert, or later-discovered defect.
- Record unavailable or skipped checks explicitly.
- Tie each implementation entry to one task ID and its active task-plan file.
- Record design-decision references when they materially govern the task.
- Record any save-schema, content-schema, content-version, engine-contract, Unity project-version, package, or asset-catalogue impact.
- Record changes to generated asset provenance/readiness where relevant.
- A task is not complete until its required validation has passed and its worklog entry exists.
- A blocked or partial task may be logged, but must not be represented as complete.
- Worklog entries report what happened; they do not change `docs/DESIGN.md`, `plans/design-decisions.md`, or `plans/implementation_plan.md`.

## Entry Template

```markdown
## YYYY-MM-DD - Task <ID>: <Title>

**Status:** complete | partial | blocked | reverted
**Integrator:**
**Contributors/subagents:** none | <names/roles>
**Design references:** `docs/DESIGN.md` section(s), DD-XX as applicable
**Plan/task file:** `plans/tasks/<file>.md`
**Commit:** <hash or pending>

### Outcome

Concise statement of the behaviour/capability actually established.

### Files and assets changed

- source/config/content files
- scenes/prefabs/assets where relevant
- generated files only when they are intentionally tracked

### Tests added or updated

- Edit Mode tests
- Play Mode tests
- pure engine/unit/property/golden tests
- content/schema tests
- other task-specific automated coverage

### Validation performed

| Command/check | Result | Notes |
|---|---|---|
| Unity compile/import status | pass/fail | |
| Edit Mode tests | pass/fail/not required | |
| Play Mode tests | pass/fail/not required | |
| CLI/Pipeline project health | pass/fail/not required | |
| Scene/layout/interaction validation | pass/fail/not required | |
| Build validation | pass/fail/not required | |
| Task-specific checks | pass/fail | |

### Skipped or unavailable validation

State `None` when all required checks ran. Otherwise list exactly what was not run and why.

### Decisions, assumptions, and deviations

- implementation assumptions that stayed within the approved contract
- approved deviations from the task plan
- unresolved specification conflict, if the task is blocked

Do not silently turn an implementation workaround into a design decision.

### Unity/project/package impact

Record changes to:
- `ProjectSettings/ProjectVersion.txt`
- Unity packages
- render/input/UI configuration
- assembly boundaries
- build profiles
- editor/CLI tooling

State `None` when unaffected.

### Save, schema, migration, and content-version impact

Record:
- save schema/version
- content schema/version
- engine/replay compatibility
- migration requirements
- fixture compatibility/retirement

State `None` when unaffected.

### Asset/provenance impact

Record imported, generated, replaced, or reclassified assets when relevant. Generated art is permitted; distinguish generation provenance from placeholder/review/readiness status.

State `None` when unaffected.

### Known follow-up

Only concrete follow-up that remains within the approved plan. Do not use this section to authorize future-scope implementation.

### Integration notes

Anything a later task or reviewer needs to know to reproduce or audit this result.
```

## 2026-07-26 - Task M0A: Unity Project, Assemblies, and Quality Contract

**Status:** complete
**Integrator:** Narilus
**Contributors/subagents:** none
**Design references:** `docs/DESIGN.md` section 23; DD-14 and DD-27
**Plan/task file:** `plans/tasks/M0A-unity-project-assemblies-quality.md`
**Commit:** pending

### Outcome

Established the Unity 6000.5.5f1 foundation: URP 2D/Input/Test/Pipeline project baseline, explicit pure engine/content/application assemblies, Unity presentation and Editor boundaries, Edit/Play smoke tests, and reproducible validation/build entrypoints. The existing SampleScene remains the bootstrap scene for the Windows smoke Player.

### Files and assets changed

- Unity project/package/settings baseline retained from the validated bootstrap.
- `Assets/Bloomdrawn/` assembly definitions, assembly markers, Editor-only Windows smoke-build entrypoint, and generated Unity `.meta` files.
- `Assets/Bloomdrawn/Tests/` Edit Mode assembly-boundary and Play Mode bootstrap-scene smoke tests.
- `Tools/validate.ps1` and `Tools/build-smoke.ps1`.
- Repository governance/bootstrap files provided with the project, including `AGENTS.md`, `.agents/`, design/decision/implementation documentation, and this worklog.

### Tests added or updated

- Edit Mode: `AssemblyBoundarySmokeTests.PureAssemblies_DoNotReferenceUnityOrEditorAssemblies`.
- Play Mode: `BootstrapSceneSmokeTests.BootstrapScene_EntersPlayMode`.
- Windows Player smoke build uses `Bloomdrawn.Editor.Build.BloomdrawnBuild.PerformWindowsSmokeBuild`.

### Validation performed

| Command/check | Result | Notes |
|---|---|---|
| Unity compile/import status | pass | Unity Pipeline console reported no errors after importing M0A assemblies. |
| Edit Mode tests | pass | `Tools/validate.ps1` invoked the installed Unity CLI in an isolated batch Editor. |
| Play Mode tests | pass | `Tools/validate.ps1` invoked the installed Unity CLI in an isolated batch Editor. |
| CLI/Pipeline project health | pass | `unity --help`, `unity pipeline list`, `unity command --help`, and live Pipeline query confirmed Unity 6000.5.5f1 and Pipeline 0.4.0-exp.1. |
| Scene/layout/interaction validation | pass | Built Windows Player remained alive for five seconds after launch, confirming SampleScene bootstrap initialization. |
| Build validation | pass | `Tools/build-smoke.ps1` produced `Builds/Smoke/Bloomdrawn.exe`. |
| Task-specific checks | pass | Pure engine/content source search found no `UnityEngine` or `UnityEditor` reference; asmdefs enforce `noEngineReferences`. |

### Skipped or unavailable validation

None. The first validation attempt established that Unity CLI batch tests cannot run while the same project is open in an Editor. The clean Editor was closed for the isolated batch checks and restored afterwards.

### Decisions, assumptions, and deviations

- Reused the already-correct Unity 6.5 URP 2D bootstrap rather than recreating it.
- Smoke-build output uses ignored `Builds/Smoke` rather than Unity's transient `Temp` directory because Unity cleans `Temp` on batch shutdown.
- Pipeline remains development tooling only and is not referenced by deterministic/runtime code.

### Unity/project/package impact

- Unity Editor is pinned at `6000.5.5f1`.
- Existing first-party URP 2D, Input System, Test Framework, uGUI/TMP, and development-only Pipeline packages remain configured.
- Added the assembly graph: `Bloomdrawn.Engine` and `Bloomdrawn.Content` have No Engine References; `Bloomdrawn.Application` is also pure; presentation and Editor code depend only downward.

### Save, schema, migration, and content-version impact

None.

### Asset/provenance impact

None. No production or generated content asset was added.

### Known follow-up

- M0B introduces the schema/import/registry contract; M0A intentionally contains no content data.

### Integration notes

Run `Tools/validate.ps1` and `Tools/build-smoke.ps1` with the Unity project closed for batch validation. Use installed `unity --help` as the authoritative CLI syntax source.

## 2026-07-26 - Task M0B: Content Schema and Unity Import Foundation

**Status:** complete
**Integrator:** Narilus
**Contributors/subagents:** none
**Design references:** `docs/DESIGN.md` section 23; DD-13 and DD-29
**Plan/task file:** `plans/tasks/M0B-content-schema-unity-import.md`
**Commit:** pending

### Outcome

Established versioned, stable-ID content DTOs, explicit validation, deterministic registry ordering/hash generation, isolated sample YAML fixtures, and reproducible JSON registry output. YAML parsing is provided by YamlDotNet 18.1.0 in an Editor-only plugin/import assembly; player and deterministic assemblies never parse YAML.

### Files and assets changed

- `Assets/Bloomdrawn/Content/` pure DTO, validator, registry, and fingerprint contracts.
- `Assets/Bloomdrawn/Editor/Content/` Editor-only YAML/JSON import service and asmdef.
- `Assets/Plugins/Bloomdrawn/Editor/YamlDotNet/` YamlDotNet 18.1.0 netstandard2.0 binary and Unity metadata.
- `GameContent/` canonical production/fixture/generated locations and four isolated sample fixture records.
- `Assets/Bloomdrawn/Tests/EditMode/ContentImportTests.cs` and the Edit Mode assembly reference.
- `ThirdPartyNotices/YamlDotNet-NOTICE.md`.

### Tests added or updated

- Valid fixture YAML import, deterministic registry construction, and generated JSON roundtrip.
- Duplicate stable ID, missing version/display field, invalid cross-reference, and invalid logical presentation-reference rejection.
- Repeated canonical import hash/order equality.
- Empty production source and invalid unvalidated production data rejection.

### Validation performed

| Command/check | Result | Notes |
|---|---|---|
| Unity compile/import status | pass | M0B assemblies imported without Unity console errors. |
| Edit Mode tests | pass | `Tools/validate.ps1` includes M0B content/import tests. |
| Play Mode tests | pass | Existing M0A bootstrap smoke remains green through the project gate. |
| CLI/Pipeline project health | pass | M0A-installed Pipeline remains available when Editor is open; M0F owns `bloom.validate-content`. |
| Scene/layout/interaction validation | not required | M0B is pure content/tooling work. |
| Build validation | not required | M0A smoke build remains the scoped build gate; M0B changed no Player code path. |
| Task-specific checks | pass | Pure content sources contain no Unity/YAML/JSON importer dependency; parser is Editor-only. |

### Skipped or unavailable validation

`bloom.validate-content` is intentionally unavailable until M0F implements the project command. The direct importer is fully covered by Edit Mode tests and `Tools/validate.ps1`.

### Decisions, assumptions, and deviations

- Pinned YamlDotNet 18.1.0 (MIT, netstandard2.0) after checking its current NuGet package target and license; retained its notice in `ThirdPartyNotices/`.
- JSON output uses the existing Unity Newtonsoft JSON package from Editor-only code.
- Sample records use only `sample.*` fixture IDs and do not represent production content.

### Unity/project/package impact

- Added an Editor-only precompiled YamlDotNet plugin and `Bloomdrawn.Content.Editor` assembly.
- No player/runtime assembly references YamlDotNet or gains YAML parsing.

### Save, schema, migration, and content-version impact

- Introduced the M0 content DTO/version/hash contract only; no save schema/version or migration.
- Generated JSON remains derivative and is not a canonical gameplay source.

### Asset/provenance impact

None. Logical presentation IDs are validated data only; no Unity asset binding or production/generated art was added.

### Known follow-up

- M0F exposes validation through `bloom.validate-content`; M2A owns production content authoring.

### Integration notes

Canonical hand-authored definitions are YAML by DD-13. Runtime registry creation accepts only `ValidatedContent`; validation must occur before generation/use.

## 2026-07-26 - Task M0C: Deterministic RNG

**Status:** complete
**Integrator:** Narilus
**Contributors/subagents:** none
**Design references:** `docs/DESIGN.md` deterministic-engine requirements; DD-27 and DD-30
**Plan/task file:** `plans/tasks/M0C-deterministic-rng.md`
**Commit:** pending

### Outcome

Added pure SplitMix64 RNG state, the nine approved authoritative named streams, stable profile/run/ID seed derivation, and an explicit rejected-fixture seam. The Engine remains serializer-library agnostic; the approved Newtonsoft JSON roundtrip is tested only in the Edit Mode application-facing boundary.

### Files and assets changed

- `Assets/Bloomdrawn/Engine/Rng/` deterministic state, stream registry, derivation, and rejection fixture.
- `Assets/Bloomdrawn/Presentation/CosmeticRandom.cs` unsaved presentation-only randomness with no engine-state reference.
- Edit Mode RNG tests and test-only Newtonsoft reference.
- `plans/design-decisions.md` DD-30 serializer decision.

### Tests added or updated

- Same-seed sequence, stream isolation, cosmetic separation, rejected-command no-consumption, and Newtonsoft state continuation roundtrip.

### Validation performed

| Command/check | Result | Notes |
|---|---|---|
| Unity compile/import status | pass | Unity batch compilation succeeded. |
| Edit Mode tests | pass | `Tools/validate.ps1`, including RNG contract tests. |
| Play Mode tests | pass | Existing bootstrap smoke gate remained green. |
| Task-specific checks | pass | Engine source contains no `UnityEngine`, `UnityEditor`, or `Newtonsoft` reference. |

### Skipped or unavailable validation

None.

### Decisions, assumptions, and deviations

- DD-30, explicitly approved by the project owner, selects the already-installed Unity Newtonsoft JSON package for save-facing JSON only.
- M0E persistence responsibilities were not implemented or moved into M0C.

### Unity/project/package impact

No package change. The existing Newtonsoft package is referenced only by the Edit Mode test assembly.

### Save, schema, migration, and content-version impact

RNG state is JSON-roundtrip compatible. No save envelope, schema version, repository, migration, checksum, or filesystem persistence was added.

### Asset/provenance impact

None.

### Known follow-up

- M0E owns save envelope and repository implementation using DD-30's selected JSON representation.

### Integration notes

Authoritative streams: `combat.shuffle`, `combat.targeting`, `enemy.intent`, `map.layout`, `map.content`, `map.nodeModifiers`, `reward`, `shop`, and `gacha`. `profile.equipment` remains intentionally absent.

## 2026-07-26 - Task M0D: Engine Command/Event Protocol

**Status:** complete
**Integrator:** Narilus
**Contributors/subagents:** none
**Design references:** `docs/DESIGN.md` deterministic-engine/event-separation requirements; DD-27
**Plan/task file:** `plans/tasks/M0D-engine-command-event-protocol.md`
**Commit:** pending

### Outcome

Added pure reusable command-result, structured rejection-diagnostic, and ordered semantic-event contracts. A fixture-only fixed-seed smoke command advances minimal state on acceptance, returns the same state on rejection, and replays through a canonical golden fixture containing initial state, commands, expected events, and a SHA-256 checksum.

### Files and assets changed

- `Assets/Bloomdrawn/Engine/Commands/` pure command protocol, smoke fixture, and golden fixture runner.
- `Assets/Bloomdrawn/Tests/EditMode/CommandProtocolTests.cs` acceptance, rejection, ordered-event, fixed-seed replay, and checksum tests.

### Tests added or updated

- Accepted commands change state and emit explicit sequence-ordered semantic events.
- Rejected commands retain the original state and return a structured diagnostic with no events.
- Repeated fixed-seed golden replay verifies identical final state, event forms, and checksum without presentation inputs.

### Validation performed

| Command/check | Result | Notes |
|---|---|---|
| Unity compile/import status | pass | Unity batch compilation succeeded. |
| Project validation | pass | `Tools/validate.ps1` passed Edit Mode and Play Mode gates. |
| Edit Mode tests | pass | Installed CLI equivalent: `unity test . --mode EditMode --output Logs\\M0D-EditMode-results.xml`. |
| Task-specific checks | pass | Protocol source has no Unity, presentation, or serializer dependency. |

### Skipped or unavailable validation

None.

### Decisions, assumptions, and deviations

- The task-plan command's `--project-path` option is unsupported by the installed Unity CLI; the documented supported positional project argument was used instead.
- The fixture seed is stable test data only; no RNG semantics or production gameplay rule was introduced.

### Unity/project/package impact

No package or Unity-project configuration change.

### Save, schema, migration, and content-version impact

No production gameplay content schema, save migration, or persistence behavior was added.

### Asset/provenance impact

None.

### Known follow-up

- Later engine systems may use these generic contracts; M0D intentionally adds no command bus or gameplay rules.

### Integration notes

Event order is represented by explicit sequence values and the golden checksum uses canonical state/event data, never frame, render, scene, or presentation state.

## 2026-07-26 - Task M0E: Save Envelope and Repository Interfaces

**Status:** complete
**Integrator:** Narilus
**Contributors/subagents:** none
**Design references:** `docs/DESIGN.md` sections 15.7 and 16.3-16.5; DD-30
**Plan/task file:** `plans/tasks/M0E-save-envelope-repositories.md`
**Commit:** pending

### Outcome

Added the M0 versioned save envelope with lower-camel JSON metadata fields (`saveSchemaVersion`, `engineVersion`, `contentVersion`, `checksum`, `payload`), canonical SHA-256 validation, typed profile/run repository interfaces, an in-memory repository, and local-file repositories. The Unity-facing factory supplies `Application.persistentDataPath`; all validation and file behavior remain in the application persistence boundary.

### Files and assets changed

- `Assets/Bloomdrawn/Application/Persistence/` envelope codec, validation result/diagnostics, fixture payload DTOs, repository interfaces, in-memory repository, injected-path local-file repositories, and `.previous` fallback.
- `Assets/Bloomdrawn/Presentation/Persistence/UnityPersistentRepositories.cs` Unity path adapter/factory only.
- `Assets/Bloomdrawn/Tests/EditMode/SaveRepositoryTests.cs` envelope, compatibility, repository, local JSON, recovery, and RNG continuation coverage.
- `Bloomdrawn.Application.asmdef` JSON serializer reference for persistence only.

### Tests added or updated

- Envelope accepts matching metadata/checksum, exposes the specified JSON field name, rejects a checksum mismatch, and rejects an incompatible schema before returning a payload.
- In-memory repository saves and loads minimal profile/run fixture payloads.
- Local-file repository roundtrips the run payload through Newtonsoft JSON and continues `RngState` with identical subsequent output.
- A deliberately invalid primary snapshot recovers the valid `.previous` snapshot after temp-write/replace.

### Validation performed

| Command/check | Result | Notes |
|---|---|---|
| Unity compile/import status | pass | Unity batch compilation succeeded. |
| Project validation | pass | `Tools/validate.ps1` passed Edit Mode and Play Mode gates. |
| Edit Mode tests | pass | `unity test . --mode EditMode --output Logs\\M0E-EditMode-results.xml`. |
| Play Mode tests | pass | `unity test . --mode PlayMode --output Logs\\M0E-PlayMode-results.xml`. |
| Task-specific checks | pass | Application persistence source has no Unity object, scene, or presentation-state reference. |

### Skipped or unavailable validation

None.

### Decisions, assumptions, and deviations

- DD-30's already-installed Newtonsoft package is used only for M0E save-facing JSON; the engine remains serializer-library agnostic.
- The task-plan `--project-path` form is unsupported by the installed Unity CLI, so its documented positional-project equivalent was used.

### Unity/project/package impact

No package addition or upgrade. The existing Newtonsoft package is referenced by the application persistence assembly; `Application.persistentDataPath` is resolved only in the presentation-side Unity adapter.

### Save, schema, migration, and content-version impact

Introduced only M0 schema version 1 and minimal test profile/run payloads. No production content, future-system fields, migration, cloud sync, encryption, UI, scene path, instance ID, `UnityEngine.Object`, or presentation state is persisted.

### Asset/provenance impact

None.

### Known follow-up

- M4 owns production profile/run fields, checkpoint policy, save migrations, and broader recovery behavior.

### Integration notes

Consumers depend on `IProfileRepository`/`IRunRepository`; local storage uses an injected root path for tests and atomic replacement with a recoverable `.previous` snapshot for runtime use.

## 2026-07-26 - Task M0F: Bootstrap Scene and Agent/Editor Tooling

**Status:** complete
**Integrator:** Narilus
**Contributors/subagents:** none
**Design references:** `docs/DESIGN.md` sections 15.2 and 15.7; DD-27 and DD-29
**Plan/task file:** `plans/tasks/M0F-bootstrap-scene-editor-tooling.md`
**Commit:** pending

### Outcome

Converted the existing `SampleScene` into the intentionally minimal M0 developer shell: a uGUI Canvas with content-validation/developer-status text and a reduced-motion seed toggle. Added Editor-only Pipeline commands `bloom.health`, `bloom.validate-content`, and `bloom.scene-summary`, plus a typed presentation asset catalogue and validation contract.

### Files and assets changed

- `Assets/Scenes/SampleScene.unity` bootstrap developer shell authored through `BootstrapSceneAuthoring`, with an Input System-compatible EventSystem.
- `Assets/Bloomdrawn/Presentation/` bootstrap shell and typed logical presentation asset catalogue.
- `Assets/Bloomdrawn/Editor/Tooling/` Pipeline command handlers, scene authoring routine, and catalogue validator.
- M0 content DTO flag for current-milestone-required presentation bindings; no production content is marked required or authored.
- Edit and Play Mode coverage for tooling/catalogue and bootstrap scene behavior.

### Tests added or updated

- Health/content commands report valid fixture registry state; controlled invalid YAML causes the content command to fail.
- Catalogue validator detects duplicate IDs, logical-role mismatch, wrong Unity asset type, and unresolved required binding.
- Bootstrap scene loads in Play Mode, exposes its shell, and toggles its reduced-motion seed state.

### Validation performed

| Command/check | Result | Notes |
|---|---|---|
| CLI/Pipeline discovery | pass | `unity --help`, `unity pipeline list`, `unity command --help`, and live command discovery verified Pipeline `0.4.0-exp.1`. |
| Project validation | pass | `Tools/validate.ps1` passed Edit Mode and Play Mode gates. |
| Edit Mode tests | pass | `unity test . --mode EditMode --output Logs\\M0F-EditMode-results.xml`. |
| Play Mode tests | pass | `unity test . --mode PlayMode --output Logs\\M0F-PlayMode-results.xml`. |
| Pipeline commands | pass | Live `bloom.health`, `bloom.validate-content`, and `bloom.scene-summary` calls returned structured success; health reported Unity `6000.5.5f1`, valid registry, and four fixture definitions. |
| Bootstrap scene inspection | pass | Pipeline opened `Assets/Scenes/SampleScene.unity`; scene summary returned four roots and `IsDirty: false`. |

### Skipped or unavailable validation

None.

### Decisions, assumptions, and deviations

- The existing `SampleScene` is retained as the bootstrap scene rather than creating a second empty scene.
- Pipeline is used only in the Editor assembly and is absent from the runtime presentation/application/engine assembly graph.
- The bootstrap shell uses the project’s configured Input System UI module; no M1 card/input behavior was added.

### Unity/project/package impact

No package addition or upgrade. The already-installed Pipeline package remains an Editor development surface; no runtime Pipeline manager/component is added to the scene or Player configuration.

### Save, schema, migration, and content-version impact

Added only an opt-in current-milestone presentation-binding requirement flag to the content DTO. No production content, save data, migrations, or persistence behavior was introduced.

### Asset/provenance impact

No production art or asset binding. The catalogue is typed and empty by default, ready for later logical bindings without modifying deterministic content.

### Known follow-up

- M1 owns combat actors, cards, input gestures, production presentation bindings, and event presentation sequencing.

### Integration notes

Commands are static Editor-only `[CliCommand]` handlers with structured response objects. `bloom.validate-content` fails on invalid input; command/Pipeline churn cannot affect the deterministic Engine.

## 2026-07-26 - Task M1A: Fixture Party and Combat Setup

**Status:** complete
**Integrator:** Narilus
**Contributors/subagents:** none
**Design references:** `docs/DESIGN.md` sections 4, 5, 6.3, 14, 15.4, and 16; DD-01, DD-13, DD-27, and DD-29
**Plan/task file:** `plans/tasks/M1A-fixture-party-combat-setup.md`
**Commit:** pending

### Outcome

Added an isolated, registry-derived M1 combat fixture: four party members, owner-specific Strike/Shield definitions, an exact-four lineup, one enemy/encounter, deterministic eight-card deck recipe, and an initial attack intent fact. The pure Content setup contracts derive stable participant and enemy IDs from authored setup identity; they create neither runtime cards/piles nor an intent lifecycle.

### Files and assets changed

- `GameContent/fixtures/m1-combat/` fixture-only character, card, lineup, enemy, and encounter YAML definitions.
- Content definition, fingerprint, and validation extensions for the M1 fixture-combat family and its cross-reference/cardinality rules.
- Pure `FixtureCombatSetup` contracts and registry-derived setup factory in `Bloomdrawn.Content`.
- Focused Edit Mode fixture/setup validation coverage and Unity-generated `.meta` files for new C# sources.

### Tests added or updated

- Edit Mode `FixtureCombatSetupTests` cover valid exact-four setup, deterministic ID/deck/intent derivation, invalid owner/lineup/card diagnostics, production-origin rejection, and data-only stat changes.

### Validation performed

| Command/check | Result | Notes |
|---|---|---|
| CLI/Pipeline discovery | pass | Confirmed `unity --help`, `unity test --help`, `unity command --help`, and live command discovery before invocation. |
| Project validation | pass | `Tools\\validate.ps1` completed successfully. |
| Edit Mode tests | pass | `unity test . --mode EditMode --output Logs\\M1A-editmode.xml`: 23 passed, including 5 M1A tests. |
| Play Mode tests | pass | `unity test . --mode PlayMode --output Logs\\M1A-playmode.xml`: 1 passed. |
| CLI/Pipeline project health | pass | `bloom.validate-content` returned valid with 19 definitions; `bloom.health` reported Unity `6000.5.5f1`, not compiling, and a valid registry. |
| Scene/layout/interaction validation | not required | M1A has no presentation or interaction scope. |
| Build validation | not required | M1A changes pure content/setup only. |
| Task-specific checks | pass | Fixture-only origin, invalid references, deck ownership, lineup cardinality, and stable setup IDs are covered by Edit Mode tests. |

### Skipped or unavailable validation

None.

### Decisions, assumptions, and deviations

- Typed fixture setup remains in the pure Content assembly so it consumes the validated registry without creating an Engine-to-Content dependency before M1B.
- The setup result carries authored deck recipe entries only; M1C owns runtime card-instance and pile state, and M1F owns enemy-intent lifecycle behavior.

### Unity/project/package impact

None. Existing assembly direction and package set are unchanged.

### Save, schema, migration, and content-version impact

Added fixture-only content fields under the existing YAML content policy. No production content, save payload, migration, replay, or persistence change was introduced.

### Asset/provenance impact

None. No presentation binding or production art asset was added.

### Known follow-up

- M1B consumes the setup result for pure combat phase state; M1C introduces runtime card instances and piles; M1F evolves only the initial intent fact into lifecycle behavior.

### Integration notes

All fixture data travels through the normal importer/validator/registry path. Stable runtime IDs encode lineup/encounter IDs plus authored ordering and never use Unity identities or object names.

## 2026-07-26 - Task M1B: Combat State Machine

**Status:** complete
**Integrator:** Narilus
**Contributors/subagents:** none
**Design references:** `docs/DESIGN.md` sections 6.2 through 6.11 and 15.1 through 15.5; DD-01 and DD-27
**Plan/task file:** `plans/tasks/M1B-combat-state-machine.md`
**Commit:** pending

### Outcome

Added the pure deterministic M1 combat phase machine with its complete phase vocabulary, explicit legal-transition table, legal public command boundaries, ordered phase events, unchanged-state rejection, and terminal command rejection. The machine consumes only the validated M1A `CombatSetupResult`; it reaches `ENEMY_PHASE_START` after End Turn and leaves slots, action resolution, intent regeneration, and sequential presentation to M1F.

### Files and assets changed

- `Bloomdrawn.Engine` Assembly Definition now has the approved one-way reference to pure `Bloomdrawn.Content`.
- Pure Engine `CombatState`, `CombatPhase`, command, phase-rule, and state-machine contracts.
- Edit Mode state-machine tests plus an assembly-boundary assertion that Content does not reference Engine.

### Tests added or updated

- `CombatStateMachineTests` cover the full transition table, opening and End Turn advancement, unchanged-state illegal-command rejection, terminal rejection, and deterministic phase/event traces.
- `AssemblyBoundarySmokeTests` now proves the approved Engine-to-Content reference and forbids a reverse Content-to-Engine reference.

### Validation performed

| Command/check | Result | Notes |
|---|---|---|
| CLI/Pipeline discovery | pass | Ran `unity --help` and `unity command --help` before CLI validation. |
| Project validation | pass | `Tools\\validate.ps1` completed successfully. |
| Edit Mode tests | pass | `unity test . --mode EditMode --output Logs\\M1B-EditMode-results.xml`: 30 passed, including 7 M1B state-machine cases. |
| CLI/Pipeline project health | pass | `bloom.health` reported Unity `6000.5.5f1`, `IsCompiling: false`, valid fixture registry, and 19 definitions. |
| Scene/layout/interaction validation | not required | M1B is pure Engine behavior. |
| Build validation | not required | No Player-facing code or asset change. |
| Task-specific checks | pass | Transition rules, ordered events, terminal rejection, deterministic traces, no Unity reference, and one-way pure assembly direction are covered. |

### Skipped or unavailable validation

None.

### Decisions, assumptions, and deviations

- Per explicit project-owner approval, Engine may reference the pure, Unity-object-free Content assembly for validated runtime DTOs/setup data. Content retains no reverse Engine reference.
- M1B models phase boundaries only. It intentionally does not resolve cards, damage, enemy slots/actions, intent lifecycle, presentation, persistence, or preview behavior.

### Unity/project/package impact

Added only the approved Engine-to-Content Assembly Definition reference. `noEngineReferences` remains enabled and no Unity, package, scene, or Player configuration changed.

### Save, schema, migration, and content-version impact

None. M1B consumes existing M1A fixture setup without changing content schema or persistence.

### Asset/provenance impact

None.

### Known follow-up

- M1C adds runtime card instances/piles; M1F owns enemy slots, sequential action resolution, intent regeneration, and presentation-ready action metadata.

### Integration notes

The only legal M1B public commands are `BeginCombat` during `CombatSetup` and `EndTurn` during `PlayerAction`. Internal transitions emit semantic `combat.phase-entered` events with monotonically increasing sequence values; terminal transitions are modeled for M1E to invoke under DD-01 Atomic Stop.

## 2026-07-26 - Task M1C: Card Instances and Piles

**Status:** complete
**Integrator:** Narilus
**Contributors/subagents:** none
**Design references:** `docs/DESIGN.md` sections 5 and 6.4 through 6.7; DD-23, DD-27, and DD-28
**Plan/task file:** `plans/tasks/M1C-card-instances-piles.md`
**Commit:** pending

### Outcome

Added pure, stable owner-aware runtime card instances and Draw, Hand, Discard, Graveyard, and Resolving pile state for the M1 fixture deck. Card base cost is derived from the validated fixture definition; draw-to-five and deterministic reshuffle use only `combat.shuffle`.

### Files and assets changed

- Pure Engine card-instance, tags, pile state, movement, draw, and reshuffle contracts.
- M1A fixture setup projection now carries validated printed base cost into runtime deck construction.
- Edit Mode pile invariant coverage and required Unity metadata.

### Tests added or updated

- Stable card ID/owner construction, draw-to-five, reshuffle stream isolation, unchanged-state rejected movement, hand-target counting, and cross-pile identity preservation.

### Validation performed

| Command/check | Result | Notes |
|---|---|---|
| Project validation | pass | `Tools\\validate.ps1` completed successfully. |
| Edit Mode tests | pass | `unity test . --mode EditMode --output Logs\\M1C-EditMode-results.xml`: 33 passed. |
| CLI/Pipeline project health | pass | `bloom.health` reported not compiling and a valid 19-definition registry. |
| Visual/interaction/build validation | not required | M1C is pure Engine state only. |

### Skipped or unavailable validation

None.

### Decisions, assumptions, and deviations

- DD-23 metadata is inert: no copy, hidden-zone selection, generated-card effect, or production-card behavior exists.

### Unity/project/package impact

None.

### Save, schema, migration, and content-version impact

No persistence or production-content change.

### Asset/provenance impact

None.

### Known follow-up

- M1D owns card play, Mana, targeting validation, and cost calculation.

### Integration notes

Rejected movement returns the original state and does not consume RNG; discard reshuffles only when Draw cannot fulfill the hand-target request.

## 2026-07-26 - Governance Tooling Repair: Automated Unity Editor Workflow

**Status:** complete
**Integrator:** Narilus
**Contributors/subagents:** none
**Design references:** `AGENTS.md` sections 4.1 and 4.2; `.agents/skills/bloomdrawn-unity/SKILL.md` automated Editor workflow
**Commit:** pending

### Outcome

Established and verified the required automated-only agent Editor workflow. The repository now inspects target-project Unity processes, refuses unsafe non-automated attachment, launches the pinned Editor with `-automated`, waits with bounded retries, and reports unambiguous health evidence through the existing M0F Pipeline command.

### Files and assets changed

- `Tools/open-automated-editor.ps1` and `Tools/get-unity-editor-state.ps1` supplied workflow scripts, including installed-Editor discovery through Unity CLI.
- `AGENTS.md` and the Bloomdrawn Unity skill operational rules for automated launch and bounded waits.
- Existing M0F `bloom.health` result fields and Edit Mode coverage.

### Validation performed

| Command/check | Result | Notes |
|---|---|---|
| Process-state check | pass | Confirmed no pre-existing project Editor, then verified exactly one target-project process with `-automated`, no `-batchmode`, and pinned version `6000.5.5f1`. |
| Automated launcher | pass | `Tools\\open-automated-editor.ps1` resolved `D:\\Dev\\Unity\\Editor\\6000.5.5f1\\Editor\\Unity.exe` via Unity CLI and launched PID 30168 with `-projectPath` and `-automated`. |
| Pipeline/readiness | pass | Bounded readiness poll reached Pipeline `ready` for the intended project/PID. |
| Health/content commands | pass | `bloom.health` returned Pipeline/editor readiness, `CompilationActive:false`, `CompileFailed:false`, `CompileSucceeded:true`, valid registry, and 19 definitions; `bloom.validate-content` passed. |
| Edit Mode tests | pass | `unity test . --mode EditMode --output Logs\\ToolingRepair-EditMode-results.xml`: 33 passed. |
| Play Mode tests | pass | `unity test . --mode PlayMode --output Logs\\ToolingRepair-PlayMode-results.xml`: 1 passed. |
| Repository validation | pass | `Tools\\validate.ps1` completed successfully. |

### Known follow-up

- Resume M1D only after this repair is committed and the worktree is clean.

## 2026-07-26 - M1D: Mana and Card Play

**Status:** complete
**Integrator:** Narilus
**Contributors/subagents:** none
**Design references:** `docs/DESIGN.md` §§5.6–5.8, 6.5, 8.5, and 15.1; DD-27 and DD-28; `plans/tasks/M1D-mana-card-play.md`
**Commit:** pending

### Outcome

Added pure authoritative Mana state and complete `PlayCard` validation. Fixture-derived runtime cards now retain their validated target and operation metadata. An accepted play spends its zero-floored final cost, moves the card from Hand to Resolving, and emits one ordered semantic event; it deliberately does not resolve card effects, reserve partial targets, or represent any UI interaction state.

### Validation performed

| Command/check | Result | Notes |
|---|---|---|
| Project validation | pass | `Tools\\validate.ps1` completed successfully, including Edit and Play Mode suites. |
| M1D Edit Mode gate | pass | `unity test . --mode EditMode --output Logs\\M1D-EditMode-results.xml` exited successfully after the clean validation. |
| Automated Editor health | pass | PID 22392 was launched by `Tools\\open-automated-editor.ps1` with `-automated`; `bloom.health` reported Pipeline/editor ready, `CompilationActive:false`, `CompileFailed:false`, `CompileSucceeded:true`, and `RegistryValid:true` with 19 definitions. |

### Contract evidence

- Base Mana is six and final cost floors at zero.
- Phase, hand, owner, target completeness/legality, and Mana failures return the identical state with no events; rejected commands have no RNG input or consumption path.
- `party` cards require no explicit enemy choice; `oneEnemy` cards require exactly one encounter enemy ID.
- `CombatState` and the complete command contract contain no hover, drag, armed, staged-card, or target-selection fields.

### Known follow-up

- M1E owns effect resolution, damage, Shield, healing, and terminal outcome ordering for accepted resolving cards.

## 2026-07-26 - M1E: Damage, Shield, and Healing

**Status:** complete
**Integrator:** Narilus
**Design references:** `docs/DESIGN.md` §§6.10–6.11, 7.1–7.4, 15.4–15.6, and 17.2; DD-01 and DD-27; `plans/tasks/M1E-damage-shield-healing.md`
**Commit:** pending

### Outcome

Added pure shared party and per-enemy HP/Shield state, owner-stat fixture effect resolution, and Atomic Stop. Accepted M1 Strike and Shield cards now emit an ordered card-play event followed by their semantic result event; all atomic effects record source kind, owner, stable affected ID, Shield absorbed, and HP damage dealt. Defeat wins a simultaneous terminal checkpoint.

### Validation performed

| Command/check | Result |
|---|---|
| `Tools\\validate.ps1` | pass, including Edit and Play Mode suites |
| `unity test . --mode EditMode --output Logs\\M1E-EditMode-results.xml` | pass |
| automated `bloom.health` | pass: Pipeline/editor ready, compile idle and successful, registry valid (19 definitions) |

### Contract evidence

- Strike uses the owner Attack; Shield uses owner Defense.
- Healing caps at maximum HP; HP-loss is distinct from damage and bypasses Shield.
- Atomic Stop emits the terminal event and skips later ordinary effects; defeated party takes precedence when both terminal conditions are true at a checkpoint.
- No statuses, Domains, reactions, production effects, or presentation rule was introduced.

### Known follow-up

- M1F owns enemy intent lifecycle and sequential enemy actions using these pure effect results.

## 2026-07-26 - M1F: Enemy Intent and Sequential Actions

**Status:** complete
**Integrator:** Narilus
**Design references:** `docs/DESIGN.md` §§6.8–6.11, 7.8, 8.7, 15.4, 15.9, and 18.3; DD-01, DD-26, DD-27; `plans/tasks/M1F-enemy-intent-sequential-actions.md`
**Commit:** pending

### Outcome

Converted M1A’s fixture initial intent into immutable M1 runtime slot/intent state. Enemy actions now advance only through an explicit enemy-phase command, act one stable slot at a time, emit presentation-ready stable IDs/intent/sequence facts, interrupt immediately on terminal state, and regenerate visible intents only after the final enemy action.

### Validation performed

| Command/check | Result |
|---|---|
| `Tools\\validate.ps1` | pass, including Edit and Play Mode suites |
| `unity test . --mode EditMode --output Logs\\M1F-EditMode-results.xml` | pass |
| automated `bloom.health` | pass: Pipeline/editor ready, compile idle/successful, registry valid (19 definitions) |

### Contract evidence

- Fixture construction supplies initial data only; M1F exclusively owns runtime slot order, iteration, action resolution, and regeneration.
- Two-enemy test fixtures prove slot 0 then slot 1, never simultaneous action resolution.
- Defeat stops later slots and ordinary regeneration; invalid phase advancement is an unchanged-state rejection.
- Events contain stable slot/enemy/target/intent/sequence facts and no presentation object references.

### Known follow-up

- M1G supplies independent actor layout; M1I maps this ordered semantic event stream to presentation tokens.

## 2026-07-26 - M1G: Combat Stage and Independent Actor Views

**Status:** complete
**Integrator:** Narilus
**Design references:** `docs/DESIGN.md` §§8.3–8.10, 15.2, 15.5, and 15.9; DD-26–DD-29; `plans/tasks/M1G-combat-stage-independent-actors.md`
**Commit:** pending

### Outcome

Created the dedicated `CombatStage` scene through project-owned Editor authoring. It has one screen-space uGUI combat Canvas, independent generic party/enemy formations and actor roots, stable presentation-only runtime binding IDs, separate visual/target/selection/status/VFX/intent anchors, and explicit safe-zone containers.

### Validation performed

| Command/check | Result |
|---|---|
| `Tools\\validate.ps1` | pass, including Edit and Play Mode suites |
| M1G Edit/Play Mode commands | pass |
| scene hierarchy/Pipeline summary | pass: `CombatStage`, two roots, saved cleanly |
| automated `bloom.health` | pass: Pipeline/editor ready, compile idle/successful, registry valid (19 definitions) |

### Layout evidence

- Canvas uses `ScaleWithScreenSize`, `1920 x 1080`, and `0.5` width/height match.
- Four party actor roots and one enemy actor root are independently addressable; no composite actor root exists.
- Every actor owns visual, target, selection, status, VFX, and intent anchors.
- Tests assert safe-zone separation for shared survival, hand, enemy target lane, End Turn, and a collapsed/overlay combat log at 16:9, 16:10, and ultrawide reference ratios.

### Known follow-up

- M1H adds card fan/drag/target interaction using the declared Hand Safe Area; M1I supplies runtime token playback and fixture bindings.

## 2026-07-26 - M1H: Bottom-Centred Card Fan, Drag Play Area, and Targeting

**Status:** complete
**Integrator:** Narilus
**Design references:** `docs/DESIGN.md` §§5.6–5.8, 8.5, 8.9–8.10, and 15.1; DD-27 and DD-28; `plans/tasks/M1H-card-fan-drag-targeting.md`
**Commit:** pending

### Outcome

Added presentation-only deterministic hand fan geometry and a single-session card interaction controller. The combat scene now supplies an explicit Play Area and full-canvas drag layer. The reparenting path uses `RectTransformUtility.ScreenPointToLocalPointInRectangle`; interactions only submit complete string-ID command payloads to an external sink and never compute costs, legality, targets, damage, RNG, or previews.

### Validation performed

| Command/check | Result |
|---|---|
| focused HandInteraction Edit Mode tests | pass: 3/3 |
| `Tools\\validate.ps1` | pass, including Edit and Play Mode suites |
| M1H Edit/Play Mode commands | pass |
| automated `bloom.health` and `bloom.scene-summary` | pass: compilation idle/successful, valid registry, saved `CombatStage` |

### Interaction evidence

- Fan positions derive only from hand order/count/current layout inputs and remain bottom-centred.
- Below-threshold release cancels; upward arming and downward disarming are explicit interaction states.
- Target-required cards stage without submission until an enemy ID is chosen; cancel and rejected sink submission restore resting state without pre-acceptance mutation.
- The command sink receives complete card/owner/optional-enemy IDs only; no general preview evaluator or M2 gameplay capability was introduced.

### Known follow-up

- M1I owns the authoritative session adapter and maps accepted engine events to ordered presentation tokens.

## 2026-07-26 - M1I: Initial Sequential Presentation Adapter

**Status:** complete
**Integrator:** Narilus
**Design references:** `docs/DESIGN.md` §§15.1, 15.5, and 15.9; DD-27–DD-29; `plans/tasks/M1I-sequential-presentation-adapter.md`
**Commit:** pending

### Outcome

Added the permanent M1 application-owned `CombatSession` path. Accepted engine events map in sequence to immutable presentation tokens, lock input until explicit ordered completion, and leave authoritative state/event history unchanged during playback. The combat stage now binds its independent actor roots from setup-derived runtime IDs and uses generic fallback act, acknowledgement, hit, Shield, victory, and defeat reactions with reduced-motion and speed hooks.

### Validation performed

| Command/check | Result |
|---|---|
| focused M1I Edit Mode tests | pass: 4/4, including command invalid-precondition failure |
| focused M1I Play Mode test | pass: 1/1 |
| `Tools\\validate.ps1` | pass, including Edit and Play Mode suites |
| `unity test . --mode EditMode --output Logs\\M1I-EditMode-results.xml` | pass |
| `unity test . --mode PlayMode --output Logs\\M1I-PlayMode-results.xml` | pass |
| automated `bloom.health` | pass: Pipeline/editor ready, compilation idle/successful, registry valid (19 definitions) |
| `bloom.load-combat-fixture`, `bloom.dump-combat-state`, `bloom.validate-combat-layout`, `bloom.reset-combat-fixture` | pass: registry-derived setup, unchanged dump/reset state, 4 party + 1 enemy independent actor bindings |

### Contract evidence

- The Application session owns state submission and accepted-event history; Presentation receives facts/IDs only and cannot resolve or alter combat rules.
- Tokens preserve accepted event sequence and use setup-derived runtime IDs for actor lookup. Completion is explicit and input is locked only while accepted tokens remain pending.
- The fixture commands are Editor-only Pipeline diagnostics, have structured output, and reject an unloaded-session precondition without hidden initialization.
- Presentation components are in one-MonoBehaviour-per-file Unity script assets so serialized CombatStage references survive scene reloads.

### Known follow-up

- M1J owns the golden replay fixture and the complete real-scene interaction/replay exit gate; M1I adds no M2 preview or future presentation system.
