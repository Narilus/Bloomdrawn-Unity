using System;
using System.IO;
using System.Linq;
using Bloomdrawn.Content;
using Bloomdrawn.Content.Editor;
using Bloomdrawn.Engine.Combat;
using Bloomdrawn.Engine.Rng;
using NUnit.Framework;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class ManaAndCardPlayTests
    {
        [Test]
        public void Mana_BaseMaximumIsSixAndFinalCostHasZeroFloor()
        {
            Assert.That(ManaState.Full().Maximum, Is.EqualTo(6));
            Assert.That(ManaState.Full().Current, Is.EqualTo(6));
            Assert.That(ManaState.CalculateFinalCost(1, -4), Is.EqualTo(0));
            Assert.That(ManaState.CalculateFinalCost(2, 3), Is.EqualTo(5));
        }

        [Test]
        public void PartyTargetCard_AcceptsWithNoExplicitTargetAndMovesOnlyToResolving()
        {
            var state = OpenedState();
            var card = state.Deck.Hand.First(candidate => candidate.TargetKind == CardTargetKind.Party);

            var result = CardPlayRules.Apply(state, new PlayCardCommand(card.Id, card.OwnerId, CardTargetChoice.None()));

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.State.Deck.Hand.Any(candidate => candidate.Id == card.Id), Is.False);
            Assert.That(result.State.Deck.Resolving.Single().Id, Is.EqualTo(card.Id));
            Assert.That(result.State.Mana.Current, Is.EqualTo(5));
            Assert.That(result.Events.Single().Kind, Is.EqualTo("combat.card-played"));
            Assert.That(result.Events.Single().Facts["finalCost"], Is.EqualTo("1"));
        }

        [Test]
        public void OneEnemyCard_RequiresOneLegalCompleteTarget()
        {
            var state = OpenedState();
            var card = state.Deck.Hand.First(candidate => candidate.TargetKind == CardTargetKind.OneEnemy);

            AssertRejectedUnchanged(state, new PlayCardCommand(card.Id, card.OwnerId, CardTargetChoice.None()), "card-play.missing-target");
            AssertRejectedUnchanged(state, new PlayCardCommand(card.Id, card.OwnerId, CardTargetChoice.OneEnemy(new RuntimeEnemyId("combat.enemy.other"))), "card-play.wrong-target");

            var accepted = CardPlayRules.Apply(state, new PlayCardCommand(card.Id, card.OwnerId, CardTargetChoice.OneEnemy(state.Setup.Enemies[0].RuntimeId)));
            Assert.That(accepted.IsAccepted, Is.True);
            Assert.That(accepted.Events.Single().TargetId, Is.EqualTo(state.Setup.Enemies[0].RuntimeId.Value));
        }

        [Test]
        public void WrongPhaseNonHandWrongOwnerAndInsufficientMana_RejectWithoutMutationOrEvents()
        {
            var setupState = CombatStateMachine.Create(Setup());
            var unopenedCard = setupState.Deck.Draw[0];
            AssertRejectedUnchanged(setupState, new PlayCardCommand(unopenedCard.Id, unopenedCard.OwnerId, CardTargetChoice.None()), "card-play.illegal-phase");

            var opened = OpenedState();
            var card = opened.Deck.Hand.First(candidate => candidate.TargetKind == CardTargetKind.Party);
            AssertRejectedUnchanged(opened, new PlayCardCommand("combat.card.absent", card.OwnerId, CardTargetChoice.None()), "card-play.not-in-hand");
            AssertRejectedUnchanged(opened, new PlayCardCommand(card.Id, new RuntimeParticipantId("combat.party.other"), CardTargetChoice.None()), "card-play.wrong-owner");

            var noMana = CombatStateMachine.WithCardPlayState(opened, opened.Deck, new ManaState(6, 0), opened.NextEventSequence);
            AssertRejectedUnchanged(noMana, new PlayCardCommand(card.Id, card.OwnerId, CardTargetChoice.None()), "card-play.insufficient-mana");
        }

        [Test]
        public void RejectionDoesNotConsumeRngOrExposePresentationInteractionState()
        {
            var state = OpenedState();
            var card = state.Deck.Hand.First(candidate => candidate.TargetKind == CardTargetKind.OneEnemy);
            var rng = AuthoritativeRngState.Create(17, 29);
            var before = rng.Clone();

            AssertRejectedUnchanged(state, new PlayCardCommand(card.Id, card.OwnerId, CardTargetChoice.None()), "card-play.missing-target");

            Assert.That(rng.Streams.Keys.All(key => rng.Streams[key].State == before.Streams[key].State), Is.True);
            var propertyNames = typeof(CombatState).GetProperties().Select(property => property.Name).ToArray();
            Assert.That(propertyNames, Does.Not.Contain("Hover"));
            Assert.That(propertyNames, Does.Not.Contain("Drag"));
            Assert.That(propertyNames, Does.Not.Contain("Armed"));
            Assert.That(propertyNames, Does.Not.Contain("StagedCard"));
            Assert.That(propertyNames, Does.Not.Contain("TargetSelection"));
        }

        private static void AssertRejectedUnchanged(CombatState state, PlayCardCommand command, string code)
        {
            var canonical = state.CanonicalForm();
            var result = CardPlayRules.Apply(state, command);
            Assert.That(result.IsAccepted, Is.False);
            Assert.That(result.State, Is.SameAs(state));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.Rejection.Code, Is.EqualTo(code));
            Assert.That(state.CanonicalForm(), Is.EqualTo(canonical));
        }

        private static CombatState OpenedState()
        {
            var opening = CombatStateMachine.Apply(CombatStateMachine.Create(Setup()), new CombatCommand(CombatCommandKind.BeginCombat));
            Assert.That(opening.IsAccepted, Is.True);
            return opening.State;
        }

        private static CombatSetupResult Setup()
        {
            var root = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            var validation = ContentImportService.ImportDirectory(Path.Combine(root, "GameContent", "fixtures"), ContentOrigin.Fixture);
            Assert.That(validation.IsValid, Is.True);
            return FixtureCombatSetupFactory.Create(FixtureCombatCatalog.Create(validation.Content), new CombatSetupRequest("fixture.m1.lineup.quartet", "fixture.m1.encounter.training"));
        }
    }
}
