# Task M1I - Initial Sequential Presentation Adapter

## Objective

Establish the permanent M1 GameEvent-to-PresentationToken path, independent actor binding, input coordination, and fixture-state Editor commands.

## Prerequisites

- M1D accepted/rejected PlayCard path, M1E effect events, M1F sequential enemy events.
- M1G actor/scene contracts and M1H interaction presentation.
- M0F Editor/Pipeline command architecture.

## In Scope

- Add CombatSession/application ownership of authoritative state submission and PresentationToken mapping for existing M1 events.
- Bind tokens to actor views by stable runtime IDs and provide generic fixture/fallback reactions for card play, owner acknowledgement, damage, Shield gain, enemy action, hit, victory, and defeat.
- Add presentation completion/input-lock coordination, basic reduced-motion and speed hooks, and safe fallback behavior.
- Add Bloom Editor commands: bloom.load-combat-fixture, bloom.reset-combat-fixture, bloom.dump-combat-state, and bloom.validate-combat-layout.

## Non-Goals

- New gameplay rules, event reordering, UI-side resolution, production VFX/audio/art, M9 sequence polish, or M2 previews.
- CLI/Pipeline runtime dependency or command branches keyed to fixture content IDs.

## Source Documents To Inspect

- docs/DESIGN.md sections 15.1, 15.5, and 15.9.
- plans/design-decisions.md DD-27, DD-28, and DD-29.
- plans/implementation_plan.md sections 2.6, 4.2, Task M1I, and Appendix F.
- M0F command/tooling source and M1D through M1H task plans.

## Public Contract Changes

- Add CombatSession, PresentationToken, token mapper/queue, stable actor lookup, and explicit presentation-completion contracts.
- Add four Editor-only bloom commands with concise structured output and explicit failure behavior.
- Tokens identify authoritative facts/IDs only; they cannot own or recompute gameplay results.

## Schema or Content Changes

- Add fixture/fallback token binding data only where generic role/type contracts need it.
- No production asset catalog expansion or gameplay content is added.

## Implementation Steps

1. Define application session ownership around the current authoritative CombatState and complete command submission.
2. Map ordered M1 GameEvents to semantic tokens without changing event sequence.
3. Bind tokens through M1G stable actor IDs and implement minimal generic reactions/fallbacks.
4. Add input locking only during accepted sequence playback; never invisibly queue commands.
5. Implement fixture load/reset/state/layout commands through Bloomdrawn.Editor and validate their failure semantics.
6. Add Edit/Play Mode token, binding, and command tests.

## Required Tests

- Same ordered event trace produces same ordered token trace.
- Damage/owner/enemy/victory/defeat tokens route to the intended independent actor by stable ID.
- Presentation/token handling cannot mutate CombatState, Mana, piles, RNG, or event history.
- Reduced-motion/speed changes preserve token order and final authoritative state.
- Each bloom fixture/layout command reports useful success and invalid-precondition failure.
- Input lock prevents conflicting simultaneous commands only while accepted playback is resolving.

## Validation Commands

- Tools\validate.ps1.
- unity test . --mode EditMode --output Logs\M1I-EditMode-results.xml.
- unity test . --mode PlayMode --output Logs\M1I-PlayMode-results.xml.
- unity command . bloom.load-combat-fixture.
- unity command . bloom.dump-combat-state.
- unity command . bloom.validate-combat-layout.
- unity command . bloom.reset-combat-fixture.

## Visual or Interaction Validation

- Play one fixture combat sequence and verify card/actor/damage/enemy/victory/defeat reactions occur in event order.
- Verify reduced motion and speed hooks preserve sequencing and input recovery.

## Exit Criteria

- M1 events flow through one permanent session/token/actor path without presentation authority.
- Stable actor binding, fallback presentation, input coordination, and project-owned fixture/layout diagnostics pass.
- No parallel tooling or M9/M10 production presentation system is introduced.

## Implementation Discretion

- Queue implementation, presentation-only interpolation, structured command field names, and fixture-view setup may vary while command semantics and ordering remain stable.

## Stop Conditions

- A need to add gameplay logic to Presentation/Editor, reorder events, persist presentation state, introduce named-content branches, or change existing Pipeline architecture.

## Worklog Entry Requirements

- Record session/token contracts, command names/output guarantees, sequencing evidence, reduced-motion behavior, and no-authority proof.
- Commit expectation: one task-scoped commit after all validation passes.
