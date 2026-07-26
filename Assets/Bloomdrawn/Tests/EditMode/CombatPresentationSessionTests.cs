using System.IO;
using System.Linq;
using Bloomdrawn.Application;
using Bloomdrawn.Content;
using Bloomdrawn.Content.Editor;
using Bloomdrawn.Editor.Tooling;
using Bloomdrawn.Engine.Commands;
using Bloomdrawn.Engine.Combat;
using Bloomdrawn.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class CombatPresentationSessionTests
    {
        [Test]
        public void OrderedM1Events_MapToOrderedTokensWithAuthoritativeFactsOnly()
        {
            var events = new[]
            {
                new GameEvent(4, "combat.card-played", "party.0", "enemy.0"),
                new GameEvent(5, "combat.damage-dealt", "party.0", "enemy.0"),
                new GameEvent(6, "combat.enemy-action-started", "enemy.0", "combat.party"),
                new GameEvent(7, "combat.victory")
            };
            var tokens = events.Select(GameEventPresentationTokenMapper.Map).ToList();
            Assert.That(tokens.Select(token => token.EventSequence), Is.EqualTo(new long[] { 4, 5, 6, 7 }));
            Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[] { PresentationTokenKind.CardPlayed, PresentationTokenKind.Damage, PresentationTokenKind.EnemyAction, PresentationTokenKind.Victory }));
            Assert.That(tokens[0].SourceReaction, Is.EqualTo(PresentationReaction.OwnerAcknowledgement));
            Assert.That(tokens[1].TargetReaction, Is.EqualTo(PresentationReaction.Hit));
            Assert.That(tokens[2].SourceRuntimeId, Is.EqualTo("enemy.0"));
        }

        [Test]
        public void AcceptedPlaybackLocksOnlyUntilOrderedTokensCompleteAndCannotMutateCombatState()
        {
            var session = new CombatSession(CombatStateMachine.Create(Setup()));
            var accepted = session.Submit(new CombatCommand(CombatCommandKind.BeginCombat));
            Assert.That(accepted.IsAccepted, Is.True);
            Assert.That(accepted.Tokens.Select(token => token.EventSequence), Is.EqualTo(accepted.Events.Select(gameEvent => gameEvent.Sequence)));
            var canonical = session.CurrentState.CanonicalForm();
            var blocked = session.Submit(new CombatCommand(CombatCommandKind.EndTurn));
            Assert.That(blocked.IsAccepted, Is.False);
            Assert.That(blocked.Rejection.Code, Is.EqualTo("combat.presentation-locked"));
            Assert.That(session.CurrentState.CanonicalForm(), Is.EqualTo(canonical));
            while (session.TryPeekPresentation(out var token)) Assert.That(session.CompletePresentation(token.EventSequence), Is.True);
            Assert.That(session.IsInputLocked, Is.False);
            Assert.That(session.CurrentState.CanonicalForm(), Is.EqualTo(canonical));
            Assert.That(session.Submit(new CombatCommand(CombatCommandKind.EndTurn)).IsAccepted, Is.True);
        }

        [Test]
        public void StageBindingRoutesFixtureRuntimeIdsToIndependentActorsAndFallbackReactions()
        {
            CombatStageAuthoring.CreateOrUpdate();
            EditorSceneManager.OpenScene("Assets/Scenes/CombatStage.unity", OpenSceneMode.Single);
            var session = new CombatSession(CombatStateMachine.Create(Setup()));
            var controller = Object.FindFirstObjectByType<CombatPresentationController>();
            controller.BindSession(session);
            var opening = session.Submit(new CombatCommand(CombatCommandKind.BeginCombat));
            while (controller.PresentNext()) { }
            var card = session.CurrentState.Deck.Hand.First(candidate => candidate.TargetKind == CardTargetKind.OneEnemy);
            var played = session.Submit(new PlayCardCommand(card.Id, card.OwnerId, CardTargetChoice.OneEnemy(session.CurrentState.Setup.Enemies[0].RuntimeId)));
            Assert.That(played.IsAccepted, Is.True);
            while (controller.PresentNext()) { }
            var partyActor = Object.FindObjectsByType<CombatActorView>(FindObjectsSortMode.None).Single(actor => actor.RuntimeId == card.OwnerId.Value);
            var enemyActor = Object.FindObjectsByType<CombatActorView>(FindObjectsSortMode.None).Single(actor => actor.RuntimeId == session.CurrentState.Setup.Enemies[0].RuntimeId.Value);
            Assert.That(partyActor.GetComponent<CombatActorTokenReaction>().ReactionCount, Is.GreaterThan(0));
            Assert.That(enemyActor.GetComponent<CombatActorTokenReaction>().LastReaction, Is.EqualTo(PresentationReaction.Hit));
            Assert.That(session.IsInputLocked, Is.False);
        }

        [Test]
        public void FixtureCommandsReportStateAndLayoutPreconditions()
        {
            typeof(BloomdrawnEditorCommands).GetField("combatFixtureSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, null);
            Assert.That(() => BloomdrawnEditorCommands.DumpCombatState(), Throws.TypeOf<System.InvalidOperationException>());
            Assert.That(BloomdrawnEditorCommands.LoadCombatFixture().Loaded, Is.True);
            var dump = BloomdrawnEditorCommands.DumpCombatState();
            Assert.That(dump.Phase, Is.EqualTo(CombatPhase.CombatSetup.ToString()));
            CombatStageAuthoring.CreateOrUpdate();
            EditorSceneManager.OpenScene("Assets/Scenes/CombatStage.unity", OpenSceneMode.Single);
            var layout = BloomdrawnEditorCommands.ValidateCombatLayout();
            Assert.That(layout.Valid, Is.True);
            Assert.That(layout.IndependentActorCount, Is.EqualTo(layout.PartyActorCount + layout.EnemyActorCount));
            Assert.That(BloomdrawnEditorCommands.ResetCombatFixture().CanonicalState, Is.EqualTo(dump.CanonicalState));
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
