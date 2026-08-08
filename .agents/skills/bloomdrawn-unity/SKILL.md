---
name: bloomdrawn-unity
description: Use for implementing, debugging, testing, inspecting, or automating the Bloomdrawn
  Unity 6.5 project — Unity Editor work, Unity CLI/Pipeline, C# assemblies, scenes/prefabs,
  uGUI/TMP, Input System, asset import, Play/Edit Mode tests, and project-owned bloom.* commands.
  This skill teaches HOW to operate the Unity toolchain safely. It does NOT define game rules —
  those live in docs/DESIGN.md, approved decisions, and the active task plan.
---

# Bloomdrawn Unity Development Skill

This skill teaches the agent how to operate Unity and the Bloomdrawn toolchain. It deliberately
does not restate game design, interaction contracts, or milestone test matrices. For WHAT the
game must do, read the source-of-truth hierarchy. This skill only covers HOW to work in this
repository without breaking determinism or task governance.

## 1. Start with authority, not Unity convenience
1. Read repository-root `AGENTS.md`.
2. Identify the active task and read its plan under `plans/tasks/`.
3. Read only the sections of `docs/DESIGN.md`, `plans/implementation_plan.md`, and approved
   decisions that the task cites.
4. Inspect existing code/assets before choosing an implementation path.
5. State a concise preflight before editing.

Game rules, interaction contracts, and acceptance criteria come from the authority documents and
the active task plan — never from this skill, from memory, or from inference. If authority and
Unity convenience conflict, authority wins.

## 2. Launching an automation-capable Unity Editor
Bloomdrawn agents can only control a Unity Editor launched with the `-automated` flag. An Editor
without `-automated` is NOT reachable through Pipeline, live evaluation, Play Mode interaction,
scene/prefab tooling, or runtime inspection.

Preferred workflow:
1. Read the pinned version from `ProjectSettings/ProjectVersion.txt`.
2. Inspect current project Editor processes with `Tools/get-unity-editor-state.ps1`.
3. If no Editor is running for the project, launch it with `Tools/open-automated-editor.ps1`.
4. If an Editor for the project is already running WITHOUT `-automated`, stop and report it.
   Never kill or restart a user-owned Editor without explicit owner authorization.
5. Discover the installed Unity CLI surface (`unity --help`, `unity status --help`,
   `unity command --help`) and target the correct project explicitly when more than one Editor
   exists.
6. Wait with a bounded timeout for the intended Editor/Pipeline instance to become available.

Before task validation, confirm: the Editor is the pinned version, automation-capable
(`-automated` present), connected when Pipeline is required, compilation idle, and compile
errors zero.

Notes:
- `-batchmode` is NOT a substitute for the interactive agent-controlled Editor path.
- Do not use `unity open` for an agent-controlled Editor unless the installed CLI explicitly
  forwards the `-automated` argument. The repository launcher is the safe default.

## 3. Waiting without progress loops
Unity startup and compilation can legitimately take time. Handle it with bounded polling, not
repeated status messages.
- Poll using commands/tools; keep the wait internal.
- Emit a progress update only on a meaningful state change or when user action is required.
- On timeout, report the last observed process/Pipeline/compilation state and the command that
  failed or stayed unavailable.
- Never repeat an unchanged "task remains active" message as a polling mechanism.

## 4. Health wording
Distinguish these states explicitly:
- Editor process running / not running;
- `-automated` argument present / absent;
- Pipeline connected / unavailable (when relevant);
- compilation active / idle;
- compile error count or explicit compile-success evidence;
- content/scene/task-specific validation result.

Prefer `Compilation: IDLE; compile errors: 0` over "Unity is not compiling."

## 5. Choose the correct layer
Before changing code, classify it. The dependency direction is contractual:
- Engine / rules — pure deterministic C#. No scene/GameObject/MonoBehaviour dependency, no
  frame/wall-clock dependency, no `UnityEngine.Random`, no presentation-derived values.
- Application / adapter — connects authoritative state to persistence, sessions, content
  registries, presentation tokens, and command submission. Must not reimplement gameplay rules.
- Presentation — scenes, prefabs, uGUI/TMP, Input System interactions, actors, animation, VFX,
  audio, cameras, layout. Consumes authoritative state/events. Never resolves gameplay.
