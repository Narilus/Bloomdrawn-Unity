# Task M1B - Combat State Machine

## Objective

Implement the pure authoritative M1 combat phase machine and legal command boundary using M1A setup output.

## Prerequisites

- M1A validated CombatSetupResult with stable fixture participants/enemies and initial intent fact.
- M0D accepted/rejected command and ordered-event contracts.

## In Scope

- Add CombatState, CombatPhase, terminal state, legal command-boundary checks, and internal deterministic phase advancement.
- Represent COMBAT_SETUP, PLAYER_TURN_START, PLAYER_ACTION, PLAYER_CLEANUP, PLAYER_END, ENEMY_PHASE_START, ENEMY_ACTION, ENEMY_END, ROUND_END, VICTORY, and DEFEAT.
- Emit ordered semantic phase/terminal events and reject illegal public commands without mutation.

## Non-Goals

- Card-pile, Mana, formula, damage, intent-lifecycle, presentation, persistence, or preview implementation.
- Frame time, coroutines, scene state, Animator state, or UI input as authoritative phase inputs.

## Source Documents To Inspect

- AGENTS.md deterministic-boundary and testing rules.
- docs/DESIGN.md sections 6.2 through 6.11 and 15.1 through 15.5.
- plans/design-decisions.md DD-01 and DD-27.
- plans/implementation_plan.md sections 2.1 through 2.3 and Task M1B.
- M1A task plan and M0D command/event protocol.

## Public Contract Changes

- Add pure CombatState and CombatPhase contracts, legal command classification, and ordered phase/terminal event kinds.
- Public command entrypoints return CommandResult<CombatState>; private phase advancement is not a UI command.

## Schema or Content Changes

None. M1B consumes M1A fixture setup and does not author content.

## Implementation Steps

1. Define immutable or externally immutable combat state/phase representation in Bloomdrawn.Engine.
2. Map M1A setup into COMBAT_SETUP and deterministic initial transition behavior.
3. Define legal public-command boundaries and unchanged-state rejection diagnostics.
4. Implement private phase advancement without Unity/frame dependencies.
5. Define terminal-state command rejection and ordered event emission.
6. Add transition-table Edit Mode tests.

## Required Tests

- Every declared phase enters only from approved predecessor states.
- Illegal command/phase combinations reject with unchanged state and no events.
- Terminal states reject normal combat commands.
- The same setup/command sequence produces identical phase/event traces.
- Engine source remains free of Unity dependencies.

## Validation Commands

- Tools\validate.ps1.
- unity test . --mode EditMode --output Logs\M1B-EditMode-results.xml.
- unity command . bloom.health.

## Visual or Interaction Validation

Not required; phase state is pure engine behavior.

## Exit Criteria

- The complete M1 phase vocabulary exists in a pure deterministic state machine.
- Phase movement is internal and public command legality is explicit.
- No phase decision depends on presentation, scene, frame time, or input gesture state.

## Implementation Discretion

- Internal transition-table representation, immutable-copy strategy, and rejection-code naming may vary while preserving declared phase semantics.

## Stop Conditions

- Any change to DESIGN combat ordering, DD-01 terminal timing, public command ownership, or requirement for Unity/presentation state inside the engine.

## Worklog Entry Requirements

- Record phase contract, rejection invariants, deterministic evidence, and no Unity/persistence/schema impact.
- Commit expectation: one task-scoped commit after all validation passes.
