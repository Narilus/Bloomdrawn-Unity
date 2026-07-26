# Task M1F - Enemy Intent and Sequential Actions

## Objective

Evolve M1A initial fixture intent into the full M1 intent lifecycle with stable enemy slots and sequential enemy resolution.

## Prerequisites

- M1A initial fixture intent/setup; M1B phases; M1E damage/terminal resolution.
- M0D ordered events and stable runtime IDs.

## In Scope

- Add EnemySlot, visible intent state, stable slot ordering, sequential enemy action iteration, regeneration after enemy end, and presentation-ready event metadata.
- Resolve each enemy action one at a time in authoritative slot order.
- Preserve terminal interruption and never let presentation alter slot order or intent results.

## Non-Goals

- Production enemy intent decks, enemy breadth, bosses, statuses, target heuristics beyond fixture behavior, or M6 encounter content.
- Unity presentation sequencing itself; M1I consumes events later.

## Source Documents To Inspect

- docs/DESIGN.md sections 6.8 through 6.11, 7.8, 8.7, 15.4, 15.9, and 18.3.
- plans/design-decisions.md DD-01, DD-26, and DD-27.
- plans/implementation_plan.md Tasks M1A, M1F, and M1I.
- M1A/M1B/M1E task plans.

## Public Contract Changes

- Add pure EnemySlot/Intent state and ordered intent/action/regeneration event kinds.
- Event facts identify stable slot, source/target runtime IDs, intent data, and sequential action boundary; they contain no Unity actor/prefab/Animator reference.

## Schema or Content Changes

- Extend only fixture encounter intent data as needed for deterministic M1 generation/regeneration.
- M1A remains owner of initial fixture intent data; M1F exclusively owns its runtime interpretation, lifecycle, stable slots, sequential resolution, and regeneration.

## Implementation Steps

1. Define stable EnemySlot ordering from M1A encounter construction.
2. Convert M1A initial intent into visible runtime intent state.
3. Advance ENEMY_PHASE_START through one EnemySlot at a time, resolving via M1E.
4. Regenerate intents only after ENEMY_END and preserve terminal interruption.
5. Emit semantic sequence/slot metadata for M1I.
6. Add lifecycle, ordering, regeneration, and terminal tests.

## Required Tests

- Repeated fixture setup yields identical slots and initial/regenerated intent sequence.
- Multiple enemies, when added as test fixtures, act in stable slot order without simultaneous resolution.
- Terminal result stops later enemy actions and ordinary regeneration.
- Presentation cannot influence slot order, chosen intent, RNG, or action result.
- Invalid phase/slot advancement rejects unchanged state.

## Validation Commands

- Tools\validate.ps1.
- unity test . --mode EditMode --output Logs\M1F-EditMode-results.xml.
- unity command . bloom.health.

## Visual or Interaction Validation

Inspect emitted semantic event facts only; visual playback belongs to M1I.

## Exit Criteria

- M1 has visible fixture intent, stable slots, sequential actions, and post-enemy-end regeneration.
- M1A/M1F intent ownership is disjoint and documented by tests.
- Presentation has sufficient ordered event data without redefining any rule.

## Implementation Discretion

- Intent-storage and iteration implementation may vary if stable ordering, lifecycle timing, and pure event contracts remain unchanged.

## Stop Conditions

- A request for production enemy content, named enemy branches, presentation-controlled ordering, changed combat timing, or M6/M9 target-layout rules.

## Worklog Entry Requirements

- Record initial-versus-lifecycle intent ownership, slot ordering, terminal evidence, and event metadata.
- Commit expectation: one task-scoped commit after all validation passes.
