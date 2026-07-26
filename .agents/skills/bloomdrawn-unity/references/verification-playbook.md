# Bloomdrawn Unity Verification Playbook

Use the active task plan as the exact required gate. This file describes the default evidence pattern.

## Before implementation

Record/inspect:

- active task ID;
- Git working tree status;
- pinned Unity version;
- relevant scene/content/assembly ownership;
- existing tests and project-owned validation commands.

## After source changes

1. Confirm Unity imported/compiled the change.
2. Read actual compile/import errors rather than inferring success from elapsed time.
3. Run the smallest relevant automated tests.
4. Run task-specific validators/project commands.
5. Exercise the changed runtime behavior if presentation or interaction is involved.

## Deterministic gameplay changes

Require evidence appropriate to the task:

- same command + same seed/state -> same state/events;
- rejection does not consume RNG or mutate state;
- golden/replay output updated only when the approved behavior actually changed;
- no presentation object participates in authoritative computation.

## Card hand / drag / targeting changes

At minimum exercise cases owned by the active milestone, including:

- hover/focus then leave -> correct fan restoration;
- repeated drag/cancel loops -> no cumulative drift;
- drag above threshold -> armed indicator;
- move back below threshold -> disarmed;
- release below threshold -> no play;
- release armed on no-target card -> one command, one resolution;
- release armed on explicit-target card -> target-selection state, no premature cost;
- legal target confirm -> one accepted command;
- cancel target selection -> full restoration, no gameplay mutation;
- rejected command -> presentation resyncs to authoritative hand;
- first/last card and dense hand sizes remain recoverable;
- required 16:9, 16:10, and milestone-specified ultrawide checks;
- dragged card never becomes irrecoverably off-screen;
- no duplicate card view/input submission after reparenting.

## Actor/battlefield changes

Verify:

- four party slots remain individually represented when the fixture/party contains four actors;
- enemies remain individually targetable;
- target anchors correspond to the intended actor;
- intent/status overlays do not obscure target identity;
- shared HP region and hand safe area do not collide with actor/target lanes at required aspect ratios;
- presentation token for owner act/hit/death affects the correct actor;
- no named content ID branch was introduced to achieve the effect.

## Scene/prefab changes

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
