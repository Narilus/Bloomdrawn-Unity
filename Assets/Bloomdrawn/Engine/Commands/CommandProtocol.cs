using System;
using System.Collections.Generic;
using System.Linq;

namespace Bloomdrawn.Engine.Commands
{
    public sealed class RejectionDiagnostic
    {
        public RejectionDiagnostic(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; }
        public string Message { get; }
    }

    public sealed class GameEvent
    {
        public GameEvent(long sequence, string kind, string sourceId = null, string targetId = null, IReadOnlyDictionary<string, string> facts = null)
        {
            Sequence = sequence;
            Kind = kind;
            SourceId = sourceId;
            TargetId = targetId;
            Facts = facts ?? new Dictionary<string, string>();
        }

        public long Sequence { get; }
        public string Kind { get; }
        public string SourceId { get; }
        public string TargetId { get; }
        public IReadOnlyDictionary<string, string> Facts { get; }

        public string CanonicalForm()
        {
            return string.Join("|", new[]
            {
                Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture), Kind ?? string.Empty,
                SourceId ?? string.Empty, TargetId ?? string.Empty,
                string.Join(",", Facts.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + "=" + pair.Value))
            });
        }
    }

    public sealed class CommandResult<TState>
    {
        private CommandResult(bool isAccepted, TState state, IReadOnlyList<GameEvent> events, RejectionDiagnostic rejection)
        {
            IsAccepted = isAccepted;
            State = state;
            Events = events;
            Rejection = rejection;
        }

        public bool IsAccepted { get; }
        public TState State { get; }
        public IReadOnlyList<GameEvent> Events { get; }
        public RejectionDiagnostic Rejection { get; }

        public static CommandResult<TState> Accepted(TState state, IReadOnlyList<GameEvent> events)
        {
            return new CommandResult<TState>(true, state, events ?? Array.Empty<GameEvent>(), null);
        }

        public static CommandResult<TState> Rejected(TState unchangedState, RejectionDiagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            return new CommandResult<TState>(false, unchangedState, Array.Empty<GameEvent>(), diagnostic);
        }
    }
}
