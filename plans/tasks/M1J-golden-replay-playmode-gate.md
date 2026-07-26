# Task M1J - Golden Combat Replay and Play Mode Interaction Gate

## Objective

Establish the complete M1 deterministic replay and integrated Play Mode exit gate for combat, actor, and card-interaction behavior.

## Prerequisites

- Completed M1A through M1I outputs, including fixture setup, phase/pile/play/effect/intent contracts, scene/interaction path, and presentation commands.
- M0D golden-fixture/checksum conventions and M0C named RNG streams.

## In Scope

- Replace the M0 smoke-only evidence with registry-derived M1 golden combat replay fixtures containing initial state, named RNG streams, complete commands, semantic events, and final checksum.
- Prove DD-01 Atomic Stop in replay evidence.
- Add the complete Edit Mode/Play Mode suite covering each M1 milestone exit criterion and all DD-28 interaction cases.
- Run project validation, fixture/layout commands, and Windows smoke build because M1 closes a Player-facing combat scene path.

## Non-Goals

- Production combat content, M2 Domain mechanics/preview evaluator, map/run/meta systems, balance simulation, or M9 polish.
- Weakening/replacing earlier task tests merely to make the final gate pass.

## Source Documents To Inspect

- AGENTS.md sections 2, 6, 9 through 11.
- docs/DESIGN.md sections 5 through 8, 15.1, 15.9, 16, and 17.2.
- plans/design-decisions.md DD-01, DD-27, and DD-28.
- plans/implementation_plan.md sections 4, Task M1J, and M1 exit criteria.
- M1A through M1I task plans and M0D/M0F validation conventions.

## Public Contract Changes

- Add M1 replay fixture/runner data format extending the M0 golden seam with registry-derived combat state, named streams, complete commands, ordered semantic trace, and checksum.
- No new gameplay command/rule contract is introduced; M1J verifies existing contracts.

## Schema or Content Changes

- Add isolated golden/test fixture data only. It must use fixture content and normal registry paths.
- Do not add production data or persistence schema.

## Implementation Steps

1. Build registry-derived initial combat state and fixed named RNG stream input from M1A through M1F.
2. Record/replay legal commands, semantic event trace, Atomic Stop outcomes, and final checksum.
3. Add deterministic Edit Mode replay, rejection, owner, and fixture-isolation tests.
4. Add Play Mode hand/drag/targeting/actor/sequence/layout tests at 16:9, 16:10, and ultrawide.
5. Run all project, fixture, scene/layout, and build validation; investigate failures without weakening approved assertions.
6. Audit the task set's dependencies and report any genuine specification conflict before marking M1 complete.

## Required Tests

- Same registry setup, named streams, and command list reproduce state, event trace, and checksum.
- Every runtime card has a valid owner and owner-scaled Strike/Shield uses fixture data.
- Rejected commands and cancelled drag/target sessions consume no RNG/events/gameplay state.
- Atomic Stop terminal trace matches DD-01.
- Five-card fan, repeated hover/drag/cancel, arm/disarm, target-complete release, explicit-target confirm/cancel, and rejection resync pass.
- Card views remain usable at 16:9, 16:10, and ultrawide.
- Independent enemy target selection and sequential enemy presentation route correctly.
- Normal application fixture load does not introduce production content or production ID branches.

## Validation Commands

- unity --help; unity command --help; unity command discovery.
- Tools\validate.ps1.
- unity test . --mode EditMode --output Logs\M1J-EditMode-results.xml.
- unity test . --mode PlayMode --output Logs\M1J-PlayMode-results.xml.
- unity command . bloom.health.
- unity command . bloom.validate-content.
- unity command . bloom.load-combat-fixture.
- unity command . bloom.dump-combat-state.
- unity command . bloom.validate-combat-layout.
- unity command . bloom.reset-combat-fixture.
- Tools\build-smoke.ps1.

## Visual or Interaction Validation

- Inspect the complete fixture combat in the actual Unity runtime at all three aspect ratios.
- Confirm no card drift/jump/off-screen loss, no ambiguous target, correct event sequence, and input recovery after accepted/rejected/cancelled paths.

## Exit Criteria

- One complete fixture combat plays through the real Unity scene and reproduces headlessly from registry-derived setup.
- All M1 implementation-plan exit criteria are directly covered by automated replay/Play Mode evidence.
- The fixture remains schema-authored/non-production; M1 has not added M2 preview, Domain, run, progression, meta, or production content.

## Implementation Discretion

- Golden fixture storage organization, checksum test helpers, test naming, and screenshot/evidence capture may vary while all required assertions remain objective and repeatable.

## Stop Conditions

- Any failing approved invariant, mismatch between replay and presentation authority, need for production/future system content, or task-plan/specification conflict discovered by the cross-plan audit.

## Worklog Entry Requirements

- Record replay fixture identity, stream/checksum evidence, Edit/Play Mode and build results, visual aspect-ratio checks, cross-plan audit result, and any unavailable validation.
- Record no save/schema migration or production-content impact.
- Commit expectation: one task-scoped commit after all validation passes.
