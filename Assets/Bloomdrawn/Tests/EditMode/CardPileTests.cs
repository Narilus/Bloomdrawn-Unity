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
    public sealed class CardPileTests
    {
        [Test]
        public void SetupCreatesStableOwnerAwareCardsAndDrawsToFive()
        {
            var first = CombatDecks.Create(Setup()); var second = CombatDecks.Create(Setup());
            Assert.That(first.Draw.Select(x => x.Id), Is.EqualTo(second.Draw.Select(x => x.Id)));
            Assert.That(first.Draw.All(x => x.OwnerId.Value.StartsWith("combat.party.")), Is.True);
            CombatDecks.TryDrawToTarget(first, AuthoritativeRngState.Create(1, 2), out var drawn);
            Assert.That(drawn.Hand, Has.Count.EqualTo(5)); Assert.That(drawn.Draw, Has.Count.EqualTo(3));
        }
        [Test]
        public void ReshuffleUsesOnlyShuffleAndRejectedMoveChangesNothing()
        {
            var rng = AuthoritativeRngState.Create(3, 4); var baseline = rng.Clone(); var state = CombatDecks.Create(Setup());
            for (var i = 0; i < 8; i++) CombatDecks.TryMove(state, state.Draw[0].Id, CardPile.Draw, CardPile.Discard, out state);
            Assert.That(CombatDecks.TryMove(state, "missing", CardPile.Draw, CardPile.Hand, out var rejected), Is.False); Assert.That(rejected, Is.SameAs(state));
            CombatDecks.TryDrawToTarget(state, rng, out var drawn);
            Assert.That(drawn.Hand, Has.Count.EqualTo(5)); Assert.That(rng.NextUInt64(AuthoritativeRngStreams.Reward), Is.EqualTo(baseline.NextUInt64(AuthoritativeRngStreams.Reward)));
        }
        [Test]
        public void ExistingHandCardsCountTowardTargetAndPileMovesPreserveIdentity()
        {
            var state = CombatDecks.Create(Setup());
            var original = state.Draw[0];
            for (var i = 0; i < 8; i++) CombatDecks.TryMove(state, state.Draw[0].Id, CardPile.Draw, CardPile.Hand, out state);
            CombatDecks.TryDrawToTarget(state, AuthoritativeRngState.Create(5, 6), out var unchanged);
            Assert.That(unchanged.Hand, Has.Count.EqualTo(8));
            Assert.That(CombatDecks.TryMove(unchanged, original.Id, CardPile.Hand, CardPile.Resolving, out var resolving), Is.True);
            Assert.That(resolving.Resolving.Single().OwnerId, Is.EqualTo(original.OwnerId));
            Assert.That(resolving.Resolving.Single().DefinitionId, Is.EqualTo(original.DefinitionId));
        }
        private static CombatSetupResult Setup()
        {
            var root = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..")); var v = ContentImportService.ImportDirectory(Path.Combine(root, "GameContent", "fixtures"), ContentOrigin.Fixture);
            Assert.That(v.IsValid, Is.True); return FixtureCombatSetupFactory.Create(FixtureCombatCatalog.Create(v.Content), new CombatSetupRequest("fixture.m1.lineup.quartet", "fixture.m1.encounter.training"));
        }
    }
}
