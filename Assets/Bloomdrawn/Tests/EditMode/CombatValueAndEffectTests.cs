using System;
using System.IO;
using System.Linq;
using Bloomdrawn.Content;
using Bloomdrawn.Content.Editor;
using Bloomdrawn.Engine.Combat;
using NUnit.Framework;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class CombatValueAndEffectTests
    {
        [Test]
        public void StrikeUsesOwnerAttackAndShieldUsesOwnerDefense()
        {
            var state = Opened();
            var strike = state.Deck.Hand.First(card => card.DefinitionId == "fixture.m1.card.alpha-strike");
            var strikeResult = CardPlayRules.Apply(state, new PlayCardCommand(strike.Id, strike.OwnerId, CardTargetChoice.OneEnemy(state.Setup.Enemies[0].RuntimeId)));
            Assert.That(strikeResult.Events.Last().Facts["hpDamageDealt"], Is.EqualTo("6"));
            var shield = strikeResult.State.Deck.Hand.First(card => card.DefinitionId == "fixture.m1.card.alpha-shield");
            var shieldResult = CardPlayRules.Apply(strikeResult.State, new PlayCardCommand(shield.Id, shield.OwnerId, CardTargetChoice.None()));
            Assert.That(shieldResult.State.Values.Party.Shield, Is.EqualTo(3));
            Assert.That(shieldResult.Events.Last().Kind, Is.EqualTo("combat.shield-gained"));
        }

        [Test]
        public void ShieldHealingAndHpLossRemainExplicitAndDeterministic()
        {
            var state = Opened();
            var owner = state.Setup.Party[0].RuntimeId;
            var enemy = state.Setup.Enemies[0].RuntimeId;
            var result = CombatEffectResolver.Resolve(state, new[]
            {
                new CombatAtomicEffect(AtomicEffectKind.GainPartyShield, 4, owner),
                new CombatAtomicEffect(AtomicEffectKind.DamageParty, 6, owner, sourceKind: "enemy-intent"),
                new CombatAtomicEffect(AtomicEffectKind.HealParty, 999, owner),
                new CombatAtomicEffect(AtomicEffectKind.LosePartyHp, 2, owner, sourceKind: "hp-loss")
            });
            Assert.That(result.Events.Select(item => item.Kind), Is.EqualTo(new[] { "combat.shield-gained", "combat.damage-dealt", "combat.healing-applied", "combat.hp-loss-applied" }));
            Assert.That(result.Events[1].Facts["shieldAbsorbed"], Is.EqualTo("4"));
            Assert.That(result.Events[1].Facts["hpDamageDealt"], Is.EqualTo("2"));
            Assert.That(result.Events[3].Facts["shieldAbsorbed"], Is.EqualTo("0"));
            Assert.That(result.State.Values.Party.CurrentHp, Is.EqualTo(result.State.Values.Party.MaximumHp - 2));
            Assert.That(result.State.Values.Enemies.Single(enemyValue => enemyValue.RuntimeId.Equals(enemy)).CurrentHp, Is.EqualTo(72));
        }

        [Test]
        public void AtomicStopStopsLaterEffectsAndDefeatWinsSimultaneousTerminalCheckpoint()
        {
            var state = Opened();
            var owner = state.Setup.Party[0].RuntimeId;
            var enemy = state.Setup.Enemies[0];
            var first = CombatEffectResolver.Resolve(state, new[]
            {
                new CombatAtomicEffect(AtomicEffectKind.DamageEnemy, enemy.MaxHp, owner, enemy.RuntimeId),
                new CombatAtomicEffect(AtomicEffectKind.GainPartyShield, 9, owner)
            });
            Assert.That(first.State.Phase, Is.EqualTo(CombatPhase.Victory));
            Assert.That(first.State.Values.Party.Shield, Is.EqualTo(0));
            Assert.That(first.Events.Select(item => item.Kind), Is.EqualTo(new[] { "combat.damage-dealt", "combat.victory" }));

            var lethal = CombatEffectResolver.Resolve(state, new[]
            {
                new CombatAtomicEffect(AtomicEffectKind.DamageEnemy, enemy.MaxHp, owner, enemy.RuntimeId),
                new CombatAtomicEffect(AtomicEffectKind.LosePartyHp, state.Values.Party.MaximumHp, owner)
            });
            Assert.That(lethal.State.Phase, Is.EqualTo(CombatPhase.Victory));

            var simultaneousValues = new CombatValues(
                new PartyCombatValues(state.Values.Party.MaximumHp, 0, 0),
                state.Values.Enemies.Select(value => new EnemyCombatValues(value.RuntimeId, value.MaximumHp, 0, value.Shield)).ToList());
            var simultaneousState = CombatStateMachine.WithCardPlayState(state, state.Deck, state.Mana, state.NextEventSequence, simultaneousValues);
            var simultaneous = CombatEffectResolver.Resolve(simultaneousState, new[] { new CombatAtomicEffect(AtomicEffectKind.GainPartyShield, 1, owner) });
            Assert.That(simultaneous.State.Phase, Is.EqualTo(CombatPhase.Defeat));
        }

        [Test]
        public void SameSetupAndEffectsProduceIdenticalChecksumReadyStateAndTrace()
        {
            var owner = Setup().Party[0].RuntimeId;
            var first = CombatEffectResolver.Resolve(Opened(), new[] { new CombatAtomicEffect(AtomicEffectKind.DamageParty, 5, owner) });
            var second = CombatEffectResolver.Resolve(Opened(), new[] { new CombatAtomicEffect(AtomicEffectKind.DamageParty, 5, owner) });
            Assert.That(second.State.CanonicalForm(), Is.EqualTo(first.State.CanonicalForm()));
            Assert.That(second.Events.Select(item => item.CanonicalForm()), Is.EqualTo(first.Events.Select(item => item.CanonicalForm())));
        }

        private static CombatState Opened() => CombatStateMachine.Apply(CombatStateMachine.Create(Setup()), new CombatCommand(CombatCommandKind.BeginCombat)).State;
        private static CombatSetupResult Setup()
        {
            var root = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            var content = ContentImportService.ImportDirectory(Path.Combine(root, "GameContent", "fixtures"), ContentOrigin.Fixture);
            Assert.That(content.IsValid, Is.True); return FixtureCombatSetupFactory.Create(FixtureCombatCatalog.Create(content.Content), new CombatSetupRequest("fixture.m1.lineup.quartet", "fixture.m1.encounter.training"));
        }
    }
}
