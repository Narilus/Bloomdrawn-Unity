# Task M1G - Combat Stage and Independent Actor Views

## Objective

Create the permanent generic Unity combat-stage architecture with independently addressable party/enemy actors and validated information safe zones.

## Prerequisites

- M1A stable participant/enemy IDs and fixture setup.
- M1F stable enemy slots/intent state and semantic events.
- M0F bootstrap scene, Input System EventSystem, PresentationAssetCatalog, and Editor/Pipeline tooling.

## In Scope

- Create the M1 combat scene/presentation skeleton using URP 2D, independent PartyFormationView, EnemyFormationView, CombatActorView roots, and generic fixture/fallback visuals.
- Give each actor separate visual, target/selection, UI/status, and VFX anchors.
- Create compact upper-left portrait/fixture-resource region, shared-survival lane, lower-left Mana region, enemy-intent anchors, bottom-centred hand safe area, End Turn control, and collapsed/minimal or overlay combat log.
- Use one screen-space uGUI combat Canvas with CanvasScaler.ScaleWithScreenSize, 1920x1080 reference resolution, and Match Width Or Height = 0.5.
- Add layout assertions at 16:9, 16:10, and one ultrawide reference resolution.

## Non-Goals

- M1H card fan/drag/target behavior, M1I token playback, production presentation bindings, M2 catalog expansion, production art, animation breadth, or preview UI.
- Composite party/enemy actor presentation, scene-owned gameplay data, or named fixture presentation branches.

## Source Documents To Inspect

- AGENTS.md sections 3 through 5 and 9 through 10.
- docs/DESIGN.md sections 8.3 through 8.10, 15.2, 15.5, and 15.9.
- plans/design-decisions.md DD-26, DD-27, DD-28, and DD-29.
- plans/implementation_plan.md sections 2.6, 3, Task M1G, and Appendix F.
- M0F task plan, current BootstrapSceneAuthoring, and Unity authoring skill reference.

## Public Contract Changes

- Add generic Presentation contracts such as CombatActorView, PartyFormationView, EnemyFormationView, actor-anchor roles, and a layout-validation result.
- Actor binding uses stable runtime IDs, never GameObject name/instance identity as gameplay authority.
- Layout input is derived from authoritative state; scene/prefab fields are presentation defaults only.

## Schema or Content Changes

- Add only fixture/fallback logical presentation bindings permitted by M0F.
- No production asset, character binding, or required-current-milestone production reference is authored.

## Implementation Steps

1. Inspect current SampleScene/bootstrap ownership and add a separate combat scene or explicitly scoped combat root without converting the M0 developer shell into a fake menu.
2. Create generic independent party/enemy actor roots and anchor contracts.
3. Configure the combat Canvas/scaler policy exactly as declared above; record the tested reference resolutions in scene/layout validation.
4. Build safe-zone containers from anchors/constraints, not fixed screenshot coordinates.
5. Add fixture/fallback actor visuals and non-gameplay target regions.
6. Add Play Mode/layout tests for actor independence, target bounds, lane collisions, log behavior, and aspect ratios.

## Required Tests

- Four fixture party actors and every fixture enemy produce independent actor roots and stable binding IDs.
- No composite party or multi-enemy targetable root exists.
- Target bounds/intent/status anchors remain unambiguous at 16:9, 16:10, and ultrawide.
- Hand safe area, shared-survival lane, enemy target lane, and End Turn control do not collide.
- Canvas mode, reference resolution, and 0.5 match policy are asserted rather than inferred from Game View state.
- Scene enters Play Mode without missing scripts, duplicate EventSystem, or duplicate combat Canvas.

## Validation Commands

- Run unity --help, unity command --help, and command discovery before Editor automation.
- Tools\validate.ps1.
- unity test . --mode EditMode --output Logs\M1G-EditMode-results.xml.
- unity test . --mode PlayMode --output Logs\M1G-PlayMode-results.xml.
- unity command . bloom.scene-summary.
- Use direct bounded scene inspection until M1I adds bloom.validate-combat-layout.

## Visual or Interaction Validation

- Inspect the combat stage in the Unity runtime at all three required aspect ratios.
- Confirm actor target regions, intent anchors, hand safe zone, shared-survival lane, and End Turn control remain visibly separated.

## Exit Criteria

- The permanent M1 combat-stage architecture contains independent generic party/enemy actor views and required anchors.
- The declared CanvasScaler policy and all safe-zone assertions pass.
- Fixture/fallback visuals are non-production and cannot own authoritative gameplay.

## Implementation Discretion

- Prefab hierarchy, exact anchor implementation, sprite placeholders, sorting setup, and responsive constraint helpers may vary while preserving roles, safe zones, and binding contracts.

## Stop Conditions

- A need to flatten actors, encode gameplay in scene/prefab values, change the selected Canvas architecture, introduce production asset bindings, or alter DD-26/DD-27 actor rules.

## Worklog Entry Requirements

- Record scene/prefab roles, Canvas policy, tested resolutions, actor/layout evidence, and fixture/fallback asset status.
- Commit expectation: one task-scoped commit after all validation passes.
