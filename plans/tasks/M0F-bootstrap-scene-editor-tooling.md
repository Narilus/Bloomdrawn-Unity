# Task M0F - Bootstrap Scene and Agent/Editor Tooling

## Objective

Create the minimal developer/user bootstrap shell and prove a development-only Unity CLI/Pipeline feedback loop without making gameplay depend on it or building a production menu.

## In Scope

- Create a minimal bootstrap/dev scene with content-validation status, reduced-motion seed setting, and developer status access.
- Configure the already-installed `com.unity.pipeline` package for development only and keep Pipeline runtime components out of release Player configuration.
- Add `Bloomdrawn.Editor` commands `bloom.health`, `bloom.validate-content`, and `bloom.scene-summary` with concise, structured output where practical.
- Add typed presentation-layer `PresentationAssetCatalog` contract mapping logical presentation IDs to Unity asset bindings. It must support character/enemy/card/background roles without changing deterministic content.
- Add Editor validation for duplicate catalogue IDs, wrong role/type bindings, and unresolved content references marked required for the current milestone.
- Demonstrate Editor query through Pipeline without Inspector copy/paste.

## Non-Goals

- A production menu, combat scene, actor/card UI, gameplay content, input gesture behavior, animation/VFX/audio, or M1 presentation systems.
- Pipeline/CLI as a runtime or gameplay dependency.
- Production art binding beyond generic UI/fallback entries permitted by the catalogue contract.

## Source Documents To Inspect

- `AGENTS.md` sections 1, 3-5, 7-10, and 12.
- `docs/DESIGN.md` sections 23 and DD-27/DD-29 architecture and generated-art gates.
- `plans/design-decisions.md` DD-27 and DD-29.
- `plans/implementation_plan.md` sections 0.1, 8, Task M0F, Appendix E-F.
- Completed M0A wrappers/assemblies, M0B content validation and logical asset references, and Unity CLI help discovered from the installed tool.
- `.agents/skills/bloomdrawn-unity/references/unity-cli-pipeline.md` and `unity-authoring.md`.

## Public Contract Changes

- `bloom.health`, `bloom.validate-content`, and `bloom.scene-summary` are development/Editor commands with clear success/failure semantics; they cannot become gameplay dependencies.
- `PresentationAssetCatalog` exposes typed logical-ID-to-Unity-binding records and validation results; it never redefines content, rules, or asset IDs.
- Reduced-motion seed state is a bootstrap settings contract only; it does not add M1 playback behavior.

## Schema or Content Changes

- Add generic/fallback catalogue entries only if necessary to validate the contract.
- Content records may mark a logical reference required for the current milestone, but no production content/art is authored and missing future-milestone bindings are not treated as M0 failures.

## Implementation Steps

1. Read installed `unity --help`, `unity pipeline list`, `unity command --help`, and command discovery; use current syntax rather than remembered examples.
2. Implement/open the minimal bootstrap scene using uGUI/TMP only as a developer shell. Expose validation result, reduced-motion seed toggle/state, and diagnostic access without fake production navigation.
3. Add Editor-only command types and structured health/validation/scene summaries. Health reports project/editor version, compilation/import status, and registry status.
4. Add the typed catalogue and its validation rules; test duplicate, wrong-type/role, and unresolved-required conditions with isolated Editor fixtures.
5. Extend validation wrappers to invoke the direct/project command path appropriate to the installed CLI/Pipeline version and propagate failures.
6. Verify Pipeline connection and command queries against the explicit project path, then verify bootstrap Play Mode.

## Required Tests

- Bootstrap scene opens and enters Play Mode.
- Health command identifies project/editor version and registry status.
- Content validation command reports both success and a controlled invalid-content failure correctly.
- Catalogue validation detects duplicate IDs, wrong asset role/type, and unresolved required bindings.
- Editor commands are queryable through Pipeline without Inspector copy/paste.

## Validation Commands

```powershell
unity --help
unity pipeline list
unity command --help
unity command --project-path .
Tools/validate.ps1
unity test --project-path . --test-mode EditMode
unity test --project-path . --test-mode PlayMode
unity command --project-path . bloom.health
unity command --project-path . bloom.validate-content
unity command --project-path . bloom.scene-summary
```

## Exit Criteria

- `Tools/validate.ps1`, Edit Mode smoke tests, Play Mode smoke tests, and `bloom.health` succeed.
- The bootstrap scene is a developer shell, not a fake production menu.
- Pipeline/CLI failure or churn cannot invalidate deterministic engine architecture or release runtime behavior.

## Worklog Entry Requirements

- Record scene purpose, command names/output guarantees, Pipeline version/discovery evidence, catalogue validation coverage, and bootstrap Play Mode result.
- Record that the Pipeline package is development-only and no production gameplay content/assets or M1 interaction systems were introduced.
