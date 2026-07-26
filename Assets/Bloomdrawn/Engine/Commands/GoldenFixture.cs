using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Bloomdrawn.Engine.Commands
{
    public sealed class GoldenFixture
    {
        public GoldenFixture(SmokeCommandState initialState, IReadOnlyList<bool> commands, IReadOnlyList<GameEvent> expectedEvents, string checksum)
        {
            InitialState = initialState;
            Commands = commands;
            ExpectedEvents = expectedEvents;
            Checksum = checksum;
        }

        public SmokeCommandState InitialState { get; }
        public IReadOnlyList<bool> Commands { get; }
        public IReadOnlyList<GameEvent> ExpectedEvents { get; }
        public string Checksum { get; }
    }

    public static class GoldenFixtureRunner
    {
        public static string Run(SmokeCommandState initialState, IEnumerable<bool> commands, out SmokeCommandState finalState, out IReadOnlyList<GameEvent> events)
        {
            var currentState = initialState;
            var emitted = new List<GameEvent>();
            foreach (var command in commands)
            {
                var result = SmokeCommandFixture.Execute(currentState, command);
                currentState = result.State;
                emitted.AddRange(result.Events);
            }

            finalState = currentState;
            events = emitted;
            var canonical = currentState.CanonicalForm() + "\n" + string.Join("\n", emitted.OrderBy(item => item.Sequence).Select(item => item.CanonicalForm()));
            using (var hash = SHA256.Create())
            {
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
