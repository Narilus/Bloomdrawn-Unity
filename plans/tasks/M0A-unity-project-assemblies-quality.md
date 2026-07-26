# Task M0A - Unity Project, Assemblies, and Quality Contract

## Objective

Establish the production-shaped Unity 6.5 workspace and dependency gates that every later milestone depends on. This task creates infrastructure only; it does not implement gameplay, production content, or M1 presentation.

## In Scope

- Pin the exact Unity `6000.5.x` Editor version already used by the project and retain the Windows development/validation baseline.
- Configure the existing URP project for the 2D Renderer and establish the baseline Windows build profile/settings.
- Retain only the approved initial first-party packages: URP 2D, uGUI/TMP, Input System, Test Framework, and development-only Pipeline tooling.
- Create the assembly definition graph: `Bloomdrawn.Engine` and deterministic runtime-content assemblies with **No Engine References**; application, presentation, Editor, and test assemblies depend in the permitted direction only.
- Add empty but production-shaped source roots, an Edit Mode smoke test, a Play Mode smoke test, and bootstrap scene support needed by the smoke build.
- Add Unity ignore and LFS policy appropriate to generated directories and future binary art/audio without adding binary assets.
- Add `Tools/validate.ps1` and `Tools/build-smoke.ps1` wrapper skeletons with explicit failures when their prerequisites are absent.
- Keep agent guidance separate from design documentation and record that installed Unity CLI help is authoritative.

## Non-Goals

- Content schemas, sample content, content import, runtime registry generation, RNG, command/event contracts, persistence, or Editor project commands.
- Production assets, production gameplay data, a menu hierarchy, combat actors, cards, or input interaction.
- Any M1 feature or third-party dependency.

## Source Documents To Inspect

- `AGENTS.md` sections 1-5 and 10-12.
- `docs/DESIGN.md` sections 22-23 and the approved DD-27 architecture gate.
- `plans/design-decisions.md` DD-27 and DD-14 status.
- `plans/implementation_plan.md` sections 0, 0.1, 3, Appendix E-F, and Task M0A.
- `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, and existing render/input settings.
- `.agents/skills/bloomdrawn-unity/SKILL.md` and `references/unity-cli-pipeline.md`.

## Public Contract Changes

- Assembly names and allowed reference directions become the repository contract.
- `Bloomdrawn.Engine` and deterministic runtime-content code must compile with `noEngineReferences: true` and must not reference Unity assemblies, scenes, or presentation/persistence code.
- `Tools/validate.ps1` and `Tools/build-smoke.ps1` become stable project validation entrypoints; later tasks may extend but not silently weaken them.

## Schema or Content Changes

None. Directory roots are not content and no production/fixture records are authored in this task.

## Implementation Steps

1. Inspect the current Editor version, manifest, URP renderer, build settings, scene, and Git state; do not recreate already-correct bootstrap assets.
2. Create the source/test directory structure and assembly definitions. Enforce one-way dependencies: pure engine/content -> application -> presentation; Editor/test assemblies may reference the layers they validate but player assemblies may not reference Editor code.
3. Add minimal compile-safe smoke types and one Edit Mode plus one Play Mode discovery test in their respective test assemblies.
4. Configure/verify URP 2D, Input System, TMP/uGUI, Windows development settings, and the bootstrap scene. Do not change to another UI/input/rendering stack.
5. Add focused PowerShell validation/build wrappers. They must locate the pinned Editor or return a clear failure, run the scoped checks, and propagate nonzero exit codes.
6. Verify package, ignore/LFS, and agent-guidance files are task-focused. Do not generate caches, solutions, builds, or Library artifacts for source control.

## Required Tests

- The pinned Editor opens/imports without compile errors.
- A deliberately Unity-dependent compile probe cannot be placed in `Bloomdrawn.Engine` or deterministic runtime-content assemblies; the enforced asmdef setting and dependency audit prove the boundary without committing intentionally uncompilable code.
- Edit Mode and Play Mode test assemblies each discover and execute at least one smoke test.
- The Windows smoke build starts the bootstrap scene.

## Validation Commands

```powershell
unity --help
unity pipeline list
Tools/validate.ps1
Tools/build-smoke.ps1
unity test --project-path . --test-mode EditMode
unity test --project-path . --test-mode PlayMode
```

Use installed CLI help to adjust invocation only when the installed syntax differs; preserve equivalent checks and capture exit codes.

## Exit Criteria

- `Tools/validate.ps1` and `Tools/build-smoke.ps1` succeed.
- The pinned Unity version, assembly dependency rules, package baseline, and validation wrappers are committed.
- Engine has no Unity, presentation, persistence, or Editor implementation dependency.
- No M0B+ contract or production content has been added.

## Worklog Entry Requirements

- Record the exact Editor version, package/configuration changes, assembly graph, wrapper commands, and smoke-test results.
- State that save schema, content schema/version, gameplay contracts, and assets are unaffected.
- Include any unavailable Unity validation and why; do not call the task complete until all required checks are available and pass.
