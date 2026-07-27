using System.Collections;
using System.Linq;
using Bloomdrawn.Engine.Combat;
using Bloomdrawn.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Bloomdrawn.Tests.PlayMode
{
    public sealed class ACombatStageOrdinaryLaunchPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator CommittedStage_OrdinaryLaunchAndPublicInputReachTerminalCombat()
        {
            yield return SceneManager.LoadSceneAsync("CombatStage", LoadSceneMode.Single);
            yield return WaitForPlayerAction();

            var bootstrap = Object.FindFirstObjectByType<CombatStageRuntimeBootstrap>();
            var hud = Object.FindFirstObjectByType<CombatHudView>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.IsBootstrapped, Is.True);
            Assert.That(bootstrap.CurrentState.Rng.Streams, Is.Not.Empty);
            Assert.That(hud.VisibleCardCount, Is.EqualTo(bootstrap.CurrentState.Deck.Hand.Count));
            Assert.That(Object.FindObjectsByType<CombatActorFallbackView>(FindObjectsSortMode.None), Has.Length.EqualTo(5));
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None), Has.Length.EqualTo(1));

            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var enemyTarget = Object.FindFirstObjectByType<CombatEnemyTargetView>().GetComponent<RectTransform>();
            var endTurn = GameObject.Find("End Turn").GetComponent<RectTransform>();
            for (var round = 0; round < 12 && !bootstrap.CurrentState.IsTerminal; round++)
            {
                yield return WaitForPlayerAction();
                for (var cardAction = 0; cardAction < 5 && !bootstrap.CurrentState.IsTerminal; cardAction++)
                {
                    var targetCard = bootstrap.CurrentState.Deck.Hand.Select((card, index) => new { card, index }).FirstOrDefault(item => item.card.TargetKind == CardTargetKind.OneEnemy);
                    if (targetCard == null) break;
                    yield return Press(keyboard, KeyForHandIndex(targetCard.index));
                    yield return WaitFor(() => bootstrap.InteractionState == CardInteractionState.TargetSelection, 90);
                    Assert.That(bootstrap.ActiveInteractionCardId, Is.EqualTo(targetCard.card.Id));
                    var beforeTargetSelection = bootstrap.CurrentState.CanonicalForm();
                    yield return Click(mouse, enemyTarget);
                    yield return WaitFor(() => bootstrap.CurrentState.CanonicalForm() != beforeTargetSelection, 90);
                    yield return WaitFor(() => bootstrap.CurrentState.IsTerminal || (bootstrap.CurrentState.Phase == CombatPhase.PlayerAction && !bootstrap.Flow.Session.IsInputLocked), 90);
                }
                if (bootstrap.CurrentState.IsTerminal) break;
                var beforeEndTurn = bootstrap.CurrentState.CanonicalForm();
                yield return Click(mouse, endTurn);
                yield return WaitFor(() => bootstrap.CurrentState.CanonicalForm() != beforeEndTurn, 90);
                yield return WaitFor(() => bootstrap.CurrentState.IsTerminal || (bootstrap.CurrentState.Phase == CombatPhase.PlayerAction && !bootstrap.Flow.Session.IsInputLocked), 120);
            }

            Assert.That(bootstrap.CurrentState.IsTerminal, Is.True);
            Assert.That(bootstrap.CurrentState.Phase, Is.EqualTo(CombatPhase.Victory));
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator WaitForPlayerAction()
        {
            yield return WaitFor(() =>
            {
                var bootstrap = Object.FindFirstObjectByType<CombatStageRuntimeBootstrap>();
                return bootstrap != null && bootstrap.IsBootstrapped && bootstrap.CurrentState.Phase == CombatPhase.PlayerAction && !bootstrap.Flow.Session.IsInputLocked;
            }, 120);
        }

        private IEnumerator Press(Keyboard keyboard, Key key)
        {
            Press((ButtonControl)keyboard[key]);
            yield return null;
            yield return null;
            yield return null;
            Release((ButtonControl)keyboard[key]);
            yield return null;
            yield return null;
        }

        private IEnumerator Click(Mouse mouse, RectTransform target)
        {
            var point = RectTransformUtility.WorldToScreenPoint(null, target.position);
            Set(mouse.position, point);
            yield return null;
            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return null;
        }

        private static Key KeyForHandIndex(int index) => index == 0 ? Key.Digit1 : index == 1 ? Key.Digit2 : index == 2 ? Key.Digit3 : index == 3 ? Key.Digit4 : Key.Digit5;

        private static IEnumerator WaitFor(System.Func<bool> predicate, int frames)
        {
            for (var frame = 0; frame < frames; frame++)
            {
                if (predicate()) yield break;
                yield return null;
            }
            var bootstrap = Object.FindFirstObjectByType<CombatStageRuntimeBootstrap>();
            var state = bootstrap == null ? "bootstrap-missing" : bootstrap.CurrentState.Phase + "/locked=" + bootstrap.Flow.Session.IsInputLocked + "/hand=" + bootstrap.CurrentState.Deck.Hand.Count + "/interaction=" + bootstrap.InteractionState + "/active=" + bootstrap.ActiveInteractionCardId + "/rejection=" + bootstrap.LastRejection;
            Assert.Fail("Timed out waiting for the ordinary CombatStage input path: " + state + ".");
        }
    }
}
