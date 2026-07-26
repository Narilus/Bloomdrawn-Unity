using System;

namespace Bloomdrawn.Engine.Rng
{
    /// <summary>Serializable state for a SplitMix64 stream. No serializer dependency is required by the engine.</summary>
    public sealed class RngState
    {
        public RngState()
        {
        }

        public RngState(ulong state)
        {
            State = state;
        }

        public ulong State { get; set; }

        public RngState Clone()
        {
            return new RngState(State);
        }
    }

    public static class DeterministicRng
    {
        public static ulong NextUInt64(RngState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.State += 0x9E3779B97F4A7C15UL;
            var value = state.State;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }

        public static int NextInt(RngState state, int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
            }

            return (int)(NextUInt64(state) % (ulong)exclusiveMaximum);
        }
    }
}
