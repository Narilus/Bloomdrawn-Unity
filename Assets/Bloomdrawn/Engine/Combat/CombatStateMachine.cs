using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bloomdrawn.Content;
using Bloomdrawn.Engine.Commands;
using Bloomdrawn.Engine.Rng;

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
        EndTurn,
        AdvanceEnemyAction
    }

    public sealed class CombatCommand
    {
        public CombatCommand(CombatCommandKind kind) { Kind = kind; }
        public CombatCommandKind Kind { get; }
    }

    public sealed class CombatState
    {
        internal CombatState(CombatSetupResult setup, CombatPhase phase, int roundNumber, long nextEventSequence, CombatDeckState deck = null, ManaState mana = null, CombatValues values = null, IReadOnlyList<EnemySlot> enemySlots = null, int nextEnemySlotIndex = 0, AuthoritativeRngState rng = null)
        {
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            Phase = phase;
            RoundNumber = roundNumber;
            NextEventSequence = nextEventSequence;
            Deck = deck ?? CombatDecks.Create(setup);
            Mana = mana ?? ManaState.Full();
            Values = values ?? CombatValues.Create(setup);
            EnemySlots = enemySlots ?? global::Bloomdrawn.Engine.Combat.EnemySlots.Create(setup);
            NextEnemySlotIndex = nextEnemySlotIndex;
            Rng = rng ?? AuthoritativeRngState.Create(0, 0);
        }

        public CombatSetupResult Setup { get; }
        public CombatPhase Phase { get; }
        public int RoundNumber { get; }
        public long NextEventSequence { get; }
        public CombatDeckState Deck { get; }
        public ManaState Mana { get; }
        public CombatValues Values { get; }
        public IReadOnlyList<EnemySlot> EnemySlots { get; }
        public int NextEnemySlotIndex { get; }
        public AuthoritativeRngState Rng { get; }
        public bool IsTerminal => Phase == CombatPhase.Victory || Phase == CombatPhase.Defeat;

        public string CanonicalForm()
        {
            return string.Join("|", new[]
            {
                Setup.LineupId ?? string.Empty,
                Setup.EncounterId ?? string.Empty,
                Phase.ToString(),
                RoundNumber.ToString(CultureInfo.InvariantCulture),
                NextEventSequence.ToString(CultureInfo.InvariantCulture),
                Mana.Current.ToString(CultureInfo.InvariantCulture),
                string.Join(",", Deck.Draw.Select(card => card.Id)),
                string.Join(",", Deck.Hand.Select(card => card.Id)),
                string.Join(",", Deck.Resolving.Select(card => card.Id)),
                string.Join(",", Deck.Discard.Select(card => card.Id)),
                string.Join(",", Rng.Streams.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + ":" + pair.Value.State.ToString(CultureInfo.InvariantCulture))),
                Values.CanonicalForm(),
                string.Join(",", EnemySlots.Select(slot => slot.SlotIndex.ToString(CultureInfo.InvariantCulture) + ":" + slot.Intent.Kind + ":" + slot.Intent.Damage.ToString(CultureInfo.InvariantCulture))),
                NextEnemySlotIndex.ToString(CultureInfo.InvariantCulture)
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
        public static CombatState Create(CombatSetupResult setup, AuthoritativeRngState rng = null)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            return new CombatState(setup, CombatPhase.CombatSetup, 0, 0, rng: rng);
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
                case CombatCommandKind.AdvanceEnemyAction:
                    return EnemyActionSequence.Advance(state);
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
            CombatDecks.TryDrawToTarget(playerTurnStart.Deck, playerTurnStart.Rng, out var openedDeck);
            var playerAction = Advance(playerTurnStart, CombatPhase.PlayerAction, transitions, openedDeck, ManaState.Full());
            return CommandResult<CombatState>.Accepted(playerAction, transitions);
        }

        private static CommandResult<CombatState> AdvanceToEnemyPhaseStart(CombatState state)
        {
            var transitions = new List<GameEvent>();
            var cleanupDeck = CombatDecks.DiscardNonRetainedHand(state.Deck);
            var cleanup = Advance(state, CombatPhase.PlayerCleanup, transitions, cleanupDeck);
            var playerEnd = Advance(cleanup, CombatPhase.PlayerEnd, transitions);
            var enemyPhaseStart = Advance(playerEnd, CombatPhase.EnemyPhaseStart, transitions);
            return CommandResult<CombatState>.Accepted(enemyPhaseStart, transitions);
        }

        public static CombatState WithCardPlayState(CombatState current, CombatDeckState deck, ManaState mana, long nextEventSequence, CombatValues values = null, CombatPhase? phase = null, IReadOnlyList<EnemySlot> enemySlots = null, int? nextEnemySlotIndex = null, AuthoritativeRngState rng = null)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (deck == null) throw new ArgumentNullException(nameof(deck));
            if (mana == null) throw new ArgumentNullException(nameof(mana));
            return new CombatState(current.Setup, phase ?? current.Phase, current.RoundNumber, nextEventSequence, deck, mana, values ?? current.Values, enemySlots ?? current.EnemySlots, nextEnemySlotIndex ?? current.NextEnemySlotIndex, rng ?? current.Rng);
        }

        public static CommandResult<CombatState> AdvanceFromEnemyEnd(CombatState state, ICollection<GameEvent> priorEvents)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.Phase != CombatPhase.EnemyEnd) throw new InvalidOperationException("Only EnemyEnd may internally advance to the next ordinary player turn.");
            var events = priorEvents == null ? new List<GameEvent>() : priorEvents.ToList();
            var roundEnd = Advance(state, CombatPhase.RoundEnd, events);
            var clearedPartyShield = new PartyCombatValues(roundEnd.Values.Party.MaximumHp, roundEnd.Values.Party.CurrentHp, 0);
            var playerStartValues = new CombatValues(clearedPartyShield, roundEnd.Values.Enemies);
            var shieldCleared = WithCardPlayState(roundEnd, roundEnd.Deck, roundEnd.Mana, roundEnd.NextEventSequence, playerStartValues);
            var playerStart = Advance(shieldCleared, CombatPhase.PlayerTurnStart, events, mana: ManaState.Full());
            CombatDecks.TryDrawToTarget(playerStart.Deck, playerStart.Rng, out var drawnDeck);
            var playerAction = Advance(playerStart, CombatPhase.PlayerAction, events, drawnDeck, ManaState.Full());
            return CommandResult<CombatState>.Accepted(playerAction, events);
        }

        private static CombatState Advance(CombatState current, CombatPhase nextPhase, ICollection<GameEvent> events, CombatDeckState deck = null, ManaState mana = null)
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
            return new CombatState(current.Setup, nextPhase, nextRound, current.NextEventSequence + 1, deck ?? current.Deck, mana ?? current.Mana, current.Values, current.EnemySlots, current.NextEnemySlotIndex, current.Rng);
        }

        private static CommandResult<CombatState> RejectIllegalPhase(CombatState state, CombatCommandKind commandKind)
        {
            return CommandResult<CombatState>.Rejected(state, new RejectionDiagnostic("combat.illegal-phase", "Command '" + commandKind + "' is not legal during phase '" + state.Phase + "'."));
        }
    }
}
