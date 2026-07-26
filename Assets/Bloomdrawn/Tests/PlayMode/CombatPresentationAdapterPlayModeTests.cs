using System.Collections;
using Bloomdrawn.Application;
using Bloomdrawn.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Bloomdrawn.Tests.PlayMode
{
    public sealed class CombatPresentationAdapterPlayModeTests
    {
        [UnityTest]
        public IEnumerator ReducedMotionAndSpeedHooks_PreserveIndependentActorTokenOrder()
        {
            var actor = new GameObject("Actor", typeof(CombatActorView), typeof(CombatActorTokenReaction));
            actor.GetComponent<CombatActorView>().Configure("combat.enemy.fixture.0", actor.transform, actor.transform, actor.transform, actor.transform, actor.transform, actor.transform);
            var presenter = new CombatTokenPresenter(new CombatActorLookup(new[] { actor.GetComponent<CombatActorView>() }));
            presenter.Present(new PresentationToken(2, PresentationTokenKind.EnemyAction, "combat.enemy.fixture.0", "combat.party", PresentationReaction.Act, PresentationReaction.None, null), false, 2f);
            presenter.Present(new PresentationToken(3, PresentationTokenKind.Damage, "combat.enemy.fixture.0", "combat.party", PresentationReaction.Act, PresentationReaction.None, null), true, 0f);
            yield return null;
            var fallback = actor.GetComponent<CombatActorTokenReaction>();
            Assert.That(fallback.ReactionCount, Is.EqualTo(2));
            Assert.That(fallback.LastReaction, Is.EqualTo(PresentationReaction.Act));
            Object.Destroy(actor);
        }
    }
}