- Editor / agent tooling — Editor-only validation, import, health checks, fixture loaders,
  CLI/Pipeline commands. Keep Editor-only dependencies out of player/runtime assemblies.

Read `references/unity-authoring.md` before scene/prefab/UI/asset-heavy work.

## 6. Unity authoring workflow
Prefer maintainable source and Unity-aware operations over brittle serialized-file surgery.
- For code changes: edit source -> let Unity import/compile -> inspect compile errors/status ->
  run the smallest relevant test/validation -> exercise runtime behavior when presentation-
  sensitive.
- For scene/prefab/asset mutations: prefer existing project-owned authoring tools; otherwise add
  bounded Editor tooling if justified; use live eval for inspection or a one-off bounded
  operation; avoid direct YAML edits unless there is a clear reason and immediate validation.
  Never invent GUIDs or separate `.meta` files from their assets.

## 7. Use Pipeline as observability, not magic
Ad-hoc live evaluation is excellent for: which scene is loaded? does this component exist? what
Canvas/render mode is active? did Unity compile the new type? what authoritative state is
presentation bound to?

Do not build large opaque programs inside eval strings. If a diagnostic/operation will be reused,
make it a project-owned Editor/Pipeline command with a stable `bloom.*` name when in scope. Good
commands are deterministic where applicable, explicitly target project/scene state, emit
machine-readable output where useful, fail loudly on invalid state, and never hide gameplay
mutation.

For CLI/Pipeline syntax and discovery, read `references/unity-cli-pipeline.md`. The installed
`--help` is authoritative; the CLI is experimental and changes independently of the Editor.

## 8. Presentation-safety techniques (how, not what)
When a task touches UI, actors, or interaction, apply these Unity techniques. The actual behavior
contract comes from the task plan and DESIGN.md — do not derive it here.
- Coordinate conversion: use one coherent coordinate space, or explicit conversion via
  `RectTransformUtility` with the correct Canvas/camera. Never mix world/screen/local casually.
  Never assign raw pointer pixels to an unrelated local/world transform.
- Reparenting: preserve intended visual screen position when moving between layers. Do not force
  the element off-screen.
- Deterministic layout: where a layout is derived from authoritative order, recompute it from
  data rather than persisting temporary gesture transforms.
- Actor identity: where the task requires independently addressable actors, keep them as separate
  roots/anchors; do not flatten them into one composite. The exact actor-separation rule is a
  DESIGN.md invariant — read it there.
- Input: gestures are presentation state until a complete command is accepted. One interaction
  controller owns a drag/target session at a time. Do not create parallel input paths that submit
  the same command.

For the expected validation pattern, read `references/verification-playbook.md`. For the exact
required gate, use the active task plan.

## 9. Generated art
Generated artwork is fully permitted and may be release-quality. Provenance is separate from
readiness. Verify the same technical things for generated and hand-authored art: subject/design
continuity, useful transparency, independent-actor suitability, gameplay-scale readability,
logical presentation-asset mapping, and appropriate import/compression/sprite settings. Do not
mark an asset temporary merely because it was generated.

## 10. Testing sequence
Use the active task plan as the exact gate. A strong default sequence:
1. compile/import health;
2. pure engine/content tests if rules/data changed;
3. Edit Mode tests for Editor/asset/serialization logic;
4. Play Mode tests for runtime scene/input/presentation behavior;
5. project `bloom.*` validation commands where available;
6. actual interaction/visual check when the task concerns feel, layout, targeting, or responsive
   UI.

Do not equate "Play Mode test passed" with "the UI feels correct" when the task explicitly
concerns feel/layout. Conversely, do not use visual inspection to replace deterministic tests.

## 11. Stop conditions
Stop and report rather than guessing when:
- design docs and task plan conflict;
- the task would require implementing a future milestone;
- a required schema/operation/presentation token does not exist and creating it is out of scope;
- a Unity/package/API change would alter architecture but is not approved;
- the only apparent solution is named-content hardcoding;
- a failing test appears to encode an approved invariant the implementation would violate;
- fixture content would leak into production-facing state.

Finish with an evidence-based summary: files changed, behavior, commands/tests run, results,
unresolved concerns.