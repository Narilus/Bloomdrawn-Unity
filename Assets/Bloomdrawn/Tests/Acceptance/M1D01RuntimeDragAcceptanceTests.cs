using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Bloomdrawn.Engine.Combat;
using Bloomdrawn.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Bloomdrawn.Tests.PlayMode.Acceptance
{
    /// <summary>
    /// Protected M1-D01 gate. It enters the committed ordinary scene and drives only public
    /// Input System devices through the scene's real EventSystem. Product interaction methods
    /// are intentionally observed through public state and never invoked by this harness.
    /// </summary>
    public sealed class M1D01RuntimeDragAcceptanceTests : InputTestFixture
    {
        private const string SceneName = "CombatStage";
        private const string ScenePath = "Assets/Scenes/CombatStage.unity";
        private const float PositionTolerance = 8f;
        private static readonly string EvidenceRoot = Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, "Logs", "M1-D01", "Acceptance", "runtime");

        [UnityTest, Order(1)]
        public IEnumerator C01_OrdinaryBootstrap_RuntimeHealth_UsesCommittedComposition()
        {
            var context = new RuntimeContext();
            yield return LoadOrdinaryRuntime(context);

            Assert.That(SceneManager.GetActiveScene().path.Replace('\\', '/'), Is.EqualTo(ScenePath), "ordinary-bootstrap: wrong committed scene");
            Assert.That(context.Bootstrap.IsBootstrapped, Is.True, "ordinary-bootstrap: automatic runtime bootstrap did not complete");
            Assert.That(context.Bootstrap.CurrentState.Phase, Is.EqualTo(CombatPhase.PlayerAction));
            Assert.That(context.Bootstrap.CurrentState.Rng.Streams, Is.Not.Empty);
            Assert.That(context.Cards.Length, Is.EqualTo(context.Bootstrap.CurrentState.Deck.Hand.Count));
            Assert.That(context.Cards.Length, Is.GreaterThan(0));
            Assert.That(Object.FindObjectsByType<Canvas>(), Has.Length.EqualTo(1), "runtime-health: extra Canvas");
            Assert.That(Object.FindObjectsByType<EventSystem>(), Has.Length.EqualTo(1), "runtime-health: extra EventSystem");
            Assert.That(Object.FindObjectsByType<InputSystemUIInputModule>(), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<CombatActorView>().Select(actor => actor.RuntimeId).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(context.Bootstrap.CurrentState.Setup.Party.Count + context.Bootstrap.CurrentState.Setup.Enemies.Count));
            Evidence("ordinary-bootstrap", "pass", context, "Committed scene automatically produced the fixture session and runtime views.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Order(2)]
        public IEnumerator C02_PointerReachability_BeginDragAndPointerRelationship_AreRealEventSystemInput()
        {
            var context = new RuntimeContext();
            yield return LoadOrdinaryRuntime(context);
            var mouse = InputSystem.AddDevice<Mouse>();
            var card = context.Cards[context.Cards.Length / 2];
            var start = ScreenPoint(card.RectTransform);

            yield return Move(mouse, start);
            var hit = TopRaycast(start);
            Evidence("pointer-reaches-runtime-card", "observed", context, "raycast=" + (hit == null ? "<none>" : Hierarchy(hit)));
            Assert.That(hit, Is.Not.Null, "pointer-reaches-runtime-card: EventSystem raycast found no target");
            Assert.That(hit.transform.IsChildOf(card.transform) || card.transform.IsChildOf(hit.transform), Is.True,
                "pointer-reaches-runtime-card: top raycast did not belong to the runtime card");

            var scaleBefore = card.RectTransform.lossyScale;
            Press(mouse.leftButton);
            yield return Frames(2);
            var dragPoint = start + Vector2.up * Math.Max(24f, EventSystem.current.pixelDragThreshold + 8f);
            yield return Move(mouse, dragPoint, 3);
            var cardPoint = ScreenPoint(card.RectTransform);
            Evidence("drag-position", "observed", context, "pointer=" + V(dragPoint) + ",card=" + V(cardPoint) + ",entity=" + card.GetEntityId());

            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.DraggingDisarmed),
                "pointer-reaches-runtime-card: public pointer input did not dispatch begin/drag to the runtime card");
            Assert.That(context.Bootstrap.ActiveInteractionCardId, Is.EqualTo(card.CardId));
            Assert.That(Object.FindObjectsByType<CombatCardView>(), Does.Contain(card), "active runtime card was destroyed during drag");
            Assert.That(Vector2.Distance(cardPoint, dragPoint), Is.LessThanOrEqualTo(PositionTolerance), "drag-position: card lost pointer relationship or jumped coordinates");
            Assert.That(Vector3.Distance(card.RectTransform.lossyScale, scaleBefore), Is.LessThan(.02f), "drag-position: scale jumped during reparenting");

            Release(mouse.leftButton);
            yield return Frames(3);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Order(3)]
        public IEnumerator C03_HoverFocus_RaisesAndRestoresWithoutAuthoritativeMutation()
        {
            var context = new RuntimeContext();
            yield return LoadOrdinaryRuntime(context);
            var mouse = InputSystem.AddDevice<Mouse>();
            var card = context.Cards[context.Cards.Length / 2];
            var before = Snapshot.Capture(context.Bootstrap);
            var rest = card.RectTransform.position;

            yield return Move(mouse, ScreenPoint(card.RectTransform), 5);
            yield return Capture("1920x1080-hover-observed", 1920, 1080, "hover-focus", card.CardId);
            var raised = card.RectTransform.position.y > rest.y + 4f;
            Evidence("hover-focus", raised ? "pass" : "fail", context, "restY=" + F(rest.y) + ",hoverY=" + F(card.RectTransform.position.y));
            Assert.That(raised, Is.True, "hover-focus: public pointer hover did not visibly raise the runtime card");
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.Hovered));
            Assert.That(context.AcceptedCommandCount, Is.EqualTo(0), "hover-focus: presentation-only hover accepted a gameplay command");
            before.AssertUnchanged(context.Bootstrap, "hover-focus");

            yield return Move(mouse, new Vector2(8, Screen.height - 8), 5);
            Assert.That(Vector3.Distance(card.RectTransform.position, rest), Is.LessThanOrEqualTo(2f), "hover-focus: leaving did not restore calculated fan pose");
            before.AssertUnchanged(context.Bootstrap, "hover-focus leave");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Order(4)]
        public IEnumerator C04_Arm_RemainArmedAboveUpperEdge_AndDownwardDisarm()
        {
            var context = new RuntimeContext();
            yield return LoadOrdinaryRuntime(context);
            var mouse = InputSystem.AddDevice<Mouse>();
            var card = FirstCard(context, requiresTarget: false);
            yield return BeginDrag(mouse, context, card);

            var inside = RectScreenCenter(context.DragLayer.PlayArea);
            yield return Move(mouse, inside, 3);
            var cue = ArmedCue(card);
            yield return Capture("arm-inside", Screen.width, Screen.height, "arm", card.CardId);
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.DraggingArmed), "arm: upward crossing did not enter DraggingArmed");
            Assert.That(cue != null && cue.activeInHierarchy, Is.True, "arm: visible non-colour-only READY cue is absent");

            var aboveUpper = PointAboveUpperEdge(context.DragLayer.PlayArea);
            yield return Move(mouse, aboveUpper, 3);
            yield return Capture("remain-armed-above-upper-edge", Screen.width, Screen.height, "remain-armed-above", card.CardId);
            Evidence("remain-armed-above", context.Bootstrap.InteractionState == CardInteractionState.DraggingArmed ? "pass" : "fail", context,
                "pointer=" + V(aboveUpper) + ",playAreaTop=" + F(RectScreenBounds(context.DragLayer.PlayArea).yMax));
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.DraggingArmed),
                "remain-armed-above: pointer above the Play Area upper edge became disarmed despite remaining above the responsive threshold");

            var below = PointBelowThreshold(context.DragLayer.PlayArea);
            yield return Move(mouse, below, 3);
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.DraggingDisarmed), "disarm: returning below threshold did not disarm");
            Assert.That(cue.activeInHierarchy, Is.False, "disarm: armed cue remained visible");
            Release(mouse.leftButton);
            yield return Frames(3);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Order(5)]
        public IEnumerator C05_ReleaseBelow_CancelsWithoutAnyAuthoritativeMutationOrDuplicateView()
        {
            var context = new RuntimeContext();
            yield return LoadOrdinaryRuntime(context);
            var mouse = InputSystem.AddDevice<Mouse>();
            var card = FirstCard(context, requiresTarget: false);
            var cardId = card.CardId;
            var before = Snapshot.Capture(context.Bootstrap);

            yield return BeginDrag(mouse, context, card);
            yield return Move(mouse, RectScreenCenter(context.DragLayer.PlayArea), 2);
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.DraggingArmed));
            yield return Move(mouse, PointBelowThreshold(context.DragLayer.PlayArea), 2);
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.DraggingDisarmed));
            var releasePointer = mouse.position.ReadValue();
            Release(mouse.leftButton);
            yield return Frames(4);

            before.AssertUnchanged(context.Bootstrap, "release-below-no-mutation");
            Assert.That(context.AcceptedCommandCount, Is.EqualTo(0), "release-below-no-mutation: cancellation accepted a command");
            Assert.That(context.Bootstrap.InteractionState == CardInteractionState.Resting ||
                        context.Bootstrap.InteractionState == CardInteractionState.Hovered,
                Is.True,
                "release-below-no-mutation: interaction remained dragging, armed, disarmed, or in target selection");
            AssertSingleViewsMatchHand(context.Bootstrap, "release-below-no-mutation");
            Assert.That(ActiveViews(cardId), Has.Count.EqualTo(1), "release-below-no-mutation: detached/duplicate view remained after cancellation");
            AssertRestoredFanPose(context.Bootstrap, cardId,
                context.Bootstrap.InteractionState == CardInteractionState.Hovered && context.Bootstrap.ActiveInteractionCardId == cardId,
                "release-below-no-mutation");

            var postCancelPointer = mouse.position.ReadValue();
            Assert.That(Vector2.Distance(postCancelPointer, releasePointer), Is.LessThanOrEqualTo(.01f),
                "release-below-no-mutation: public pointer moved after release");
            var raycastTarget = TopRaycast(postCancelPointer);
            var raycastCard = raycastTarget == null ? null : raycastTarget.GetComponentInParent<CombatCardView>();
            var hoveredCardId = context.Bootstrap.InteractionState == CardInteractionState.Hovered
                ? context.Bootstrap.ActiveInteractionCardId
                : null;
            if (context.Bootstrap.InteractionState == CardInteractionState.Hovered)
            {
                Assert.That(raycastCard, Is.Not.Null,
                    "release-below-no-mutation: Hovered is valid only when the unchanged public pointer raycasts a restored runtime card");
                Assert.That(raycastCard.gameObject.activeInHierarchy, Is.True,
                    "release-below-no-mutation: Hovered raycast target is not an active restored runtime card");
                Assert.That(ActiveViews(raycastCard.CardId), Has.Count.EqualTo(1),
                    "release-below-no-mutation: Hovered raycast target does not have exactly one active runtime view");
                Assert.That(hoveredCardId, Is.EqualTo(raycastCard.CardId),
                    "release-below-no-mutation: interaction hovered-card identity does not match the real EventSystem raycast target");
            }

            Evidence("release-below-no-mutation", "pass", context,
                "card=" + cardId +
                ",postCancelPointer=" + V(postCancelPointer) +
                ",raycastTarget=" + (raycastTarget == null ? "<none>" : Hierarchy(raycastTarget)) +
                ",hoveredRuntimeCardId=" + (hoveredCardId ?? "<none>") +
                ",raycastRuntimeCardId=" + (raycastCard == null ? "<none>" : raycastCard.CardId) +
                ",cancelledCardIsRaycastTarget=" + (raycastCard != null && raycastCard.CardId == cardId) +
                ",zeroMutation=true,singleViews=true,fanRestored=true");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Order(6)]
        public IEnumerator C06_TargetCompleteArmedRelease_SubmitsExactlyOneAcceptedCommand()
        {
            var context = new RuntimeContext();
            yield return LoadOrdinaryRuntime(context);
            var mouse = InputSystem.AddDevice<Mouse>();
            var card = FirstCard(context, requiresTarget: false);
            var id = card.CardId;
            var before = Snapshot.Capture(context.Bootstrap);

            yield return BeginDrag(mouse, context, card);
            yield return Move(mouse, RectScreenCenter(context.DragLayer.PlayArea), 3);
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.DraggingArmed));
            Release(mouse.leftButton);
            yield return Frames(3);

            var after = Snapshot.Capture(context.Bootstrap);
            Assert.That(after.Canonical, Is.Not.EqualTo(before.Canonical), "target-complete-once: accepted transition did not occur");
            Assert.That(after.Hand, Does.Not.Contain(id), "target-complete-once: played card remained in hand");
            Assert.That(after.EventCount, Is.GreaterThan(before.EventCount));
            Assert.That(context.Bootstrap.Flow.Session.EventHistory.Skip(before.EventCount).Count(e => e.Kind == "combat.card-played" && e.SourceId == card.OwnerId), Is.EqualTo(1),
                "target-complete-once: expected exactly one accepted card-play transition");
            Assert.That(context.AcceptedCommandCount, Is.EqualTo(1), "target-complete-once: expected exactly one accepted command");
            Evidence("target-complete-once", "pass", context, "card=" + id + ",eventDelta=" + (after.EventCount - before.EventCount));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Order(7)]
        public IEnumerator C07_ExplicitTargetRelease_StagesOneCardHighlightsTargetsWithoutMutation()
        {
            var context = new RuntimeContext();
            yield return LoadOrdinaryRuntime(context);
            var mouse = InputSystem.AddDevice<Mouse>();
            var card = FirstCard(context, requiresTarget: true);
            var id = card.CardId;
            var before = Snapshot.Capture(context.Bootstrap);

            yield return BeginDrag(mouse, context, card);
            yield return Move(mouse, RectScreenCenter(context.DragLayer.PlayArea), 3);
            Release(mouse.leftButton);
            yield return Frames(3);
            yield return Capture("explicit-target-stage", Screen.width, Screen.height, "explicit-target-stage", id);

            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.TargetSelection));
            before.AssertUnchanged(context.Bootstrap, "explicit-target-stage");
            Assert.That(context.AcceptedCommandCount, Is.EqualTo(0), "explicit-target-stage: staging prematurely accepted a command");
            Assert.That(ActiveViews(id), Has.Count.EqualTo(1), "explicit-target-stage: staged card also has a duplicate interactive hand view");
            Assert.That(HighlightedTargets(), Is.GreaterThan(0), "explicit-target-stage: no legal independent enemy target is highlighted");
            Evidence("explicit-target-stage", "pass", context, "card=" + id + ",highlighted=" + HighlightedTargets());
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Order(8)]
        public IEnumerator C08_TargetCancellation_EscapeAndRightClick_RestoreWithoutMutation()
        {
            yield return VerifyTargetCancel(useEscape: true);
            yield return VerifyTargetCancel(useEscape: false);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Order(9)]
        public IEnumerator C09_LegalTargetPointerSelection_SubmitsExactlyOneCompleteCommand()
        {
            var context = new RuntimeContext();
            yield return LoadOrdinaryRuntime(context);
            var mouse = InputSystem.AddDevice<Mouse>();
            var card = FirstCard(context, requiresTarget: true);
            var id = card.CardId;
            var owner = card.OwnerId;
            var before = Snapshot.Capture(context.Bootstrap);

            yield return BeginDrag(mouse, context, card);
            yield return Move(mouse, RectScreenCenter(context.DragLayer.PlayArea), 3);
            Release(mouse.leftButton);
            yield return Frames(3);
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.TargetSelection));
            before.AssertUnchanged(context.Bootstrap, "legal-target-once staging");

            var target = Object.FindAnyObjectByType<CombatEnemyTargetView>();
            var intended = target.GetComponentInParent<CombatActorView>().RuntimeId;
            yield return Click(mouse, target.GetComponent<RectTransform>(), MouseButton.Left);
            yield return Frames(3);
            var after = Snapshot.Capture(context.Bootstrap);
            Assert.That(after.Canonical, Is.Not.EqualTo(before.Canonical));
            Assert.That(context.Bootstrap.Flow.Session.EventHistory.Skip(before.EventCount).Count(e => e.Kind == "combat.card-played" && e.SourceId == owner), Is.EqualTo(1),
                "legal-target-once: legal public target click did not produce exactly one complete accepted command");
            Assert.That(context.AcceptedCommandCount, Is.EqualTo(1), "legal-target-once: expected exactly one accepted command");
            Assert.That(context.Bootstrap.Flow.Session.EventHistory.Skip(before.EventCount).Any(e => e.TargetId == intended), Is.True,
                "legal-target-once: accepted events do not identify the intended independent enemy");
            Assert.That(after.Hand, Does.Not.Contain(id));
            Evidence("legal-target-once", "pass", context, "card=" + id + ",enemy=" + intended);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Order(10)]
        public IEnumerator C10_RepeatedPublicDragCancelCycles_HaveNoDriftDuplicatesOrStaleViews()
        {
            var context = new RuntimeContext();
            yield return LoadOrdinaryRuntime(context);
            var mouse = InputSystem.AddDevice<Mouse>();
            var id = FirstCard(context, requiresTarget: false).CardId;
            var before = Snapshot.Capture(context.Bootstrap);
            var initial = RestPose(id);

            for (var cycle = 1; cycle <= 10; cycle++)
            {
                var card = ActiveViews(id).Single();
                yield return Move(mouse, ScreenPoint(card.RectTransform), 2);
                yield return BeginDrag(mouse, context, card);
                yield return Move(mouse, RectScreenCenter(context.DragLayer.PlayArea), 2);
                yield return Move(mouse, PointBelowThreshold(context.DragLayer.PlayArea), 2);
                Release(mouse.leftButton);
                yield return Frames(3);

                before.AssertUnchanged(context.Bootstrap, "repeated-cycles " + cycle);
                Assert.That(context.AcceptedCommandCount, Is.EqualTo(0), "repeated-cycles: cancellation accepted a command at cycle " + cycle);
                AssertSingleViewsMatchHand(context.Bootstrap, "repeated-cycles " + cycle);
                var current = RestPose(id);
                Assert.That(Vector2.Distance(current.Position, initial.Position), Is.LessThanOrEqualTo(1f), "repeated-cycles: cumulative positional drift at cycle " + cycle);
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(current.Rotation, initial.Rotation)), Is.LessThanOrEqualTo(.25f), "repeated-cycles: cumulative rotational drift at cycle " + cycle);
                Assert.That(IsInUsableBounds(ActiveViews(id).Single().RectTransform), Is.True, "repeated-cycles: card became off-screen/unrecoverable at cycle " + cycle);
            }
            Evidence("repeated-cycles", "pass", context, "cycles=10,card=" + id);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Order(11)]
        public IEnumerator C11_ClickCompatibility_TargetCompleteAndExplicitTargetRoutesRemainFunctional()
        {
            var context = new RuntimeContext();
            yield return LoadOrdinaryRuntime(context);
            var mouse = InputSystem.AddDevice<Mouse>();
            var complete = FirstCard(context, requiresTarget: false);
            var beforeComplete = context.Bootstrap.Flow.Session.EventHistory.Count;
            yield return Click(mouse, complete.RectTransform, MouseButton.Left);
            yield return Frames(3);
            Assert.That(context.Bootstrap.Flow.Session.EventHistory.Skip(beforeComplete).Count(e => e.Kind == "combat.card-played"), Is.EqualTo(1));

            yield return WaitForPlayerAction(context.Bootstrap);
            var explicitCard = Object.FindObjectsByType<CombatCardView>().First(view => view.RequiresEnemyTarget);
            var beforeExplicit = context.Bootstrap.Flow.Session.EventHistory.Count;
            yield return Click(mouse, explicitCard.RectTransform, MouseButton.Left);
            yield return Frames(2);
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.TargetSelection));
            yield return Click(mouse, Object.FindAnyObjectByType<CombatEnemyTargetView>().GetComponent<RectTransform>(), MouseButton.Left);
            yield return Frames(3);
            Assert.That(context.Bootstrap.Flow.Session.EventHistory.Skip(beforeExplicit).Count(e => e.Kind == "combat.card-played"), Is.EqualTo(1));
            Evidence("click-compatibility", "pass", context, "target-complete and explicit-target click routes each submitted once");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Order(12)]
        public IEnumerator C12_KeyboardCompatibility_NumberEscapeEnterAndSpaceRemainFunctional()
        {
            var context = new RuntimeContext();
            yield return LoadOrdinaryRuntime(context);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var explicitIndex = Array.FindIndex(context.Cards, card => card.RequiresEnemyTarget);
            Assert.That(explicitIndex, Is.InRange(0, 4));
            var before = Snapshot.Capture(context.Bootstrap);
            yield return KeyPress(keyboard[Digit(explicitIndex)]);
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.TargetSelection));
            before.AssertUnchanged(context.Bootstrap, "keyboard explicit staging");
            yield return KeyPress(keyboard.escapeKey);
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.Resting));
            before.AssertUnchanged(context.Bootstrap, "keyboard Escape cancellation");

            yield return KeyPress(keyboard[Digit(explicitIndex)]);
            var eventCount = context.Bootstrap.Flow.Session.EventHistory.Count;
            yield return KeyPress(keyboard.enterKey);
            yield return Frames(3);
            Assert.That(context.Bootstrap.Flow.Session.EventHistory.Skip(eventCount).Count(e => e.Kind == "combat.card-played"), Is.EqualTo(1));

            yield return WaitForPlayerAction(context.Bootstrap);
            var stateBeforeSpace = context.Bootstrap.CurrentState.CanonicalForm();
            yield return KeyPress(keyboard.spaceKey);
            yield return Frames(2);
            Assert.That(context.Bootstrap.CurrentState.CanonicalForm(), Is.Not.EqualTo(stateBeforeSpace), "keyboard-compatibility: current Space behavior regressed");
            Evidence("keyboard-compatibility", "pass", context, "number/Escape/Enter/Space routes observed");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Order(13)]
        public IEnumerator C13_ResponsiveRuntimeEvidence_ExercisesActualViewsAtAllRequiredResolutions()
        {
            var failures = new List<string>();
            var sizes = new[] { new Vector2Int(1920, 1080), new Vector2Int(1920, 1200), new Vector2Int(3440, 1440) };
            foreach (var size in sizes)
            {
                Assert.That(AcceptanceGameViewResolution.TrySet(size.x, size.y), Is.True,
                    "responsive-layout: required runtime Game View resolution is unavailable");
                yield return WaitFor(() => Screen.width == size.x && Screen.height == size.y, 60,
                    "Game View resolution " + size.x + "x" + size.y);
                var context = new RuntimeContext();
                yield return LoadOrdinaryRuntime(context);
                var mouse = InputSystem.AddDevice<Mouse>();
                var explicitCard = FirstCard(context, requiresTarget: true);
                var rest = explicitCard.RectTransform.position.y;

                yield return Move(mouse, ScreenPoint(explicitCard.RectTransform), 4);
                var hoverRaised = explicitCard.RectTransform.position.y > rest + 4f;
                yield return Capture(size.x + "x" + size.y + "-resting-hover", size.x, size.y, "hover-focus", explicitCard.CardId);
                if (!hoverRaised) failures.Add(size.x + "x" + size.y + ": hover rise absent");

                yield return BeginDrag(mouse, context, explicitCard);
                yield return Move(mouse, RectScreenCenter(context.DragLayer.PlayArea), 3);
                var armed = context.Bootstrap.InteractionState == CardInteractionState.DraggingArmed && ArmedCue(explicitCard).activeInHierarchy;
                yield return Capture(size.x + "x" + size.y + "-armed", size.x, size.y, "arm", explicitCard.CardId);
                if (!armed) failures.Add(size.x + "x" + size.y + ": armed cue absent");
                Release(mouse.leftButton);
                yield return Frames(3);
                var staged = context.Bootstrap.InteractionState == CardInteractionState.TargetSelection && HighlightedTargets() > 0;
                yield return Capture(size.x + "x" + size.y + "-target-stage", size.x, size.y, "explicit-target-stage", explicitCard.CardId);
                if (!staged) failures.Add(size.x + "x" + size.y + ": target stage/highlight absent");

                foreach (var rect in RequiredRuntimeRects())
                    if (!IsInUsableBounds(rect)) failures.Add(size.x + "x" + size.y + ": off-screen " + Hierarchy(rect.gameObject));
                if (Screen.width != size.x || Screen.height != size.y)
                    failures.Add(size.x + "x" + size.y + ": actual runtime size was " + Screen.width + "x" + Screen.height);
                Evidence("responsive-layout", failures.Count == 0 ? "pass" : "observed", context,
                    "requested=" + size.x + "x" + size.y + ",actual=" + Screen.width + "x" + Screen.height);

                yield return KeyPress(InputSystem.AddDevice<Keyboard>().escapeKey);
                yield return Frames(2);
            }

            Assert.That(failures, Is.Empty, "responsive-layout: " + string.Join("; ", failures));
            LogAssert.NoUnexpectedReceived();
        }

        private IEnumerator VerifyTargetCancel(bool useEscape)
        {
            var context = new RuntimeContext();
            yield return LoadOrdinaryRuntime(context);
            var mouse = InputSystem.AddDevice<Mouse>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var card = FirstCard(context, requiresTarget: true);
            var id = card.CardId;
            var before = Snapshot.Capture(context.Bootstrap);
            yield return BeginDrag(mouse, context, card);
            yield return Move(mouse, RectScreenCenter(context.DragLayer.PlayArea), 3);
            Release(mouse.leftButton);
            yield return Frames(3);
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.TargetSelection));
            before.AssertUnchanged(context.Bootstrap, "target-cancel staging");

            if (useEscape)
                yield return KeyPress(keyboard.escapeKey);
            else
            {
                var staged = ActiveViews(id).OrderByDescending(view => view.transform.GetSiblingIndex()).First();
                yield return Click(mouse, staged.RectTransform, MouseButton.Right);
            }
            yield return Frames(3);
            before.AssertUnchanged(context.Bootstrap, useEscape ? "target-cancel Escape" : "target-cancel right-click");
            Assert.That(context.AcceptedCommandCount, Is.EqualTo(0), "target-cancel: cancellation accepted a command");
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.Resting));
            Assert.That(ActiveViews(id), Has.Count.EqualTo(1));
            AssertSingleViewsMatchHand(context.Bootstrap, "target-cancel");
            Evidence("target-cancel", "pass", context, useEscape ? "Escape" : "right-click");
        }

        private static IEnumerator LoadOrdinaryRuntime(RuntimeContext context)
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return WaitFor(() =>
            {
                var bootstrap = Object.FindAnyObjectByType<CombatStageRuntimeBootstrap>();
                return bootstrap != null && bootstrap.IsBootstrapped && bootstrap.CurrentState != null &&
                       bootstrap.CurrentState.Phase == CombatPhase.PlayerAction && !bootstrap.Flow.Session.IsInputLocked &&
                       Object.FindObjectsByType<CombatCardView>().Length > 0;
            }, 180, "ordinary CombatStage automatic bootstrap");
            context.Bootstrap = Object.FindAnyObjectByType<CombatStageRuntimeBootstrap>();
            context.DragLayer = Object.FindAnyObjectByType<CardDragLayer>();
            context.Bootstrap.Flow.StateChanged += submission =>
            {
                if (submission.IsAccepted) context.AcceptedCommandCount++;
                else context.RejectedCommandCount++;
            };
            context.Cards = Object.FindObjectsByType<CombatCardView>()
                .OrderBy(card => context.Bootstrap.CurrentState.Deck.Hand.ToList().FindIndex(instance => instance.Id == card.CardId)).ToArray();
        }

        private static IEnumerator WaitForPlayerAction(CombatStageRuntimeBootstrap bootstrap)
        {
            yield return WaitFor(() => bootstrap.CurrentState.IsTerminal || (bootstrap.CurrentState.Phase == CombatPhase.PlayerAction && !bootstrap.Flow.Session.IsInputLocked),
                180, "player action and presentation unlock");
        }

        private IEnumerator BeginDrag(Mouse mouse, RuntimeContext context, CombatCardView card)
        {
            var start = ScreenPoint(card.RectTransform);
            yield return Move(mouse, start, 2);
            Press(mouse.leftButton);
            yield return Frames(2);
            yield return Move(mouse, start + Vector2.up * Math.Max(24f, EventSystem.current.pixelDragThreshold + 8f), 3);
            Assert.That(context.Bootstrap.ActiveInteractionCardId, Is.EqualTo(card.CardId), "public pointer drag did not retain active runtime card identity");
        }

        private IEnumerator Move(Mouse mouse, Vector2 point, int frames = 1)
        {
            Set(mouse.position, point);
            yield return Frames(frames);
        }

        private IEnumerator Click(Mouse mouse, RectTransform target, MouseButton button)
        {
            yield return Move(mouse, ScreenPoint(target), 2);
            var control = button == MouseButton.Right ? mouse.rightButton : mouse.leftButton;
            Press(control);
            yield return Frames(2);
            Release(control);
            yield return Frames(2);
        }

        private IEnumerator KeyPress(ButtonControl key)
        {
            Press(key);
            yield return Frames(2);
            Release(key);
            yield return Frames(2);
        }

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++) yield return null;
        }

        private static IEnumerator WaitFor(Func<bool> predicate, int frames, string description)
        {
            for (var i = 0; i < frames; i++)
            {
                if (predicate()) yield break;
                yield return null;
            }
            Assert.Fail("Timed out waiting for " + description + ".");
        }

        private static CombatCardView FirstCard(RuntimeContext context, bool requiresTarget)
        {
            var card = context.Cards.FirstOrDefault(view => view.RequiresEnemyTarget == requiresTarget);
            Assert.That(card, Is.Not.Null, "Ordinary runtime hand lacks the required generic target category.");
            return card;
        }

        private static Vector2 ScreenPoint(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.position);

        private static Rect RectScreenBounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Vector2 RectScreenCenter(RectTransform rect) => RectScreenBounds(rect).center;
        private static Vector2 PointAboveUpperEdge(RectTransform playArea)
        {
            var bounds = RectScreenBounds(playArea);
            return new Vector2(bounds.center.x, Math.Min(Screen.height - 24f, bounds.yMax + Math.Max(40f, bounds.height * .25f)));
        }
        private static Vector2 PointBelowThreshold(RectTransform playArea)
        {
            var bounds = RectScreenBounds(playArea);
            return new Vector2(bounds.center.x, Math.Max(24f, bounds.yMin - 50f));
        }

        private static GameObject ArmedCue(CombatCardView card)
        {
            var child = card.transform.Find("Armed Cue");
            return child == null ? null : child.gameObject;
        }

        private static GameObject TopRaycast(Vector2 point)
        {
            var data = new PointerEventData(EventSystem.current) { position = point };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);
            return results.Count == 0 ? null : results[0].gameObject;
        }

        private static int HighlightedTargets()
        {
            return Object.FindObjectsByType<CombatEnemyTargetView>()
                .Count(target => target.GetComponentInChildren<Image>() != null && target.GetComponentInChildren<Image>().color.a > .2f);
        }

        private static IReadOnlyList<CombatCardView> ActiveViews(string cardId)
        {
            return Object.FindObjectsByType<CombatCardView>().Where(view => view.CardId == cardId && view.gameObject.activeInHierarchy).ToList();
        }

        private static void AssertSingleViewsMatchHand(CombatStageRuntimeBootstrap bootstrap, string criterion)
        {
            var views = Object.FindObjectsByType<CombatCardView>().Where(view => view.gameObject.activeInHierarchy).ToList();
            var hand = bootstrap.CurrentState.Deck.Hand.Select(card => card.Id).ToList();
            Assert.That(views.Count, Is.EqualTo(hand.Count), criterion + ": active view count differs from authoritative hand count");
            foreach (var id in hand) Assert.That(views.Count(view => view.CardId == id), Is.EqualTo(1), criterion + ": expected exactly one active view for " + id);
            Assert.That(views.Distinct().Count(), Is.EqualTo(views.Count), criterion + ": duplicate/stale view references");
        }

        private static RestingPose RestPose(string cardId)
        {
            var card = ActiveViews(cardId).Single();
            return new RestingPose(card.RectTransform.anchoredPosition, card.RectTransform.localEulerAngles.z);
        }

        private static void AssertRestoredFanPose(CombatStageRuntimeBootstrap bootstrap, string cardId, bool hovered, string criterion)
        {
            var hand = bootstrap.CurrentState.Deck.Hand.ToList();
            var index = hand.FindIndex(instance => instance.Id == cardId);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), criterion + ": cancelled card is absent from authoritative hand");
            var card = ActiveViews(cardId).Single();
            var handContainer = card.RectTransform.parent as RectTransform;
            Assert.That(handContainer, Is.Not.Null, criterion + ": restored runtime card is not parented to the authoritative hand container");
            var width = handContainer.rect.width <= 1 ? 1040 : handContainer.rect.width;
            var expected = HandFanLayout.Calculate(hand.Count, width, 188f, 22f, 8f)[index];
            var expectedPosition = expected.Position + (hovered ? Vector2.up * 32f : Vector2.zero);
            Assert.That(Vector2.Distance(card.RectTransform.anchoredPosition, expectedPosition), Is.LessThanOrEqualTo(1f),
                criterion + ": cancelled card did not return to its recalculated authoritative fan pose");
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(card.RectTransform.localEulerAngles.z, expected.Rotation)), Is.LessThanOrEqualTo(.25f),
                criterion + ": cancelled card rotation did not return to its recalculated authoritative fan pose");
        }

        private static bool IsInUsableBounds(RectTransform rect)
        {
            var bounds = RectScreenBounds(rect);
            return bounds.xMax > 0 && bounds.xMin < Screen.width && bounds.yMax > 0 && bounds.yMin < Screen.height &&
                   bounds.center.x >= 0 && bounds.center.x <= Screen.width && bounds.center.y >= 0 && bounds.center.y <= Screen.height;
        }

        private static IEnumerable<RectTransform> RequiredRuntimeRects()
        {
            foreach (var card in Object.FindObjectsByType<CombatCardView>()) yield return card.RectTransform;
            var playArea = Object.FindAnyObjectByType<CardDragLayer>();
            if (playArea != null) yield return playArea.PlayArea;
            foreach (var target in Object.FindObjectsByType<CombatEnemyTargetView>()) yield return target.GetComponent<RectTransform>();
            var endTurn = GameObject.Find("End Turn");
            if (endTurn != null) yield return endTurn.GetComponent<RectTransform>();
        }

        private static Key Digit(int index)
        {
            return index == 0 ? Key.Digit1 : index == 1 ? Key.Digit2 : index == 2 ? Key.Digit3 : index == 3 ? Key.Digit4 : Key.Digit5;
        }

        private static IEnumerator Capture(string name, int width, int height, string state, string cardId)
        {
            Directory.CreateDirectory(Path.Combine(EvidenceRoot, "screenshots"));
            var file = Path.Combine(EvidenceRoot, "screenshots", Safe(name) + ".png");
            ScreenCapture.CaptureScreenshot(file);
            EvidenceRecord.Write(new EvidenceRecord
            {
                criterion = "visual-evidence",
                result = "captured",
                detail = file,
                width = width,
                height = height,
                interactionState = state,
                activeCardId = cardId,
                sequence = EvidenceRecord.NextSequence()
            });
            yield return new WaitForEndOfFrame();
            yield return null;
        }

        private static void Evidence(string criterion, string result, RuntimeContext context, string detail)
        {
            EvidenceRecord.Write(new EvidenceRecord
            {
                criterion = criterion,
                result = result,
                detail = detail,
                width = Screen.width,
                height = Screen.height,
                interactionState = context.Bootstrap == null ? "unavailable" : context.Bootstrap.InteractionState.ToString(),
                activeCardId = context.Bootstrap == null ? null : context.Bootstrap.ActiveInteractionCardId,
                sequence = EvidenceRecord.NextSequence()
            });
        }

        private static string Hierarchy(GameObject value)
        {
            if (value == null) return "<null>";
            var parts = new List<string>();
            for (var current = value.transform; current != null; current = current.parent) parts.Add(current.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string Safe(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value;
        }
        private static string V(Vector2 value) => F(value.x) + "," + F(value.y);
        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private enum MouseButton { Left, Right }
        private sealed class RuntimeContext
        {
            public CombatStageRuntimeBootstrap Bootstrap;
            public CardDragLayer DragLayer;
            public CombatCardView[] Cards;
            public int AcceptedCommandCount;
            public int RejectedCommandCount;
        }

        private readonly struct RestingPose
        {
            public RestingPose(Vector2 position, float rotation) { Position = position; Rotation = rotation; }
            public Vector2 Position { get; }
            public float Rotation { get; }
        }

        private sealed class Snapshot
        {
            public string Canonical;
            public string Hand;
            public string Mana;
            public string Piles;
            public string Events;
            public string Rng;
            public int EventCount;

            public static Snapshot Capture(CombatStageRuntimeBootstrap bootstrap)
            {
                var state = bootstrap.CurrentState;
                return new Snapshot
                {
                    Canonical = state.CanonicalForm(),
                    Hand = string.Join(",", state.Deck.Hand.Select(card => card.Id)),
                    Mana = state.Mana.Current + "/" + state.Mana.Maximum,
                    Piles = Pile(state.Deck.Draw) + "|" + Pile(state.Deck.Hand) + "|" + Pile(state.Deck.Discard) + "|" + Pile(state.Deck.Graveyard) + "|" + Pile(state.Deck.Resolving),
                    EventCount = bootstrap.Flow.Session.EventHistory.Count,
                    Events = string.Join(";", bootstrap.Flow.Session.EventHistory.Select(e => e.Sequence + ":" + e.Kind + ":" + e.SourceId + ":" + e.TargetId)),
                    Rng = string.Join(",", state.Rng.Streams.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + ":" + pair.Value.State))
                };
            }

            public void AssertUnchanged(CombatStageRuntimeBootstrap bootstrap, string criterion)
            {
                var after = Capture(bootstrap);
                Assert.That(after.Canonical, Is.EqualTo(Canonical), criterion + ": canonical combat state mutated");
                Assert.That(after.Hand, Is.EqualTo(Hand), criterion + ": authoritative hand order mutated");
                Assert.That(after.Mana, Is.EqualTo(Mana), criterion + ": Mana mutated");
                Assert.That(after.Piles, Is.EqualTo(Piles), criterion + ": pile contents mutated");
                Assert.That(after.EventCount, Is.EqualTo(EventCount), criterion + ": gameplay event count mutated");
                Assert.That(after.Events, Is.EqualTo(Events), criterion + ": gameplay event order mutated");
                Assert.That(after.Rng, Is.EqualTo(Rng), criterion + ": named RNG stream state mutated");
            }

            private static string Pile(IEnumerable<CardInstance> cards) => string.Join(",", cards.Select(card => card.Id));
        }

        [Serializable]
        private sealed class EvidenceRecord
        {
            private static int nextSequence;
            public string criterion;
            public string result;
            public string detail;
            public int width;
            public int height;
            public string interactionState;
            public string activeCardId;
            public int sequence;
            public static int NextSequence() => ++nextSequence;
            public static void Write(EvidenceRecord record)
            {
                Directory.CreateDirectory(EvidenceRoot);
                File.AppendAllText(Path.Combine(EvidenceRoot, "public-input-trace.ndjson"), JsonUtility.ToJson(record) + Environment.NewLine);
            }
        }
    }

    internal static class AcceptanceGameViewResolution
    {
        public static bool TrySet(int width, int height)
        {
#if UNITY_EDITOR
            var editorAssembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            var sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            var sizeKindType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            var groupKindType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
            var gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            if (sizesType == null || sizeType == null || sizeKindType == null || groupKindType == null || gameViewType == null) return false;

            var singletonType = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singletonType.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);
            var getGroup = sizesType.GetMethod("GetGroup", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var group = getGroup?.Invoke(instance, new[] { Enum.ToObject(groupKindType, 0) });
            if (group == null) return false;

            var getBuiltinCount = group.GetType().GetMethod("GetBuiltinCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var getCustomCount = group.GetType().GetMethod("GetCustomCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var getGameViewSize = group.GetType().GetMethod("GetGameViewSize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var addCustomSize = group.GetType().GetMethod("AddCustomSize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var builtinCount = (int)getBuiltinCount.Invoke(group, null);
            var customCount = (int)getCustomCount.Invoke(group, null);
            var label = "M1-D01 " + width + "x" + height;
            var selectedIndex = -1;
            for (var index = 0; index < builtinCount + customCount; index++)
            {
                var existing = getGameViewSize.Invoke(group, new object[] { index });
                var existingWidth = (int)sizeType.GetProperty("width").GetValue(existing);
                var existingHeight = (int)sizeType.GetProperty("height").GetValue(existing);
                if (existingWidth == width && existingHeight == height) { selectedIndex = index; break; }
            }
            if (selectedIndex < 0)
            {
                var constructor = sizeType.GetConstructor(new[] { sizeKindType, typeof(int), typeof(int), typeof(string) });
                if (constructor == null || addCustomSize == null) return false;
                var value = constructor.Invoke(new[] { Enum.ToObject(sizeKindType, 1), (object)width, height, label });
                addCustomSize.Invoke(group, new[] { value });
                selectedIndex = builtinCount + customCount;
            }

            var window = UnityEditor.EditorWindow.GetWindow(gameViewType);
            var selectedProperty = gameViewType.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (window == null || selectedProperty == null) return false;
            selectedProperty.SetValue(window, selectedIndex);
            window.Repaint();
            return true;
#else
            return false;
#endif
        }
    }
}
