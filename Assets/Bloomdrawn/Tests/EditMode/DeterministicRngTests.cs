using System.Linq;
using Bloomdrawn.Engine.Rng;
using Bloomdrawn.Presentation;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class DeterministicRngTests
    {
        [Test]
        public void SameSeeds_ProduceSameNamedStreamSequence()
        {
            var first = AuthoritativeRngState.Create(12UL, 34UL);
            var second = AuthoritativeRngState.Create(12UL, 34UL);

            var firstValues = Enumerable.Range(0, 8).Select(_ => first.NextUInt64(AuthoritativeRngStreams.CombatShuffle)).ToArray();
            var secondValues = Enumerable.Range(0, 8).Select(_ => second.NextUInt64(AuthoritativeRngStreams.CombatShuffle)).ToArray();

            Assert.That(secondValues, Is.EqualTo(firstValues));
        }

        [Test]
        public void StreamAdvancementIsIsolated_AndCosmeticRandomDoesNotMutateAuthoritativeState()
        {
            var baseline = AuthoritativeRngState.Create(5UL, 6UL);
            var advanced = baseline.Clone();
            var cosmeticRandom = new CosmeticRandom();

            advanced.NextUInt64(AuthoritativeRngStreams.Reward);
            cosmeticRandom.Next(100);

            Assert.That(advanced.NextUInt64(AuthoritativeRngStreams.Shop), Is.EqualTo(baseline.NextUInt64(AuthoritativeRngStreams.Shop)));
        }

        [Test]
        public void RejectedFixture_DoesNotConsumeRng()
        {
            var baseline = AuthoritativeRngState.Create(7UL, 8UL);
            var rejected = baseline.Clone();
            ulong ignored;

            Assert.That(RngCommandFixture.TryAdvance(false, rejected, AuthoritativeRngStreams.EnemyIntent, out ignored), Is.False);
            Assert.That(rejected.NextUInt64(AuthoritativeRngStreams.EnemyIntent), Is.EqualTo(baseline.NextUInt64(AuthoritativeRngStreams.EnemyIntent)));
        }

        [Test]
        public void NewtonsoftRoundtrip_ContinuesWithIdenticalState()
        {
            var original = AuthoritativeRngState.Create(90UL, 12UL);
            original.NextUInt64(AuthoritativeRngStreams.MapLayout);
            var restored = JsonConvert.DeserializeObject<AuthoritativeRngState>(JsonConvert.SerializeObject(original));

            Assert.That(restored.NextUInt64(AuthoritativeRngStreams.MapLayout), Is.EqualTo(original.NextUInt64(AuthoritativeRngStreams.MapLayout)));
            Assert.That(restored.NextUInt64(AuthoritativeRngStreams.Gacha), Is.EqualTo(original.NextUInt64(AuthoritativeRngStreams.Gacha)));
        }
    }
}
