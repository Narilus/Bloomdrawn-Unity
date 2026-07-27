using System;
using System.Collections.Generic;
using Bloomdrawn.Content;
using Bloomdrawn.Engine.Combat;
using Bloomdrawn.Engine.Rng;
using Newtonsoft.Json;

namespace Bloomdrawn.Application
{
    [Serializable]
    public sealed class M1FixtureRuntimeArtifact
    {
        public string Origin { get; set; }
        public string ContentHash { get; set; }
        public List<ContentDefinition> Definitions { get; set; }
        public M1FixtureLaunchManifest Launch { get; set; }
    }

    public static class M1FixtureRuntimeLoader
    {
        public static CombatSession CreateSession(string artifactJson)
        {
            if (string.IsNullOrWhiteSpace(artifactJson)) throw new ArgumentException("A fixture runtime artifact is required.", nameof(artifactJson));
            var artifact = JsonConvert.DeserializeObject<M1FixtureRuntimeArtifact>(artifactJson);
            if (artifact == null || !string.Equals(artifact.Origin, ContentOrigin.Fixture.ToString(), StringComparison.Ordinal)) throw new InvalidOperationException("Runtime combat accepts fixture-origin artifacts only.");
            if (artifact.Launch == null || artifact.Launch.SchemaVersion != 1 || string.IsNullOrWhiteSpace(artifact.Launch.LineupId) || string.IsNullOrWhiteSpace(artifact.Launch.EncounterId)) throw new InvalidOperationException("Fixture runtime launch manifest is invalid.");
            var validation = ContentValidator.Validate(artifact.Definitions, ContentOrigin.Fixture);
            if (!validation.IsValid) throw new InvalidOperationException("Fixture runtime artifact content is invalid.");
            var registry = ContentRegistry.Create(validation.Content);
            if (!string.Equals(registry.ContentHash, artifact.ContentHash, StringComparison.Ordinal)) throw new InvalidOperationException("Fixture runtime artifact content hash does not match its validated definitions.");
            var setup = FixtureCombatSetupFactory.Create(FixtureCombatCatalog.Create(validation.Content), new CombatSetupRequest(artifact.Launch.LineupId, artifact.Launch.EncounterId));
            return new CombatSession(CombatStateMachine.Create(setup, AuthoritativeRngState.Create(artifact.Launch.ProfileSeed, artifact.Launch.RunSeed)));
        }
    }

    public sealed class CombatRuntimeFlow
    {
        private readonly CombatSession session;
        public event Action<CombatSessionSubmission> StateChanged;
        public CombatRuntimeFlow(CombatSession value) { session = value ?? throw new ArgumentNullException(nameof(value)); }
        public CombatSession Session => session;
        public CombatState CurrentState => session.CurrentState;
        public bool IsTerminal => CurrentState.IsTerminal;
        public CombatSessionSubmission Begin() => Notify(session.Submit(new CombatCommand(CombatCommandKind.BeginCombat)));
        public CombatSessionSubmission EndTurn() => Notify(session.Submit(new CombatCommand(CombatCommandKind.EndTurn)));
        public CombatSessionSubmission Play(string cardId, string ownerId, string enemyId) => Notify(session.Submit(new PlayCardCommand(cardId, new RuntimeParticipantId(ownerId), string.IsNullOrEmpty(enemyId) ? CardTargetChoice.None() : CardTargetChoice.OneEnemy(new RuntimeEnemyId(enemyId)))));
        public CombatSessionSubmission AdvanceEnemyIfReady()
        {
            if (session.IsInputLocked || CurrentState.IsTerminal || (CurrentState.Phase != CombatPhase.EnemyPhaseStart && CurrentState.Phase != CombatPhase.EnemyAction)) return null;
            return Notify(session.Submit(new CombatCommand(CombatCommandKind.AdvanceEnemyAction)));
        }

        private CombatSessionSubmission Notify(CombatSessionSubmission submission)
        {
            StateChanged?.Invoke(submission);
            return submission;
        }
    }
}
