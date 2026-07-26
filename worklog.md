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
