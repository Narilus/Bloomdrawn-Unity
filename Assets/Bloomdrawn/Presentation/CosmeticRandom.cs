using System;

namespace Bloomdrawn.Presentation
{
    /// <summary>Unsaved presentation-only randomness; it has no reference to authoritative engine state.</summary>
    public sealed class CosmeticRandom
    {
        private readonly Random random = new Random();

        public int Next(int exclusiveMaximum)
        {
            return random.Next(exclusiveMaximum);
        }
    }
}
