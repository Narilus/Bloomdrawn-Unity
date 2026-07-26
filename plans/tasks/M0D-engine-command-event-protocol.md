# Task M0D - Engine Command/Event Protocol

## Objective

Define the reusable pure-C# command result, rejection diagnostics, ordered gameplay-event envelope, and first golden fixture format that later engine systems will use without presentation coupling.

## In Scope

- Define `CommandResult` with accepted/rejected outcomes and unchanged-state rejection semantics.
- Define accepted-event envelopes with deterministic ordering fields, semantic event facts, and optional stable runtime source/target IDs.
- Define a structured rejected diagnostic shape.
- Implement one no-op/smoke command fixture and a golden fixture format containing initial state, commands, semantic events, and checksum.
- Add deterministic checksum creation/verification independent of frame/render state.

## Non-Goals

- Combat/card/map/rule implementation, Animator/prefab/VFX references, presentation token mapping, scene behavior, production content, or a general command bus.
- Changing RNG semantics beyond using M0C state where the smoke fixture needs a fixed seed.

## Source Documents To Inspect

- `AGENTS.md` sections 1, 6, 7, and 10.
- `docs/DESIGN.md` deterministic engine and event/presentation separation requirements.
- `plans/design-decisions.md` DD-27.
- `plans/implementation_plan.md` sections 0.1, 6, Task M0D, and Appendix E.
- Completed M0A/M0C contracts.

## Public Contract Changes

- `CommandResult`, rejection diagnostic fields, event ordering fields, stable runtime ID use, semantic event payload rules, and golden fixture checksum format become reusable engine contracts.
- Events may describe engine facts only. They must not encode Animator states, GameObject names, prefab paths, VFX names, or frame ordering.

## Schema or Content Changes

- Add test-only golden fixture data with a fixed seed and no production content.
- Do not add production gameplay schemas.

## Implementation Steps

1. Define the pure command/result/event interfaces in `Bloomdrawn.Engine` and ensure only stable data types cross the boundary.
2. Define event ordering as explicit deterministic sequence values, not collection/Update/render order.
3. Implement the smoke command that deterministically accepts, changes minimal fixture state, and emits one semantic event; implement a rejection path returning byte-for-byte/equivalently unchanged authoritative state.
4. Implement canonical golden fixture serialization/checksum and a focused runner.
5. Add acceptance, rejection, checksum stability, and frame-independence tests.

## Required Tests

- Accepted command changes state and emits an ordered event.
- Rejected command returns unchanged state and a diagnostic.
- The golden fixture checksum is stable across repeat execution.
- Event ordering is independent of frame/render/presentation state.
- Fixed-seed command smoke test passes in Edit Mode.

## Validation Commands

```powershell
Tools/validate.ps1
unity test --project-path . --test-mode EditMode
```

## Exit Criteria

- Fixed-seed command smoke test passes in Edit Mode.
- The protocol is pure C# and contains no Unity/presentation implementation dependency.
- Golden data and checksum prove deterministic replay of the smoke fixture.

## Worklog Entry Requirements

- Record the public protocol types, event ordering/checksum contract, fixture isolation, and passing deterministic evidence.
- State that no production gameplay rule, content schema, scene, asset catalog, or save migration was introduced.
