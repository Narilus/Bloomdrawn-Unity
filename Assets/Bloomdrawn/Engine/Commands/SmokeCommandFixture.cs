using System;
using System.Collections.Generic;

namespace Bloomdrawn.Engine.Commands
{
    public sealed class SmokeCommandState
    {
        public SmokeCommandState(int revision, long nextEventSequence, ulong seed)
        {
            Revision = revision;
            NextEventSequence = nextEventSequence;
            Seed = seed;
        }

        public int Revision { get; }
        public long NextEventSequence { get; }
        public ulong Seed { get; }

        public string CanonicalForm()
        {
            return Revision.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + NextEventSequence.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + Seed.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    public static class SmokeCommandFixture
    {
        public static CommandResult<SmokeCommandState> Execute(SmokeCommandState state, bool accepted)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!accepted)
            {
                return CommandResult<SmokeCommandState>.Rejected(state, new RejectionDiagnostic("fixture.rejected", "Fixture command was rejected."));
            }

            var nextState = new SmokeCommandState(state.Revision + 1, state.NextEventSequence + 1, state.Seed);
            var gameEvent = new GameEvent(state.NextEventSequence, "fixture.state-advanced", facts: new Dictionary<string, string>
            {
                { "revision", nextState.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            });
            return CommandResult<SmokeCommandState>.Accepted(nextState, new[] { gameEvent });
        }
    }
}
