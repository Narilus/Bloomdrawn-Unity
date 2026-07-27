# Task M1R - Runtime Integration Recovery

## Objective

Repair the ordinary Player/Editor Play path for the committed M1 `CombatStage` without changing Engine rules, production content, M2 scope, or persistence.

## Prerequisites

- Completed M0B/M0F and M1A through M1J contracts.
- Project-owner approval of generated fixture-only runtime content delivery Option 1.
- Existing Engine -> Application -> Presentation dependency direction and DD-27/DD-28 contracts.

## In Scope

- Durable `CardDragLayer` script identity and committed-scene missing-behaviour validation.
- Editor-generated fixture-only runtime registry artifact, Application loader/flow, and automatic ordinary Play bootstrap.
- Orthographic URP 2D camera; generic fixture/fallback actors, HUD, hand/card views, target affordances, End Turn, and token progression.
- Real pointer, click, keyboard, drag, target, cancel, rejection, and sequential enemy presentation paths through the existing M1 contracts.
- Exact committed-scene ordinary-launch regression coverage and all M1 aspect-ratio gates.

## Non-Goals

- Engine rules, YAML runtime parsing, production registries/content/assets, M2 systems/previews, saves, or new gameplay commands.
- Test-only session injection, fixture CLI injection, direct presentation driving, or scene/UI authority over rules.

## Source Documents To Inspect

- `AGENTS.md` sections 1-12 and the Bloomdrawn Unity skill/references.
- `docs/DESIGN.md` sections 8, 15.1-15.5, DD-27, and DD-28.
- `plans/implementation_plan.md` M0B, M1G-M1J, and M1 exit criteria.
- M0B/M0F and M1G-M1J task plans and the approved runtime content delivery decision.

## Public Contract Changes

- Application exposes a fixture-runtime artifact loader and a read-only combat runtime flow that delegates all resolution to `CombatSession`/Engine.
- Presentation exposes scene bootstrap, runtime HUD/hand/card/target views, and scene validation contracts. UI submits only complete existing commands.
- Editor tooling exposes a non-mutating committed-scene validator through the existing Pipeline architecture.

## Schema or Content Changes

- Add one fixture-only YAML launch manifest and one reproducible generated JSON runtime artifact. The artifact records fixture origin, schema version, canonical content hash, validated definitions, setup request, and named seeds.
- No production registry, save payload, or production asset binding is introduced.

## Implementation Steps

1. Add/import/validate the fixture launch manifest and generate the committed runtime JSON artifact from the existing canonical fixture import path.
2. Add Application artifact validation, registry-derived setup/session construction, and automatic enemy-phase flow after presentation completion.
3. Move `CardDragLayer` into its own source asset; rebuild the scene through `CombatStageAuthoring`, never YAML editing or fabricated metas.
4. Add the camera, generic fallback visuals, uGUI/TMP HUD, hand/card views, actor targets, End Turn, and bootstrap bindings to the single screen-space Canvas scene.
5. Route pointer/click/keyboard interaction through `CardInteractionController`, `CardDragLayer`, Application flow, and ordered token completion only.
6. Add committed-scene, ordinary-launch, public-input terminal, Console, missing-script, and three-aspect-ratio tests.

## Required Tests

- Direct committed-scene Edit Mode validation finds no null behaviours, exactly one Canvas/EventSystem/Main Camera, durable `CardDragLayer`, and fallback references.
- Play Mode loads `CombatStage` with no injection/direct session/presenter calls; automatic registry-derived bootstrap creates visible actors/HUD/hand and reports no unexpected Console errors.
- Virtual mouse/keyboard uses the real EventSystem path to play cards, select targets, End Turn, and reach terminal state.
- DD-28 cancellation/rejection/drift/duplicate/off-screen/click-keyboard parity and 16:9, 16:10, 3440x1440 gates pass against runtime views.
- Golden replay and Editor fixture commands remain deterministic/tooling evidence only.

## Validation Commands

- Discover `unity --help`, `unity pipeline list`, `unity command --help`, and command discovery.
- Verify the pinned automated Editor and bounded readiness/compilation status.
- Run focused M1R Edit/Play tests, `Tools/validate.ps1`, complete Edit/Play suites, `bloom.health`, `bloom.validate-content`, existing fixture/layout commands, committed-scene validation, and `Tools/build-smoke.ps1`.
- Open the committed scene and press Play normally at all required aspect ratios.

## Exit Criteria

- The ordinary committed `CombatStage` starts a registry-derived fixture combat without test/tool injection and has no missing behaviours or unexpected Console errors.
- Real UI input completes a combat through the existing command/event/token path; UI/scene state never resolves gameplay.
- Generic non-production fallback presentation is visible and independently targetable at every required aspect ratio.
- No M2, production, persistence, or Engine-boundary change is introduced.

## Implementation Discretion

- Private helper names, fallback colours/chrome, prefab hierarchy, and presentation-only interpolation may vary while preserving the declared contracts and one-Canvas policy.

## Stop Conditions

- Runtime YAML/Editor dependency, fixture leakage into production, scene/UI-owned rules, DD-27/DD-28 change, M2 preview/content requirement, or save/schema change.

## Worklog Entry Requirements

- Record the original scene failure, artifact hash/identity, ordinary-launch/session/UI evidence, aspect-ratio evidence, no-production/M2/save impact, and all validation results.
- Commit expectation: one task-scoped commit after all validation passes.
