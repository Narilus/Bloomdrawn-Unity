using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bloomdrawn.Engine.Combat;
using Bloomdrawn.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bloomdrawn.Tests.PlayMode
{
    public sealed class M1D01RuntimeDragRegressionPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator PublicDragDisarmReleaseBelow_RestoresSingleViewsWithoutMutation()
        {
            var context = new RuntimeContext();
            yield return LoadOrdinaryRuntime(context);

            var mouse = InputSystem.AddDevice<Mouse>();
            var card = context.Cards.First(view => !view.RequiresEnemyTarget);
            var before = Snapshot.Capture(context.Bootstrap);

            yield return BeginDrag(mouse, context, card);
            yield return Move(mouse, RectScreenCenter(context.DragLayer.PlayArea), 3);
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.DraggingArmed));

            yield return Move(mouse, PointBelowThreshold(context.DragLayer.PlayArea), 3);
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.DraggingDisarmed));

            Release(mouse.leftButton);
            yield return Frames(4);

            before.AssertUnchanged(context.Bootstrap, "release-below regression");
            Assert.That(context.AcceptedCommandCount, Is.EqualTo(0), "release-below regression accepted a command");
            AssertSettledAfterRelease(context.Bootstrap, mouse.position.ReadValue());
            AssertSingleViewsMatchHand(context.Bootstrap, "release-below regression");
            AssertRestoredCardPose(context.Bootstrap, card.CardId, "release-below regression");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PublicExplicitTargetReleaseThenEscape_RestoresOneViewWithoutMutation()
        {
            var context = new RuntimeContext();
            yield return LoadOrdinaryRuntime(context);

            var mouse = InputSystem.AddDevice<Mouse>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var card = context.Cards.First(view => view.RequiresEnemyTarget);
            var before = Snapshot.Capture(context.Bootstrap);

            yield return BeginDrag(mouse, context, card);
            yield return Move(mouse, RectScreenCenter(context.DragLayer.PlayArea), 3);
            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.DraggingArmed));

            Release(mouse.leftButton);
            yield return Frames(4);

            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.TargetSelection));
            Assert.That(context.Bootstrap.ActiveInteractionCardId, Is.EqualTo(card.CardId));
            before.AssertUnchanged(context.Bootstrap, "explicit-target staging regression");
            Assert.That(context.AcceptedCommandCount, Is.EqualTo(0), "explicit-target staging regression accepted a command");
            Assert.That(ActiveViews(card.CardId), Has.Count.EqualTo(1), "explicit-target staging regression duplicated the staged view");
            AssertSingleViewsMatchHand(context.Bootstrap, "explicit-target staging regression");

            Press(keyboard.escapeKey);
            yield return Frames(2);
            Release(keyboard.escapeKey);
            yield return Frames(4);

            Assert.That(context.Bootstrap.InteractionState, Is.EqualTo(CardInteractionState.Resting));
            before.AssertUnchanged(context.Bootstrap, "explicit-target cancellation regression");
            Assert.That(context.AcceptedCommandCount, Is.EqualTo(0), "explicit-target cancellation regression accepted a command");
            AssertSingleViewsMatchHand(context.Bootstrap, "explicit-target cancellation regression");
            Assert.That(ActiveViews(card.CardId), Has.Count.EqualTo(1), "explicit-target cancellation regression lost or duplicated the card view");
            AssertRestoredCardPose(context.Bootstrap, card.CardId, "explicit-target cancellation regression");
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator LoadOrdinaryRuntime(RuntimeContext context)
        {
            yield return SceneManager.LoadSceneAsync("CombatStage", LoadSceneMode.Single);
            yield return WaitFor(() =>
            {
                var bootstrap = UnityEngine.Object.FindFirstObjectByType<CombatStageRuntimeBootstrap>();
                return bootstrap != null && bootstrap.IsBootstrapped && bootstrap.CurrentState != null &&
                       bootstrap.CurrentState.Phase == CombatPhase.PlayerAction &&
                       !bootstrap.Flow.Session.IsInputLocked &&
                       UnityEngine.Object.FindObjectsByType<CombatCardView>(FindObjectsSortMode.None).Length > 0;
            }, 180, "ordinary CombatStage automatic bootstrap");

            context.Bootstrap = UnityEngine.Object.FindFirstObjectByType<CombatStageRuntimeBootstrap>();
            context.DragLayer = UnityEngine.Object.FindFirstObjectByType<CardDragLayer>();
            context.Bootstrap.Flow.StateChanged += submission =>
            {
                if (submission.IsAccepted) context.AcceptedCommandCount++;
            };
            context.Cards = UnityEngine.Object.FindObjectsByType<CombatCardView>(FindObjectsSortMode.None)
                .OrderBy(view => context.Bootstrap.CurrentState.Deck.Hand.ToList().FindIndex(instance => instance.Id == view.CardId))
                .ToArray();

            var inputModule = UnityEngine.Object.FindFirstObjectByType<InputSystemUIInputModule>();
            Assert.That(inputModule, Is.Not.Null, "ordinary CombatStage has no Input System UI module");
            inputModule.UnassignActions();
            inputModule.AssignDefaultActions();

            yield return WaitFor(() =>
            {
                var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
                var inputModules = UnityEngine.Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsSortMode.None);
                return eventSystems.Length == 1 && EventSystem.current == eventSystems[0] &&
                       inputModules.Length == 1 && inputModules[0].isActiveAndEnabled &&
                       inputModules[0].actionsAsset != null;
            }, 180, "ordinary CombatStage EventSystem/Input System readiness");
            yield return Frames(2);
        }

        private IEnumerator BeginDrag(Mouse mouse, RuntimeContext context, CombatCardView card)
        {
            var start = ScreenPoint(card.RectTransform);
            yield return Move(mouse, start, 2);

            var raycastTarget = TopRaycast(start);
            Assert.That(raycastTarget, Is.Not.Null, "public drag regression did not raycast a runtime card");
            Assert.That(raycastTarget.GetComponentInParent<CombatCardView>(), Is.EqualTo(card),
                "public drag regression raycast did not target the selected runtime card");

            Press(mouse.leftButton);
            yield return Frames(2);
            yield return Move(mouse, start + Vector2.up * Math.Max(24f, EventSystem.current.pixelDragThreshold + 8f), 3);

            Assert.That(context.Bootstrap.ActiveInteractionCardId, Is.EqualTo(card.CardId),
                "public drag regression lost the runtime card identity");
        }

        private IEnumerator Move(Mouse mouse, Vector2 point, int frames = 1)
        {
            Set(mouse.position, point);
            yield return Frames(frames);
        }

        private static IEnumerator Frames(int count)
        {
            for (var index = 0; index < count; index++) yield return null;
        }

        private static IEnumerator WaitFor(Func<bool> predicate, int frames, string description)
        {
            for (var index = 0; index < frames; index++)
            {
                if (predicate()) yield break;
                yield return null;
            }

            Assert.Fail("Timed out waiting for " + description + ".");
        }

        private static void AssertSettledAfterRelease(CombatStageRuntimeBootstrap bootstrap, Vector2 unchangedPointer)
        {
            Assert.That(bootstrap.InteractionState == CardInteractionState.Resting ||
                        bootstrap.InteractionState == CardInteractionState.Hovered,
                Is.True,
                "release-below regression remained in a drag, armed, disarmed, or target-selection state");

            if (bootstrap.InteractionState != CardInteractionState.Hovered) return;

            var raycastTarget = TopRaycast(unchangedPointer);
            var raycastCard = raycastTarget == null ? null : raycastTarget.GetComponentInParent<CombatCardView>();
            Assert.That(raycastCard, Is.Not.Null,
                "release-below regression accepted Hovered without a real restored EventSystem card raycast");
            Assert.That(raycastCard.gameObject.activeInHierarchy, Is.True,
                "release-below regression hovered an inactive runtime card");
            Assert.That(bootstrap.ActiveInteractionCardId, Is.EqualTo(raycastCard.CardId),
                "release-below regression hovered-card identity did not match the EventSystem raycast target");
        }

        private static void AssertSingleViewsMatchHand(CombatStageRuntimeBootstrap bootstrap, string criterion)
        {
            var views = UnityEngine.Object.FindObjectsByType<CombatCardView>(FindObjectsSortMode.None)
                .Where(view => view.gameObject.activeInHierarchy)
                .ToList();
            var hand = bootstrap.CurrentState.Deck.Hand.Select(instance => instance.Id).ToList();

            Assert.That(views.Count, Is.EqualTo(hand.Count), criterion + ": active view count differs from authoritative hand count");
            foreach (var id in hand)
                Assert.That(views.Count(view => view.CardId == id), Is.EqualTo(1), criterion + ": expected exactly one active view for " + id);
        }

        private static void AssertRestoredCardPose(CombatStageRuntimeBootstrap bootstrap, string cardId, string criterion)
        {
            var hand = bootstrap.CurrentState.Deck.Hand.ToList();
            var index = hand.FindIndex(instance => instance.Id == cardId);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), criterion + ": card is absent from the authoritative hand");

            var card = ActiveViews(cardId).Single();
            Assert.That(card.RectTransform.IsChildOf(UnityEngine.Object.FindFirstObjectByType<CardDragLayer>().DragLayer), Is.False,
                criterion + ": restored card remained in the dedicated drag layer");
            var handContainer = card.RectTransform.parent as RectTransform;
            Assert.That(handContainer, Is.Not.Null, criterion + ": restored card has no runtime hand parent");

            var width = handContainer.rect.width <= 1f ? 1040f : handContainer.rect.width;
            var expected = HandFanLayout.Calculate(hand.Count, width, 188f, 22f, 8f)[index];
            var hovered = bootstrap.InteractionState == CardInteractionState.Hovered && bootstrap.ActiveInteractionCardId == cardId;
            var expectedPosition = expected.Position + (hovered ? Vector2.up * 32f : Vector2.zero);
            Assert.That(Vector2.Distance(card.RectTransform.anchoredPosition, expectedPosition), Is.LessThanOrEqualTo(1f),
                criterion + ": restored card did not return to its calculated fan pose");
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(card.RectTransform.localEulerAngles.z, expected.Rotation)), Is.LessThanOrEqualTo(.25f),
                criterion + ": restored card rotation did not return to its calculated fan pose");
        }

        private static IReadOnlyList<CombatCardView> ActiveViews(string cardId)
        {
            return UnityEngine.Object.FindObjectsByType<CombatCardView>(FindObjectsSortMode.None)
                .Where(view => view.CardId == cardId && view.gameObject.activeInHierarchy)
                .ToList();
        }

        private static GameObject TopRaycast(Vector2 point)
        {
            var data = new PointerEventData(EventSystem.current) { position = point };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);
            return results.Count == 0 ? null : results[0].gameObject;
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

        private static Vector2 PointBelowThreshold(RectTransform playArea)
        {
            var bounds = RectScreenBounds(playArea);
            return new Vector2(bounds.center.x, Mathf.Max(24f, bounds.yMin - 50f));
        }

        private sealed class RuntimeContext
        {
            public CombatStageRuntimeBootstrap Bootstrap;
            public CardDragLayer DragLayer;
            public CombatCardView[] Cards;
            public int AcceptedCommandCount;
        }

        private sealed class Snapshot
        {
            private string canonical;
            private string hand;
            private string mana;
            private string piles;
            private string events;
            private string rng;
            private int eventCount;

            public static Snapshot Capture(CombatStageRuntimeBootstrap bootstrap)
            {
                var state = bootstrap.CurrentState;
                return new Snapshot
                {
                    canonical = state.CanonicalForm(),
                    hand = string.Join(",", state.Deck.Hand.Select(card => card.Id)),
                    mana = state.Mana.Current + "/" + state.Mana.Maximum,
                    piles = Pile(state.Deck.Draw) + "|" + Pile(state.Deck.Hand) + "|" + Pile(state.Deck.Discard) + "|" +
                            Pile(state.Deck.Graveyard) + "|" + Pile(state.Deck.Resolving),
                    eventCount = bootstrap.Flow.Session.EventHistory.Count,
                    events = string.Join(";", bootstrap.Flow.Session.EventHistory.Select(e => e.Sequence + ":" + e.Kind + ":" + e.SourceId + ":" + e.TargetId)),
                    rng = string.Join(",", state.Rng.Streams.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                        .Select(pair => pair.Key + ":" + pair.Value.State))
                };
            }

            public void AssertUnchanged(CombatStageRuntimeBootstrap bootstrap, string criterion)
            {
                var after = Capture(bootstrap);
                Assert.That(after.canonical, Is.EqualTo(canonical), criterion + ": canonical combat state mutated");
                Assert.That(after.hand, Is.EqualTo(hand), criterion + ": authoritative hand order mutated");
                Assert.That(after.mana, Is.EqualTo(mana), criterion + ": Mana mutated");
                Assert.That(after.piles, Is.EqualTo(piles), criterion + ": pile contents mutated");
                Assert.That(after.eventCount, Is.EqualTo(eventCount), criterion + ": gameplay event count mutated");
                Assert.That(after.events, Is.EqualTo(events), criterion + ": gameplay event history mutated");
                Assert.That(after.rng, Is.EqualTo(rng), criterion + ": named RNG stream state mutated");
            }

            private static string Pile(IEnumerable<CardInstance> cards) => string.Join(",", cards.Select(card => card.Id));
        }
    }
}
