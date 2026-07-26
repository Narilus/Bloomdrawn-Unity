using System.IO;
using System.Linq;
using Bloomdrawn.Content;
using Bloomdrawn.Content.Editor;
using Bloomdrawn.Engine.Combat;
using Bloomdrawn.Engine.Rng;
using NUnit.Framework;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class CombatGoldenReplayTests
    {
        [Test]
        public void RegistryFixtureNamedStreamsAndCompleteCommands_ReplayExactTerminalTraceAndChecksum()
        {
            var replay = CombatGoldenReplayRunner.RecordFixture(Setup(), AuthoritativeRngState.Create(71, 913));
            var checksum = CombatGoldenReplayRunner.Replay(replay, out var finalState, out var events);
            Assert.That(finalState.Phase, Is.EqualTo(CombatPhase.Victory));
            Assert.That(checksum, Is.EqualTo(replay.Checksum));
            Assert.That(events.Select(item => item.CanonicalForm()), Is.EqualTo(replay.Events.Select(item => item.CanonicalForm())));
            Assert.That(replay.Rng.Streams.Keys, Does.Contain(AuthoritativeRngStreams.CombatShuffle));
            Assert.That(replay.Commands.Any(command => command.Kind == CombatReplayCommandKind.PlayCard && !string.IsNullOrEmpty(command.CardId) && !string.IsNullOrEmpty(command.OwnerId) && !string.IsNullOrEmpty(command.EnemyId)), Is.True);
            Assert.That(events.Select(item => item.Kind), Does.Contain("combat.enemy-intents-regenerated"));
        }
        [Test]
        public void GoldenReplay_RecordsAtomicStopWithTerminalImmediatelyAfterKillingDamage()
        {
            var replay = CombatGoldenReplayRunner.RecordFixture(Setup(), AuthoritativeRngState.Create(71, 913));
            var terminalIndex = replay.Events.ToList().FindIndex(item => item.Kind == "combat.victory");
            Assert.That(terminalIndex, Is.GreaterThan(0));
            Assert.That(replay.Events[terminalIndex - 1].Kind, Is.EqualTo("combat.damage-dealt"));
            Assert.That(terminalIndex, Is.EqualTo(replay.Events.Count - 1));
        }
        private static CombatSetupResult Setup()
        {
            var root = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..")); var imported = ContentImportService.ImportDirectory(Path.Combine(root, "GameContent", "fixtures"), ContentOrigin.Fixture);
            Assert.That(imported.IsValid, Is.True); return FixtureCombatSetupFactory.Create(FixtureCombatCatalog.Create(imported.Content), new CombatSetupRequest("fixture.m1.lineup.quartet", "fixture.m1.encounter.training"));
        }
    }
}
