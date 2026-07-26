# Task M1A - Fixture Party and Combat Setup

## Objective

Establish isolated, schema-authored M1 combat fixture content and a pure registry-derived setup path for one exact-four party and one fixture encounter.

## Prerequisites

- M0A assemblies and validation wrappers.
- M0B validated content registry/import path and fixture-content isolation.
- M0C deterministic RNG state/stream derivation.
- M0D CommandResult, GameEvent, stable semantic ID, and golden-fixture conventions.

## In Scope

- Add M1-only fixture YAML definitions and typed validated projections for four characters, owner-scaled Strike/Shield cards, one exact-four lineup, one enemy, one encounter, and a deterministic eight-card deck recipe.
- Add pure setup contracts, including CombatSetupRequest, a registry-derived setup result, stable runtime participant/enemy identifiers, and deterministic fixture initial intent data.
- Derive runtime IDs from stable content/setup identity, never Unity instance IDs, object names, or collection iteration accident.
- Extend registry validation only as needed for this fixture-combat family and keep all records under GameContent/fixtures.

## Non-Goals

- Runtime CardInstance creation, piles, phase state, card play, damage, or full intent lifecycle.
- Production characters/cards/enemies, Domain resources, passives, Ultimates, Transcend, generated cards, presentation bindings, or M2 schemas.
- Scene-authored lineup/enemy data or fixture-ID engine/UI branches.

## Source Documents To Inspect

- AGENTS.md sections 1, 2, 6, 7, 10, and 11.
- docs/DESIGN.md sections 4, 5, 6.3, 14, 15.4, and 16.
- plans/design-decisions.md DD-01, DD-13, DD-27, and DD-29.
- plans/implementation_plan.md sections 2.5, 3, 4, and Task M1A.
- Completed M0B/M0C/M0D task plans and current ContentRegistry/CommandProtocol source.

## Public Contract Changes

- Add pure fixture-combat content projections and CombatSetupRequest/CombatSetupResult contracts in Bloomdrawn.Content or Bloomdrawn.Engine according to dependency direction.
- Add stable runtime participant and enemy identifier value types or equivalent immutable contracts.
- Initial intent is a deterministic fixture setup fact only. M1F owns all slot/lifecycle/regeneration behavior.

## Schema or Content Changes

- Add explicit fixture-only records for character, card, lineup, enemy, and encounter data using the DD-13 YAML policy.
- Each card definition declares owner, printed cost, targeting category, and generic M1 operation tag; the deck recipe references definitions rather than embedding values in code.
- No production registry, save payload, catalog-required binding, or asset reference is introduced.

## Implementation Steps

1. Inspect M0B validation/import contracts and define the smallest typed fixture-combat projection that consumes validated content rather than bypassing it.
2. Author the four-character fixture, cards, exact-four lineup, enemy, encounter, and deterministic deck recipe in isolated fixture sources.
3. Validate cross-references, stable IDs, fixture origin, lineup cardinality, and deterministic deck ordering.
4. Implement pure setup construction from registry plus lineup/encounter input and stable runtime ID derivation.
5. Produce only the fixture encounter's deterministic initial intent fact; leave slot ordering and subsequent intent handling to M1F.
6. Add focused Edit Mode validation/setup tests and preserve M0 content-command behavior.

## Required Tests

- Valid fixture content produces one setup with four party members, eight deck entries, and one enemy.
- Invalid owner, lineup, encounter, card, or enemy references fail with deterministic diagnostics.
- Repeated construction from the same registry/setup input produces identical runtime IDs, deck recipe, and initial intent.
- Fixture definitions cannot be loaded through production-origin registry selection.
- Changing a fixture stat/card value changes setup through data only and requires no engine/UI branch.

## Validation Commands

- Run unity --help, unity command --help, and unity command discovery before using CLI-heavy validation.
- Tools\validate.ps1.
- unity test . --mode EditMode --output Logs\M1A-EditMode-results.xml.
- unity command . bloom.validate-content.
- unity command . bloom.health.

## Visual or Interaction Validation

Not required; this task creates pure content/setup contracts only.

## Exit Criteria

- One validated fixture setup is registry-derived, deterministic, and isolated from production content.
- Runtime owner/enemy IDs are stable data values unrelated to Unity identities.
- M1A exposes only initial fixture intent; no full intent lifecycle or runtime card-pile system exists here.

## Implementation Discretion

- Private DTO/helper names, internal collection types, and exact deterministic ID encoding may vary if public semantics and stable output remain unchanged.
- Test fixture organization may follow existing Edit Mode conventions.

## Stop Conditions

- A required production schema, named production content branch, Unity object reference in authoritative setup, or M1F intent-lifecycle behavior.
- Any change to stable content-ID rules, registry origin policy, command/event semantics, persistence compatibility, or assembly direction.

## Worklog Entry Requirements

- Record fixture IDs/families, setup/ID invariants, validation results, and that content remains non-production.
- Record no save migration, production asset, or M2 preview/Domain impact.
- Commit expectation: one task-scoped commit after all validation passes.
