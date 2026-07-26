# Task M1C - Card Instances and Piles

## Objective

Implement pure runtime card identity, ownership, and pile behavior for the M1 fixture deck.

## Prerequisites

- M1A validated card/deck recipe and stable owner IDs.
- M1B CombatState and legal phase boundary.
- M0C deterministic RNG and M0D command/event conventions.

## In Scope

- Add CardInstance, stable card-instance ID, owner ID, definition ID, pile, base/current cost, tags, generated/combat-scoped flags, spent-once state, upgrade/copy-prohibition metadata, and explicit pile state.
- Implement Draw, Hand, Discard, Graveyard, and Resolving behavior; hand target 5, maximum 10, retained-card counting, and deterministic reshuffle when Draw cannot satisfy a request.
- Preserve explicit metadata required by approved DD-23 guardrails without implementing any copy, hidden-zone selection, or generated-card gameplay.

## Non-Goals

- Card play/Mana costs, formula resolution, production cards, Transcend, generated-card effects, or DD-23 copy mechanics.
- UI hand transforms, drag state, or presentation ordering.

## Source Documents To Inspect

- docs/DESIGN.md sections 5.1 through 5.6, 5.9 through 5.11, 6.4 through 6.7, and 16.
- plans/design-decisions.md DD-23, DD-27, and DD-28.
- plans/implementation_plan.md sections 2.4.2, 2.5, and Task M1C.
- M1A/M1B task plans and M0C/M0D contracts.

## Public Contract Changes

- Add pure CardInstance, CardPile, combat deck/pile state, and deterministic draw/reshuffle operations.
- Card identity is separate from card definition identity and remains stable for the combat lifetime.

## Schema or Content Changes

- M1A fixture card definitions may gain only fields required for M1C runtime construction and generic tags.
- No production card schema, copy lineage, hidden-zone selection, or run persistence field is added.

## Implementation Steps

1. Define runtime card/pile contracts in Bloomdrawn.Engine with no Unity references.
2. Instantiate M1A deck recipe entries deterministically with stable IDs and owners.
3. Implement pile movement and hand-target/max rules as pure operations.
4. Implement deterministic reshuffle using the named combat.shuffle stream only when required.
5. Preserve future-safe metadata as inert data; do not add future mechanics.
6. Add invariant, rejection, and deterministic shuffle tests.

## Required Tests

- Identical setup/RNG produces identical card-instance IDs and pile order.
- Each runtime card retains its owner/definition identity through every pile move.
- Retained cards count toward target, hand never exceeds ten, and overflow remains deterministic.
- Reshuffle occurs only when Draw cannot satisfy the request and consumes only the owned shuffle stream.
- Rejected operations leave piles/state/RNG unchanged.
- No copy, hidden-zone, or production-card path is introduced.

## Validation Commands

- Tools\validate.ps1.
- unity test . --mode EditMode --output Logs\M1C-EditMode-results.xml.
- unity command . bloom.health.

## Visual or Interaction Validation

Not required; M1H owns visual hand layout and gesture behavior.

## Exit Criteria

- M1 has deterministic owner-aware runtime cards and the five required piles.
- Draw/hand/retain/reshuffle invariants pass without presentation coupling.
- Future-safe metadata is explicit but no DD-23 behavior exists.

## Implementation Discretion

- Internal pile collections, shuffle algorithm wiring through M0C, and test-property generators may vary if ordering/RNG contracts hold.

## Stop Conditions

- Any requirement for UI state, production card content, persistence of combat cards, copy behavior, hidden information exposure, or new RNG stream ownership.

## Worklog Entry Requirements

- Record runtime-card/pile contracts, named RNG usage, invariant tests, and explicit DD-23 non-implementation.
- Commit expectation: one task-scoped commit after all validation passes.
