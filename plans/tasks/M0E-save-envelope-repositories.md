# Task M0E - Save Envelope and Repository Interfaces

## Objective

Establish minimal validated persistence contracts and repositories without precommitting future gameplay/profile systems or storing Unity object state.

## In Scope

- Define a versioned save envelope with `saveSchemaVersion`, `engineVersion`, `contentVersion`, checksum, and payload.
- Define profile and run repository interfaces in non-UI application code.
- Implement an in-memory repository for tests.
- Implement a minimal local-file repository beneath `Application.persistentDataPath` through a Unity-facing adapter, with temp-write/replace and previous-valid fallback behavior.
- Validate envelopes before load and reject incompatible schema/content/engine metadata as applicable.
- Prove serializable RNG state roundtrips through the selected save serializer.

## Non-Goals

- Future profile inventory, rosters, equipment, banners, Shops, map/combat saves, migrations beyond incompatible-schema rejection, save UI, cloud sync, encryption, or Unity object serialization.
- Persisting scene paths, instance IDs, GameObjects, MonoBehaviours, or presentation state.

## Source Documents To Inspect

- `AGENTS.md` sections 1, 6, 7, 10, and 12.
- `docs/DESIGN.md` persistence/save terminology and section 23 save checkpoint constraint.
- `plans/implementation_plan.md` sections 0.1, 7, Task M0E, and Appendix E.
- Completed M0A application/engine boundaries and M0C RNG serialization contract.

## Public Contract Changes

- Save envelope fields, checksum/validation behavior, repository interface methods, and safe replacement/fallback behavior become M0 application contracts.
- Domain/save DTOs contain stable IDs and serializable data only. The Unity persistent-path adapter remains outside the pure engine.

## Schema or Content Changes

- Add only the M0 save-envelope schema/version and test payload DTOs.
- Do not add persisted fields for inactive future systems, production content, or migrations.

## Implementation Steps

1. Define pure envelope/payload validation and checksum code, including explicit incompatible-schema diagnostics.
2. Define profile/run repository interfaces in the application layer and an in-memory implementation for deterministic tests.
3. Implement the local file adapter using a supplied path abstraction so tests can use isolated temporary directories; resolve `Application.persistentDataPath` only at the Unity boundary.
4. Implement durable temp-write then replace, retain the last valid snapshot, and recover it when a replacement is invalid/interrupted in the tested failure case.
5. Add validation-before-load, payload roundtrip, RNG roundtrip, incompatible-schema, and recovery tests.

## Required Tests

- Save envelope validates and checksum failure is rejected.
- Incompatible schema rejects with no payload use.
- In-memory repository saves/loads profile and run payloads.
- Local-file smoke roundtrip works through the application interface.
- Invalid/interrupted replacement preserves or recovers the last valid snapshot in the tested failure case.
- RNG state survives save/load with identical subsequent values.

## Validation Commands

```powershell
Tools/validate.ps1
unity test --project-path . --test-mode EditMode
unity test --project-path . --test-mode PlayMode
```

## Exit Criteria

- Persistence consumers use repository interfaces only.
- Save validation occurs before payload use; local smoke roundtrip and fallback tests pass.
- No Unity object reference, scene path, or instance ID is persisted.

## Worklog Entry Requirements

- Record envelope/version/checksum contract, repository boundaries, recovery behavior, serializer evidence, and all test results.
- State precisely that only M0 envelope schema exists and no future-system persistence/migration or production content was introduced.
