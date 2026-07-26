using System;
using System.IO;
using System.Linq;
using Bloomdrawn.Content;
using Bloomdrawn.Content.Editor;
using Bloomdrawn.Engine.Combat;
using NUnit.Framework;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class EnemyIntentSequenceTests
    {
        [Test]
        public void SetupCreatesStableSlotsAndInitialIntentWithoutLifecycleInM1A()
        {
            var first = CombatStateMachine.Create(Setup()); var second = CombatStateMachine.Create(Setup());
            Assert.That(first.EnemySlots.Select(slot => slot.SlotIndex + "|" + slot.EnemyId.Value + "|" + slot.Intent.Kind + "|" + slot.Intent.Damage), Is.EqualTo(second.EnemySlots.Select(slot => slot.SlotIndex + "|" + slot.EnemyId.Value + "|" + slot.Intent.Kind + "|" + slot.Intent.Damage)));
            Assert.That(first.EnemySlots.Single().Intent.Kind, Is.EqualTo("attack"));
        }

        [Test]
        public void EnemyActionsResolveOneAtATimeInStableSlotOrderThenRegenerate()
        {
            var state = BeginEnemyPhase(TwoEnemySetup());
            var first = CombatStateMachine.Apply(state, new CombatCommand(CombatCommandKind.AdvanceEnemyAction));
            Assert.That(first.IsAccepted, Is.True); Assert.That(first.State.Phase, Is.EqualTo(CombatPhase.EnemyAction));
            Assert.That(first.Events.First().Facts["slotIndex"], Is.EqualTo("0")); Assert.That(first.State.Values.Party.CurrentHp, Is.EqualTo(first.State.Values.Party.MaximumHp - 7));
            var second = CombatStateMachine.Apply(first.State, new CombatCommand(CombatCommandKind.AdvanceEnemyAction));
            Assert.That(second.State.Phase, Is.EqualTo(CombatPhase.EnemyEnd));
            Assert.That(second.Events.First().Facts["slotIndex"], Is.EqualTo("1"));
            Assert.That(second.Events.Last().Kind, Is.EqualTo("combat.enemy-intents-regenerated"));
            Assert.That(second.State.NextEnemySlotIndex, Is.EqualTo(0));
        }

        [Test]
        public void TerminalEnemyActionStopsLaterSlotsAndRegeneration()
        {
            var opened = BeginEnemyPhase(TwoEnemySetup());
            var lethalValues = new CombatValues(new PartyCombatValues(opened.Values.Party.MaximumHp, 7, 0), opened.Values.Enemies);
            var state = CombatStateMachine.WithCardPlayState(opened, opened.Deck, opened.Mana, opened.NextEventSequence, lethalValues);
            var result = CombatStateMachine.Apply(state, new CombatCommand(CombatCommandKind.AdvanceEnemyAction));
            Assert.That(result.State.Phase, Is.EqualTo(CombatPhase.Defeat));
            Assert.That(result.Events.Select(item => item.Kind), Does.Not.Contain("combat.enemy-intents-regenerated"));
            Assert.That(result.State.NextEnemySlotIndex, Is.EqualTo(1));
        }

        [Test]
        public void InvalidPhaseDoesNotPermitPresentationOrSlotControl()
        {
            var state = CombatStateMachine.Create(Setup());
            var result = CombatStateMachine.Apply(state, new CombatCommand(CombatCommandKind.AdvanceEnemyAction));
            Assert.That(result.IsAccepted, Is.False); Assert.That(result.State, Is.SameAs(state)); Assert.That(result.Events, Is.Empty); Assert.That(result.Rejection.Code, Is.EqualTo("enemy-action.illegal-phase"));
        }

        private static CombatState BeginEnemyPhase(CombatSetupResult setup)
        {
            var opened = CombatStateMachine.Apply(CombatStateMachine.Create(setup), new CombatCommand(CombatCommandKind.BeginCombat));
            return CombatStateMachine.Apply(opened.State, new CombatCommand(CombatCommandKind.EndTurn)).State;
        }
        private static CombatSetupResult TwoEnemySetup()
        {
            var setup = Setup(); var first = setup.Enemies[0];
            var second = new FixtureEnemySetup(new RuntimeEnemyId(first.RuntimeId.Value + ".second"), first.DefinitionId, first.MaxHp, new InitialEnemyIntent(first.InitialIntent.Kind, first.InitialIntent.Damage));
            return new CombatSetupResult(setup.LineupId, setup.EncounterId, setup.Party, setup.DeckRecipe, new[] { first, second });
        }
        private static CombatSetupResult Setup()
        {
            var root = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            var imported = ContentImportService.ImportDirectory(Path.Combine(root, "GameContent", "fixtures"), ContentOrigin.Fixture);
            Assert.That(imported.IsValid, Is.True); return FixtureCombatSetupFactory.Create(FixtureCombatCatalog.Create(imported.Content), new CombatSetupRequest("fixture.m1.lineup.quartet", "fixture.m1.encounter.training"));
        }
    }
}
