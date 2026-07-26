using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bloomdrawn.Engine.Rng
{
    public static class AuthoritativeRngStreams
    {
        public const string CombatShuffle = "combat.shuffle";
        public const string CombatTargeting = "combat.targeting";
        public const string EnemyIntent = "enemy.intent";
        public const string MapLayout = "map.layout";
        public const string MapContent = "map.content";
        public const string MapNodeModifiers = "map.nodeModifiers";
        public const string Reward = "reward";
        public const string Shop = "shop";
        public const string Gacha = "gacha";

        public static readonly IReadOnlyList<string> All = new[]
        {
            CombatShuffle, CombatTargeting, EnemyIntent, MapLayout, MapContent,
            MapNodeModifiers, Reward, Shop, Gacha
        };
    }

    public sealed class AuthoritativeRngState
    {
        public AuthoritativeRngState()
        {
            Streams = new Dictionary<string, RngState>(StringComparer.Ordinal);
        }

        public Dictionary<string, RngState> Streams { get; set; }

        public static AuthoritativeRngState Create(ulong profileSeed, ulong runSeed)
        {
            var result = new AuthoritativeRngState();
            foreach (var stream in AuthoritativeRngStreams.All)
            {
                result.Streams.Add(stream, new RngState(RngSeedDerivation.Derive(profileSeed, runSeed, stream)));
            }

            return result;
        }

        public ulong NextUInt64(string stream)
        {
            RngState state;
            if (!Streams.TryGetValue(stream, out state))
            {
                throw new ArgumentException("Unknown authoritative RNG stream.", nameof(stream));
            }

            return DeterministicRng.NextUInt64(state);
        }

        public AuthoritativeRngState Clone()
        {
            var clone = new AuthoritativeRngState();
            foreach (var pair in Streams)
            {
                clone.Streams.Add(pair.Key, pair.Value.Clone());
            }

            return clone;
        }
    }

    public static class RngSeedDerivation
    {
        public static ulong Derive(ulong profileSeed, ulong runSeed, string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                throw new ArgumentException("A stable ID is required.", nameof(stableId));
            }

            var hash = 14695981039346656037UL;
            foreach (var value in BitConverter.GetBytes(profileSeed).Concat(BitConverter.GetBytes(runSeed)).Concat(Encoding.UTF8.GetBytes(stableId)))
            {
                hash ^= value;
                hash *= 1099511628211UL;
            }

            var state = new RngState(hash);
            return DeterministicRng.NextUInt64(state);
        }
    }

    /// <summary>Minimal M0C fixture seam proving rejected operations leave authoritative RNG untouched.</summary>
    public static class RngCommandFixture
    {
        public static bool TryAdvance(bool accepted, AuthoritativeRngState state, string stream, out ulong value)
        {
            if (!accepted)
            {
                value = 0;
                return false;
            }

            value = state.NextUInt64(stream);
            return true;
        }
    }
}
