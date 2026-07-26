using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bloomdrawn.Content;
using Bloomdrawn.Engine.Commands;

namespace Bloomdrawn.Engine.Combat
{
    public enum CombatPhase
    {
        CombatSetup,
        PlayerTurnStart,
        PlayerAction,
        PlayerCleanup,
        PlayerEnd,
        EnemyPhaseStart,
        EnemyAction,
        EnemyEnd,
        RoundEnd,
        Victory,
        Defeat
    }

    public enum CombatCommandKind
    {
        BeginCombat,
        EndTurn
    }

    public sealed class CombatCommand
    {
        public CombatCommand(CombatCommandKind kind) { Kind = kind; }
        public CombatCommandKind Kind { get; }
    }

    public sealed class CombatState
    {
        internal CombatState(CombatSetupResult setup, CombatPhase phase, int roundNumber, long nextEventSequence)
        {
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            Phase = phase;
            RoundNumber = roundNumber;
            NextEventSequence = nextEventSequence;
        }

        public CombatSetupResult Setup { get; }
        public CombatPhase Phase { get; }
        public int RoundNumber { get; }
        public long NextEventSequence { get; }
        public bool IsTerminal => Phase == CombatPhase.Victory || Phase == CombatPhase.Defeat;

        public string CanonicalForm()
        {
            return string.Join("|", new[]
            {
                Setup.LineupId ?? string.Empty,
                Setup.EncounterId ?? string.Empty,
                Phase.ToString(),
                RoundNumber.ToString(CultureInfo.InvariantCulture),
                NextEventSequence.ToString(CultureInfo.InvariantCulture)
            });
        }
    }

    public static class CombatPhaseRules
    {
        public static bool CanTransition(CombatPhase from, CombatPhase to)
        {
            if (to == CombatPhase.Victory || to == CombatPhase.Defeat)
                return from != CombatPhase.Victory && from != CombatPhase.Defeat;

            switch (from)
            {
                case CombatPhase.CombatSetup: return to == CombatPhase.PlayerTurnStart;
                case CombatPhase.PlayerTurnStart: return to == CombatPhase.PlayerAction;
                case CombatPhase.PlayerAction: return to == CombatPhase.PlayerCleanup;
                case CombatPhase.PlayerCleanup: return to == CombatPhase.PlayerEnd;
                case CombatPhase.PlayerEnd: return to == CombatPhase.EnemyPhaseStart;
                case CombatPhase.EnemyPhaseStart: return to == CombatPhase.EnemyAction || to == CombatPhase.EnemyEnd;
                case CombatPhase.EnemyAction: return to == CombatPhase.EnemyAction || to == CombatPhase.EnemyEnd;
                case CombatPhase.EnemyEnd: return to == CombatPhase.RoundEnd;
                case CombatPhase.RoundEnd: return to == CombatPhase.PlayerTurnStart;
                default: return false;
            }
        }
    }

    public static class CombatStateMachine
    {
        public static CombatState Create(CombatSetupResult setup)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            return new CombatState(setup, CombatPhase.CombatSetup, 0, 0);
        }

        public static CommandResult<CombatState> Apply(CombatState state, CombatCommand command)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (command == null) throw new ArgumentNullException(nameof(command));

            if (state.IsTerminal)
            {
                return CommandResult<CombatState>.Rejected(state, new RejectionDiagnostic("combat.terminal", "Normal combat commands are rejected after combat reaches a terminal phase."));
            }

            switch (command.Kind)
            {
                case CombatCommandKind.BeginCombat:
                    return state.Phase == CombatPhase.CombatSetup
                        ? AdvanceOpening(state)
                        : RejectIllegalPhase(state, command.Kind);
                case CombatCommandKind.EndTurn:
                    return state.Phase == CombatPhase.PlayerAction
                        ? AdvanceToEnemyPhaseStart(state)
                        : RejectIllegalPhase(state, command.Kind);
                default:
                    return CommandResult<CombatState>.Rejected(state, new RejectionDiagnostic("combat.unknown-command", "The combat command kind is not recognized."));
            }
        }

        public static CombatState CreateTerminal(CombatSetupResult setup, CombatPhase terminalPhase)
        {
            if (terminalPhase != CombatPhase.Victory && terminalPhase != CombatPhase.Defeat)
                throw new ArgumentOutOfRangeException(nameof(terminalPhase), "Only Victory or Defeat may create a terminal combat state.");
            return new CombatState(setup, terminalPhase, 0, 0);
        }

        private static CommandResult<CombatState> AdvanceOpening(CombatState state)
        {
            var transitions = new List<GameEvent>();
            var playerTurnStart = Advance(state, CombatPhase.PlayerTurnStart, transitions);
            var playerAction = Advance(playerTurnStart, CombatPhase.PlayerAction, transitions);
            return CommandResult<CombatState>.Accepted(playerAction, transitions);
        }

        private static CommandResult<CombatState> AdvanceToEnemyPhaseStart(CombatState state)
        {
            var transitions = new List<GameEvent>();
            var cleanup = Advance(state, CombatPhase.PlayerCleanup, transitions);
            var playerEnd = Advance(cleanup, CombatPhase.PlayerEnd, transitions);
            var enemyPhaseStart = Advance(playerEnd, CombatPhase.EnemyPhaseStart, transitions);
            return CommandResult<CombatState>.Accepted(enemyPhaseStart, transitions);
        }

        private static CombatState Advance(CombatState current, CombatPhase nextPhase, ICollection<GameEvent> events)
        {
            if (!CombatPhaseRules.CanTransition(current.Phase, nextPhase))
                throw new InvalidOperationException("Invalid internal combat phase transition from " + current.Phase + " to " + nextPhase + ".");

            var nextRound = nextPhase == CombatPhase.PlayerTurnStart ? current.RoundNumber + 1 : current.RoundNumber;
            events.Add(new GameEvent(current.NextEventSequence, "combat.phase-entered", facts: new Dictionary<string, string>
            {
                { "from", current.Phase.ToString() },
                { "to", nextPhase.ToString() },
                { "round", nextRound.ToString(CultureInfo.InvariantCulture) }
            }));
            return new CombatState(current.Setup, nextPhase, nextRound, current.NextEventSequence + 1);
        }

        private static CommandResult<CombatState> RejectIllegalPhase(CombatState state, CombatCommandKind commandKind)
        {
            return CommandResult<CombatState>.Rejected(state, new RejectionDiagnostic("combat.illegal-phase", "Command '" + commandKind + "' is not legal during phase '" + state.Phase + "'."));
        }
    }
}
