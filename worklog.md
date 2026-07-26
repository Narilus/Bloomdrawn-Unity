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
