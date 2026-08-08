# Bloomdrawn Unity Verification Playbook

This playbook describes the default evidence pattern. The active task plan is the exact required
gate — where this file and the task plan differ, the task plan wins. This file does not enumerate
milestone-specific acceptance cases; those live in the task plan's Required Tests and Exit
Criteria.

## Before implementation
Record/inspect:
- active task ID;
- Git working tree status;
- pinned Unity version (`ProjectSettings/ProjectVersion.txt`);
- relevant scene/content/assembly ownership;
- existing tests and project-owned validation commands;
- whether an `-automated` Editor is running when runtime/Play Mode evidence is required.

## After source changes
- Confirm Unity imported/compiled the change. Read actual compile/import errors rather than
  inferring success from elapsed time.
- Run the smallest relevant automated tests.
- Run task-specific validators/project commands.
- Exercise the changed runtime behavior if presentation or interaction is involved.

## Deterministic gameplay changes
Require evidence appropriate to the task:
- same command + same seed/state -> same state/events;
- rejection does not consume RNG or mutate state;
- golden/replay output updated only when the approved behavior actually changed;
- no presentation object participates in authoritative computation.

## Presentation / interaction changes
Exercise the interaction cases owned by the active milestone. The exact cases come from the task
plan, not this file. As a default shape, verify:
- the gesture/state machine returns cleanly to authoritative state after every cancel/reject path;
- repeated interaction cycles produce no cumulative drift or off-screen loss;
- no gesture mutates authoritative state or consumes RNG before command acceptance;
- required aspect-ratio checks for the milestone;
- click/keyboard paths reach the same states as pointer paths where the task requires parity.

## Actor / battlefield changes
Verify per the task plan:
- required actors remain individually addressable;
- target anchors map to the intended actor;
- overlays do not obscure target identity;
- presentation tokens affect the correct actor;
- no named-content-ID branch was introduced to achieve the effect.

## Scene / prefab changes
Verify:
- scene opens;
- Play Mode enters without missing-script/reference errors;
- expected objects/components exist exactly once where uniqueness matters;
- changed prefabs instantiate correctly;
- project-owned scene/layout validator passes when available.

## Completion report
Report evidence, not confidence language:
- files changed;
- tests/commands run;
- pass/fail counts where available;
- runtime interactions checked;
- aspect ratios checked where relevant;
- any concern not resolved because it was outside scope.