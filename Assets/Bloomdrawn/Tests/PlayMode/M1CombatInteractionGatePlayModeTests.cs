using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bloomdrawn.Application;
using Bloomdrawn.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Bloomdrawn.Tests.PlayMode
{
    public sealed class M1CombatInteractionGatePlayModeTests
    {
        [UnityTest]
        public IEnumerator FanDragTargetingAndActorSequence_RemainStableAcrossRequiredAspectRatios()
        {
            foreach (var size in new[] { new Vector2(1920, 1080), new Vector2(1920, 1200), new Vector2(3440, 1440) })
            {
                var fan = HandFanLayout.Calculate(5, size.x);
                Assert.That(fan[2].Position.x, Is.EqualTo(size.x * .5f));
                Assert.That(fan.All(pose => pose.Position.x >= 0 && pose.Position.x <= size.x), Is.True);
            }
            var sink = new Sink(); var interaction = new CardInteractionController(sink);
            for (var cycle = 0; cycle < 4; cycle++)
            {
                interaction.Hover("card." + cycle); interaction.BeginDrag("card." + cycle, "party.0", false); interaction.UpdateArmed(true); interaction.UpdateArmed(false);
                Assert.That(interaction.Release(), Is.False); Assert.That(interaction.State, Is.EqualTo(CardInteractionState.Resting));
            }
            interaction.BeginDrag("party-card", "party.0", false); interaction.UpdateArmed(true); Assert.That(interaction.Release(), Is.True);
            interaction.BeginDrag("target-card", "party.1", true); interaction.UpdateArmed(true); Assert.That(interaction.Release(), Is.False); Assert.That(interaction.State, Is.EqualTo(CardInteractionState.TargetSelection)); interaction.Cancel(); Assert.That(sink.Submissions.Count, Is.EqualTo(1));
            sink.Accept = false; interaction.BeginDrag("rejected", "party.2", true); interaction.UpdateArmed(true); interaction.Release(); Assert.That(interaction.SelectEnemy("enemy.0"), Is.False); Assert.That(interaction.State, Is.EqualTo(CardInteractionState.Resting));
            var party = Actor("party.0"); var enemy = Actor("enemy.0"); var presenter = new CombatTokenPresenter(new CombatActorLookup(new[] { party.GetComponent<CombatActorView>(), enemy.GetComponent<CombatActorView>() }));
            presenter.Present(new PresentationToken(10, PresentationTokenKind.CardPlayed, "party.0", "enemy.0", PresentationReaction.OwnerAcknowledgement, PresentationReaction.None, null), false, 1f);
            presenter.Present(new PresentationToken(11, PresentationTokenKind.Damage, "party.0", "enemy.0", PresentationReaction.Act, PresentationReaction.Hit, null), true, 2f);
            yield return null;
            Assert.That(party.GetComponent<CombatActorTokenReaction>().ReactionCount, Is.EqualTo(2)); Assert.That(enemy.GetComponent<CombatActorTokenReaction>().LastReaction, Is.EqualTo(PresentationReaction.Hit));
            Object.Destroy(party); Object.Destroy(enemy);
        }
        private static GameObject Actor(string id) { var actor = new GameObject(id, typeof(CombatActorView), typeof(CombatActorTokenReaction)); var view = actor.GetComponent<CombatActorView>(); view.Configure(id, actor.transform, actor.transform, actor.transform, actor.transform, actor.transform, actor.transform); return actor; }
        private sealed class Sink : ICompleteCardCommandSink { public bool Accept = true; public List<CardCommandSubmission> Submissions = new List<CardCommandSubmission>(); public bool Submit(CardCommandSubmission value) { Submissions.Add(value); return Accept; } }
    }
}
