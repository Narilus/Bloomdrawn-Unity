---
name: bloomdrawn-unity
description: Use for implementing, debugging, testing, inspecting, or automating the Bloomdrawn Unity 6.5 project, especially Unity Editor work, Unity CLI/Pipeline, C# assemblies, scenes/prefabs, uGUI/TMP, Input System, combat actors, card hand/drag/targeting, asset import, Play/Edit Mode tests, and project-owned bloom.* commands. Do not use this skill to invent game design or expand task scope beyond the approved Bloomdrawn task plan.
---

# Bloomdrawn Unity Development

Use this skill to operate Unity effectively while preserving Bloomdrawn's deterministic architecture and strict task governance.

## Start with project authority, not Unity convenience

1. Read the repository `AGENTS.md`.
2. Identify the active task and read its task plan under `plans/tasks/`.
3. Read only the sections of `docs/DESIGN.md`, `plans/implementation_plan.md`, approved decisions, and companion docs required by that task.
4. Inspect existing code/assets before choosing an implementation path.
5. Give a concise preflight before editing.

Unity is the host and presentation environment. It is not permission to move authoritative rules into scene objects or MonoBehaviours.

## Confirm the local Unity toolchain

The repository targets Unity 6.5, but the exact patch is whatever `ProjectSettings/ProjectVersion.txt` pins.

For CLI/Pipeline work, first discover the installed interface instead of assuming syntax:

```powershell
unity --help
unity pipeline list
unity command --help
unity command
```

Use `unity status` when present and useful. If multiple Editors are running, target the intended project explicitly.

The Unity CLI/Pipeline surface is experimental and can change independently of the Unity Editor. Installed `--help` and live command discovery outrank remembered examples.

Read `references/unity-cli-pipeline.md` when using CLI/Pipeline or creating agent-facing Editor commands.


## Launch and verify an automation-capable Editor

Bloomdrawn requires `-automated` on any Unity Editor instance that the agent will control through Pipeline, live evaluation, Play Mode interaction, scene/prefab tooling, or runtime inspection.

Do not rely on Unity Hub's per-project launch arguments when the agent launches the Editor directly; those settings can be bypassed.

Preferred workflow:

1. Read the pinned version from `ProjectSettings/ProjectVersion.txt`.
2. Inspect the current project Editor processes with `Tools/get-unity-editor-state.ps1`.
3. If no Editor is running for the project, launch it with `Tools/open-automated-editor.ps1`.
4. If an Editor for the project is already running without `-automated`, stop and report it. Do not kill or restart a user-owned Editor without explicit authorization.
5. Discover the installed Unity CLI surface (`unity --help`, `unity status --help`, `unity command --help`) and target the correct project explicitly when more than one Editor exists.
6. Wait with a bounded timeout for the intended Editor/Pipeline instance to become available.
7. Before task validation, confirm the Editor is the pinned version, automation-capable, connected when Pipeline is required, compilation is idle, and compile errors are zero.

`-batchmode` is not a substitute for this interactive agent-controlled Editor path.

Do not use `unity open` for an agent-controlled Editor unless the installed CLI explicitly supports forwarding the required `-automated` Editor argument. The repository launcher is the safe default because it makes the project requirement explicit.

### Waiting without progress loops

Unity startup and compilation can legitimately take time. Handle that with bounded polling rather than repeated assistant status messages.

- Poll using commands/tools and keep the wait internal.
- Emit a progress update only for a meaningful state change or when user action is required.
- On timeout, report the last observed process/Pipeline/compilation state and the command that failed or remained unavailable.
- Never repeat an unchanged “task remains active” message as a polling mechanism.

### Health wording

Distinguish these states explicitly:

- Editor process running / not running;
- automation argument present / absent;
- Pipeline connected / unavailable when relevant;
- compilation active / idle;
- compile error count or explicit compile-success evidence;
- content/scene/task-specific validation result.

For example, prefer `Compilation: IDLE; compile errors: 0` over “Unity is not compiling.”

## Choose the correct layer

Before changing code, classify it:

### Engine / rules
Pure deterministic C# state, commands, events, formulas, RNG, targeting rules, card zones, Domain rules, map rules, rewards, gacha, persistence-domain contracts.

Requirements:

- no scene/GameObject/MonoBehaviour dependency;
- no frame/wall-clock dependency;
- no `UnityEngine.Random`;
- no presentation-derived authoritative values;
- named content expressed through validated data and generic operations.

### Application / adapter
Connects authoritative state to persistence, session lifetime, content registries, presentation token generation, and command submission.

It may understand Unity hosting needs but must not reimplement gameplay rules.

### Presentation
Scenes, prefabs, uGUI/TMP, Input System interactions, actors, card views, animation, VFX, audio, cameras, layout, tooltips.

Presentation consumes authoritative state/events. It can stage, interpolate, highlight, preview, animate, and lock input. It may not secretly resolve gameplay.

### Editor / agent tooling
Editor-only validation, import tooling, project health checks, fixture loaders, scene summaries, CLI/Pipeline commands.

