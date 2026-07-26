# Task M1E - Damage, Shield, and Healing

## Objective

Implement pure M1 shared-party/enemy combat values, owner-stat formulas, and DD-01 Atomic Stop.

## Prerequisites

- M1A fixture stats/cards, M1B phase/terminal state, M1C card identity, and M1D accepted PlayCard boundary.
- M0D ordered GameEvent contract and M0C deterministic state.

## In Scope

- Add shared party HP/Shield, enemy HP/Shield, owner-stat lookup, basic M1 formulas, damage, Shield, healing, and nonlethal HP-loss distinction.
- Implement owner-Attack Strike and owner-Defense Shield fixture effects.
- Emit semantic source/result facts: command/source kind, owner, Shield absorbed, HP damage dealt, and affected stable IDs.
- Apply DD-01 Atomic Stop after every terminal-capable atomic effect.

## Non-Goals

- Domain/status systems, production formulas, enemy intent behavior, passives, Ultimates, Transcend, preview API, or future reactions.
- Presentation VFX/animation determining damage or terminal timing.

## Source Documents To Inspect

- docs/DESIGN.md sections 6.10 through 6.11, 7.1 through 7.4, 15.4 through 15.6, and 17.2.
- plans/design-decisions.md DD-01 and DD-27.
- plans/implementation_plan.md sections 2.2, 2.4.1, and Task M1E.
- M1A through M1D task plans and M0D golden conventions.

## Public Contract Changes

- Add pure combat-value/formula/effect contracts and semantic damage, Shield, healing, and terminal event kinds.
- Atomic terminal checks are part of accepted resolution semantics, not presentation timing.

## Schema or Content Changes

- M1 fixture operation tags may declare generic Strike/Shield/healing/HP-loss behavior only.
- Do not add production effect trees or status schemas.

## Implementation Steps

1. Define pure combat-value state and generic owner-stat lookup.
2. Implement M1 fixture formula evaluation and atomic damage/Shield/healing/HP-loss operations.
3. Emit ordered source/result facts sufficient for later generic extensions.
4. Check victory/defeat after each terminal-capable atomic event and skip remaining ordinary sub-effects on terminal.
5. Integrate accepted PlayCard resolution without presentation dependencies.
6. Add unit, multi-effect, and deterministic trace tests.

## Required Tests

- Strike uses the owning fixture character's Attack; Shield uses the owning Defense.
- Healing caps at shared maximum HP; HP-loss remains distinguishable from damage.
- Shield absorption and remaining HP damage are correctly represented in events.
- Enemy death skips later ordinary action sub-effects; party defeat stops resolution immediately.
- Simultaneous terminal checkpoint follows approved defeat precedence.
- Repeated setup/commands yield identical events and checksum-ready state.

## Validation Commands

- Tools\validate.ps1.
- unity test . --mode EditMode --output Logs\M1E-EditMode-results.xml.
- unity command . bloom.health.

## Visual or Interaction Validation

Not required; M1I later renders existing event facts.

## Exit Criteria

- Shared party/enemy HP/Shield and owner-scaled M1 effects resolve deterministically.
- DD-01 Atomic Stop is covered by direct and golden-compatible tests.
- No Domain, status, presentation, or production-content rule was added.

## Implementation Discretion

- Formula AST/helper layout and internal state-copy strategy may vary if pure deterministic behavior and event facts remain stable.

## Stop Conditions

- Any change to DD-01, formula ownership, terminal precedence, required source metadata, or request for status/Domain/reaction systems.

## Worklog Entry Requirements

- Record formula/event contracts, Atomic Stop scenarios, deterministic evidence, and no future-system expansion.
- Commit expectation: one task-scoped commit after all validation passes.
