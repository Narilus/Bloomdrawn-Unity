using Bloomdrawn.Application;
using Bloomdrawn.Editor.Tooling;
using Bloomdrawn.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class M1RuntimeIntegrationEditModeTests
    {
        [Test]
        public void CommittedCombatStage_HasDurableIdentityAndRequiredRuntimeBindings()
        {
            var summary = CombatStageSceneValidator.ValidateCommittedScene();
            Assert.That(summary.Valid, Is.True);
            Assert.That(summary.MissingBehaviourCount, Is.Zero);
            var drag = Object.FindFirstObjectByType<CardDragLayer>();
            var script = MonoScript.FromMonoBehaviour(drag);
            Assert.That(AssetDatabase.GetAssetPath(script), Is.EqualTo("Assets/Bloomdrawn/Presentation/CardDragLayer.cs"));
            Assert.That(drag.PlayArea, Is.Not.Null);
            Assert.That(drag.DragLayer, Is.Not.Null);
        }

        [Test]
        public void GeneratedFixtureArtifact_RevalidatesIntoRegistryDerivedSession()
        {
            var artifact = AssetDatabase.LoadAssetAtPath<TextAsset>(M1RuntimeFixtureArtifactGenerator.ArtifactPath);
            Assert.That(artifact, Is.Not.Null);
            var session = M1FixtureRuntimeLoader.CreateSession(artifact.text);
            Assert.That(session.CurrentState.Setup.LineupId, Is.EqualTo("fixture.m1.lineup.quartet"));
            Assert.That(session.CurrentState.Setup.EncounterId, Is.EqualTo("fixture.m1.encounter.training"));
            Assert.That(session.CurrentState.Rng.Streams, Is.Not.Empty);
        }
    }
}
