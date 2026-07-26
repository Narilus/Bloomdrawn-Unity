using System.Collections;
using System.Linq;
using Bloomdrawn.Application;
using Bloomdrawn.Content;
using Bloomdrawn.Engine.Combat;
using Bloomdrawn.Engine.Rng;
using Bloomdrawn.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bloomdrawn.Tests.PlayMode
{
    public sealed class CombatStageFixtureSequencePlayModeTests
    {
        [UnityTest]
        public IEnumerator LoadedCombatStage_BindsFixtureSessionAndPresentsCompleteCombatInOrder()
        {
            yield return SceneManager.LoadSceneAsync("CombatStage", LoadSceneMode.Single);
            yield return null;
            var stage = Object.FindFirstObjectByType<CombatPresentationController>();
            Assert.That(stage, Is.Not.Null);
            var session = new CombatSession(CombatStateMachine.Create(Setup(), AuthoritativeRngState.Create(71, 913)));
            stage.ConfigurePlayback(false, 1f); stage.BindSession(session);
            Assert.That(session.Submit(new CombatCommand(CombatCommandKind.BeginCombat)).IsAccepted, Is.True); Drain(stage);
            while (!session.CurrentState.IsTerminal)
            {
                var card = session.CurrentState.Deck.Hand.First(candidate => candidate.TargetKind == CardTargetKind.OneEnemy);
                var result = session.Submit(new PlayCardCommand(card.Id, card.OwnerId, CardTargetChoice.OneEnemy(session.CurrentState.Setup.Enemies[0].RuntimeId)));
                Assert.That(result.IsAccepted, Is.True); Drain(stage);
            }
            Assert.That(session.CurrentState.Phase, Is.EqualTo(CombatPhase.Victory));
            var enemy = Object.FindObjectsByType<CombatActorView>(FindObjectsSortMode.None).Single(actor => actor.RuntimeId == session.CurrentState.Setup.Enemies[0].RuntimeId.Value);
            Assert.That(enemy.GetComponent<CombatActorTokenReaction>().ReactionCount, Is.GreaterThan(0));
            Assert.That(session.IsInputLocked, Is.False);
        }
        private static void Drain(CombatPresentationController stage) { while (stage.PresentNext()) { } }
        private static CombatSetupResult Setup()
        {
            var party = new[] { 0, 1, 2, 3 }.Select(index => new FixturePartyMember(new RuntimeParticipantId("runtime.party." + index), "fixture.party." + index, 20, 20, 2)).ToList();
            var deck = party.Select((member, index) => new FixtureDeckRecipeEntry(index, "fixture.card.strike." + index, member.RuntimeId, 1, "oneEnemy", "strike")).ToList();
            var enemy = new FixtureEnemySetup(new RuntimeEnemyId("runtime.enemy.0"), "fixture.enemy", 50, new InitialEnemyIntent("attack", 1));
            return new CombatSetupResult("fixture.runtime.lineup", "fixture.runtime.encounter", party, deck, new[] { enemy });
        }
    }
}
