# Task M1D - Mana and Card Play

## Objective

Implement M1 Mana and the authoritative complete PlayCard command boundary.

## Prerequisites

- M1A fixture card target/cost metadata and stable owner IDs.
- M1B legal phase state and M1C runtime cards/piles.
- M0D CommandResult/rejection semantics.

## In Scope

- Add ManaState with base maximum 6 and final-cost floor 0.
- Add complete PlayCard command payload, target-choice representation, validation, accepted state transition, and rejection diagnostics.
- Validate phase, hand location, card/owner identity, target completeness/legality, cost, and card-specific M1 preconditions.
- Require every explicit target choice before command submission.

## Non-Goals

- UI hover, drag, armed, staged-card, or target-selection state in authoritative state.
- Formula/damage resolution beyond delegating future effects to M1E.
- Preview evaluator, Domain costs/resources, production cards, or invisible command queueing.

## Source Documents To Inspect

- docs/DESIGN.md sections 5.6 through 5.8, 6.5, 8.5, and 15.1.
- plans/design-decisions.md DD-27 and DD-28.
- plans/implementation_plan.md sections 2.2 through 2.4 and Task M1D.
- M1B/M1C task plans.

## Public Contract Changes

- Add ManaState, PlayCard command data, target-choice contract, validation/rejection codes, and accepted play event facts.
- A PlayCard command is complete only after all required targets are provided; presentation submits it but cannot partially reserve it.

## Schema or Content Changes

- Add only fixture card fields needed to express M1 target category, printed cost, and generic preconditions.
- Do not add production targeting schemas or preview data.

## Implementation Steps

1. Define pure ManaState and deterministic final-cost calculation.
2. Define complete PlayCard payload and target validation using M1B phase/M1C pile state.
3. Implement accepted transition through resolving state without UI interaction data.
4. Implement precise rejection diagnostics and unchanged-state/no-RNG guarantees.
5. Leave outcome calculation hooks narrow enough for M1E; do not add a general preview API.
6. Add Edit Mode validation tests.

## Required Tests

- Base Mana is six and final cost never falls below zero.
- Wrong phase, missing/wrong target, non-hand card, wrong owner, and insufficient Mana reject unchanged state.
- Rejected commands emit no gameplay events and consume no RNG.
- Target-complete M1 cards accept without explicit target; one-enemy cards require one legal target before acceptance.
- No presentation interaction field appears in CombatState or command acceptance.

## Validation Commands

- Tools\validate.ps1.
- unity test . --mode EditMode --output Logs\M1D-EditMode-results.xml.
- unity command . bloom.health.

## Visual or Interaction Validation

Not required; M1H later proves gesture paths submit the same completed commands.

## Exit Criteria

- Mana and complete authoritative PlayCard validation work through pure engine state.
- Partial target selection has no authoritative effect.
- Rejection preserves state, piles, Mana, RNG, and event history.

## Implementation Discretion

- Exact target-choice value-type shape, cost-modifier storage, and rejection-code naming may vary within the declared semantic contract.

## Stop Conditions

- Any need for UI state in engine, UI-side formula/legality calculation, new target rules beyond approved M1 fixture categories, or future preview/Domain behavior.

## Worklog Entry Requirements

- Record command/rejection contracts, Mana invariants, no-mutation evidence, and explicit UI-state exclusion.
- Commit expectation: one task-scoped commit after all validation passes.
