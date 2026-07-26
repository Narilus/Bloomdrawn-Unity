# Task M0C - Deterministic RNG

## Objective

Implement serializable, named authoritative RNG streams in the pure engine without Unity random state, time, or presentation influence.

## In Scope

- Define a pure, serializable RNG state and deterministic next-value API in `Bloomdrawn.Engine`.
- Define the authoritative stream names: `combat.shuffle`, `combat.targeting`, `enemy.intent`, `map.layout`, `map.content`, `map.nodeModifiers`, `reward`, `shop`, and `gacha`.
- Store stream state in authoritative state and provide deterministic derivation from profile/run seeds and stable IDs.
- Establish a clearly separate cosmetic/presentation randomness boundary that cannot read, advance, or alter authoritative streams.
- Add a rejected-command fixture only as necessary to prove no authoritative RNG consumption on rejection.

## Non-Goals

- Combat, map, rewards, Shop, or gacha mechanics; production content; `profile.equipment` stream; Unity random use; or presentation effects.
- Save repository implementation beyond the serializable-state compatibility required by M0E.

## Source Documents To Inspect

- `AGENTS.md` sections 1, 6, 7, and 10.
- `docs/DESIGN.md` deterministic/RNG requirements and DD-27 architecture gate.
- `plans/design-decisions.md` DD-27.
- `plans/implementation_plan.md` sections 0.1, 5, Task M0C, and Appendix E.
- Completed M0A assembly boundary and M0B stable-ID conventions.

## Public Contract Changes

- RNG state serialization shape, stream-name constants, deterministic derivation contract, and bounded/random-result semantics become engine contracts.
- Authoritative code may only use named streams owned by authoritative state; unseeded/global randomness and `UnityEngine.Random` are prohibited.

## Schema or Content Changes

None. Test fixtures must remain explicit fixtures and contain no production IDs/content.

## Implementation Steps

1. Select a pure deterministic algorithm suitable for reproducible serialization and document its seed/state behavior in code/tests without exposing Unity dependencies.
2. Implement immutable or explicitly returned state advancement so call order is auditable.
3. Add named stream registry/state and deterministic substream derivation using stable IDs from M0B.
4. Add a minimal command fixture through the M0D-compatible protocol seam only if it is needed to prove rejection does not consume RNG; do not implement M0D’s full event protocol early.
5. Add sequence, stream-isolation, cosmetic-separation, rejection, and serialization tests.

## Required Tests

- Same seed produces the same sequence.
- Advancing one named stream does not alter any other stream.
- Cosmetic/presentation random calls cannot affect or advance authoritative gameplay streams.
- A rejected command fixture consumes no RNG.
- RNG state serializes and deserializes without changing subsequent output.

## Validation Commands

```powershell
Tools/validate.ps1
unity test --project-path . --test-mode EditMode
```

## Exit Criteria

- Named RNG state is serializable and roundtrips through the serializer adopted by M0E.
- No authoritative engine code references `UnityEngine.Random`, frame time, wall-clock time, or presentation state.
- Required deterministic tests pass.

## Worklog Entry Requirements

- Record algorithm/state contract, complete stream list, derivation rule, serializer roundtrip evidence, and rejection-path test evidence.
- State that no production content, save schema version, Unity package, or presentation implementation was added.
