# Task M1H - Bottom-Centred Card Fan, Drag Play Area, and Targeting

## Objective

Implement DD-28's production-shaped uGUI/Input System hand, drag, Play Area, and explicit-target interaction without creating authoritative UI state.

## Prerequisites

- M1C runtime hand/pile data, M1D complete PlayCard command, and M1F stable enemy IDs.
- M1G combat Canvas, actor target anchors, safe zones, and Input System scene baseline.
- M0F presentation/Editor conventions.

## In Scope

- Add deterministic HandFanLayout derived from authoritative hand order and current layout inputs.
- Add UI-only interaction state: hover/focus, one drag session, armed/disarmed state, staged target selection, legal target highlighting, cancel/resync behavior, and dedicated drag layer.
- Use RectTransformUtility.ScreenPointToLocalPointInRectangle against the declared drag Canvas/camera path; preserve screen position on reparenting.
- Implement responsive Play Area threshold, non-colour-only armed feedback, click/keyboard semantic parity, and rejection resynchronization.

## Non-Goals

- UI-side cost, target legality, formula, damage, preview, or RNG calculation.
- Production card art, any authoritative preview subsystem (M2H remains the first authoritative preview evaluator), controller-specific redesign, persistence of gesture state, or token playback beyond submitted command handling.

## Source Documents To Inspect

- AGENTS.md combat hand invariants.
- docs/DESIGN.md sections 5.6 through 5.8, 8.5, 8.9 through 8.10, and 15.1.
- plans/design-decisions.md DD-28 and DD-27.
- plans/implementation_plan.md sections 2.6 and Task M1H.
- Unity authoring and verification-playbook skill references.
- M1C, M1D, M1F, and M1G task plans.

## Public Contract Changes

- Add presentation-only HandFanLayout, CardInteractionState, DragSession, PlayArea, and TargetSelectionState contracts/components.
- Interaction submits only complete M1D PlayCard commands and receives accepted/rejected authoritative state through the session/adapter seam.

## Schema or Content Changes

None beyond M1 fixture card target categories already defined by M1A/M1D.

## Implementation Steps

1. Bind card views to stable runtime CardInstance IDs and calculate all resting transforms from hand order.
2. Implement hover/focus lift without changing hand order.
3. Implement one-session drag layer reparenting and tested coordinate conversion.
4. Implement responsive threshold arming/disarming and visual-ready cue.
5. Implement target-complete release, staged explicit-target selection, legal target click, and Escape/right-click/cancel recovery.
6. Resynchronize every rejected command or cancellation from authoritative hand state.
7. Add repeated Play Mode interaction/aspect-ratio tests.

## Required Tests

- Five-card and variable hand sizes remain bottom-centred/fanned with no cumulative drift after repeated hover/drag/cancel cycles.
- Drag layer reparenting preserves visual position and creates no duplicate card view.
- Below-threshold release never submits; upward crossing arms; downward return disarms.
- Armed target-complete release submits exactly one complete command.
- Armed explicit-target release stages the card without Mana/pile/RNG mutation; legal target click submits exactly one command.
- Escape/right-click/cancel and rejected command restore the authoritative hand with no cost.
- Click and keyboard routes reach the same semantic interaction states/commands.
- Cards remain in usable bounds at 16:9, 16:10, and ultrawide.

## Validation Commands

- Tools\validate.ps1.
- unity test . --mode EditMode --output Logs\M1H-EditMode-results.xml.
- unity test . --mode PlayMode --output Logs\M1H-PlayMode-results.xml.
- unity command . bloom.scene-summary.

## Visual or Interaction Validation

- Exercise every required DD-28 path in the actual combat scene at all three aspect ratios.
- Confirm armed feedback is not colour-only and target highlights map to independent enemy actors.

## Exit Criteria

- Hand rest geometry is deterministic and never adopts drag transforms.
- Every DD-28 cancellation/rejection path is safe and mutation-free before engine acceptance.
- No general authoritative preview system or M2 behavior is introduced.

## Implementation Discretion

- Fan curve constants, hover interpolation, drag-layer hierarchy, and presentation-only animation helpers may vary if tests prove all contract invariants.

## Stop Conditions

- Any requirement for UI-owned gameplay rules, coordinate-space mixing that cannot be proven safe, production content binding, changed DD-28 behavior, or preview/formula calculation in presentation.

## Worklog Entry Requirements

- Record Canvas/coordinate path, aspect ratios, repeated-interaction evidence, all cancellation/rejection cases, and absence of pre-acceptance mutation.
- Commit expectation: one task-scoped commit after all validation passes.