Keep Editor-only dependencies out of player/runtime assemblies.

Read `references/unity-authoring.md` before scene/prefab/UI/asset-heavy work.

## Unity authoring workflow

Prefer maintainable source and Unity-aware operations over brittle serialized-file surgery.

For code changes:

1. edit source;
2. let Unity import/compile;
3. inspect compile errors/status;
4. run the smallest relevant test/validation;
5. exercise runtime behavior when the task is presentation-sensitive.

For scene/prefab/asset mutations:

1. prefer existing project-owned authoring tools;
2. otherwise add bounded Editor tooling if the task justifies reusable automation;
3. use live eval for inspection or a one-off bounded operation;
4. avoid direct YAML edits unless there is a clear reason and immediate validation.

Do not invent GUIDs or separate `.meta` files from their assets.

## Use Pipeline as observability, not magic

Ad-hoc live evaluation is excellent for questions such as:

- Which scene is loaded?
- Does this GameObject/component exist?
- What Canvas/render mode/reference resolution is active?
- What are the current RectTransform values?
- Did Unity compile the new type?
- What authoritative combat state is the presentation currently bound to?

Do not build large opaque programs inside eval strings. If the same diagnostic/operation will be used again, make a project-owned Editor/Pipeline command with a stable `bloom.*` name when in scope.

Good command traits:

- deterministic where applicable;
- explicit project/scene targeting;
- machine-readable output where useful;
- nonzero exit/failure result on invalid state;
- no hidden gameplay mutation;
- useful diagnostics rather than “success” without evidence.

## Combat actor rule

Never represent the whole party or a multi-enemy formation as a single composite battlefield actor.

Party members and enemies must remain individually addressable presentation actors through the generic actor contracts defined by the current milestone. Use separate visual roots/anchors/interaction regions where required by the task.

This matters for:

- owner acknowledgement;
- target selection;
- hit/act/death reactions;
- status/intents;
- VFX placement;
- future animation and layout stability.

## Card hand and drag rule

Treat the hand as a deterministic layout system plus a temporary interaction overlay.

### Resting fan

- Anchor the hand to bottom-centre.
- Derive every card's resting position, rotation, scale/overlap, and depth from authoritative hand order and current layout inputs.
- Recalculate after hand mutation and after every completed/cancelled drag.
- Never use the dragged transform as the next resting transform.

### Drag

- Start a UI-only drag session on pointer-down.
- Move the visual into the approved drag/interaction layer while preserving its screen position.
- Convert coordinates through the correct Canvas/camera/local RectTransform path; do not mix world, screen, and local coordinates casually.
- Keep the card readable and recoverable on-screen.

### Play Area threshold

- Below threshold: disarmed.
- Cross threshold upward: armed and visibly ready to play using more than colour alone.
- Return below threshold: disarm.
- Release disarmed: cancel and restore to recalculated fan.
- Release armed, no explicit target required: submit the complete play command.
- Release armed, explicit target required: stage the card above the hand, highlight legal targets, then wait for target click/confirm.
- Cancel target selection: return to hand without Mana/RNG/card-zone/resource change.

The gesture never mutates authoritative combat. Only acceptance of the complete Game Command does.

When changing hand behavior, explicitly test repeated hover/drag/cancel cycles and more than one required aspect ratio. The previous class of bugs to guard against is cumulative drift, incorrect reparenting coordinate conversion, off-screen cards, duplicate views, and accidental play while returning to the hand.

Read `references/verification-playbook.md` for the expected validation pattern.

## Generated art

Generated artwork is fully permitted and may be release-quality. Treat provenance separately from readiness.

For generated or hand-authored art, verify the same things:

- correct subject/design continuity;
- useful transparency/cropping;
- independent actor suitability;
- gameplay-scale readability;
- logical presentation asset mapping;
- import/compression/sprite settings appropriate to the task;
- no accidental composite art where independent actors are required.

Do not mark an asset temporary merely because it was generated.

## Testing sequence

Use the active task plan as the exact gate. A strong default sequence is:

1. compile/import health;
2. pure engine/content tests if rules/data changed;
3. Edit Mode tests for Editor/asset/serialization logic;
4. Play Mode tests for runtime scene/input/presentation behavior;
5. project `bloom.*` validation commands where available;
6. actual interaction/visual check for card feel, targeting, actor layout, animation, VFX, or responsive UI.

Do not equate “Play Mode test passed” with “the UI feels correct” when the task explicitly concerns feel/layout. Conversely, do not use visual inspection as a replacement for deterministic tests.

## Stop conditions

Stop and report rather than guessing when:

- design docs and task plan conflict;
- the task would require implementing a future milestone;
- a required schema/operation/presentation token does not yet exist and creating it is out of scope;
- a Unity/package/API change would alter architecture but is not approved;
- the only apparent solution is named-content hardcoding;
- a failing test appears to encode an approved invariant that the proposed implementation would violate;
- fixture content would leak into production-facing state.

Finish with an evidence-based summary: files changed, behavior, commands/tests run, results, and unresolved concerns.
