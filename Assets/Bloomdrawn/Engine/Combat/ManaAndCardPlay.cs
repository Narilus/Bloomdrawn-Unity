using System;
using System.Collections.Generic;
using System.Linq;
using Bloomdrawn.Content;
using Bloomdrawn.Engine.Commands;

namespace Bloomdrawn.Engine.Combat
{
    public sealed class ManaState
    {
        public const int BaseMaximum = 6;

        public ManaState(int maximum, int current)
        {
            if (maximum < 0) throw new ArgumentOutOfRangeException(nameof(maximum));
            if (current < 0) throw new ArgumentOutOfRangeException(nameof(current));
            Maximum = maximum;
            Current = current;
        }

        public int Maximum { get; }
        public int Current { get; }

        public static ManaState Full() => new ManaState(BaseMaximum, BaseMaximum);
        public static int CalculateFinalCost(int printedCost, int modifier)
        {
            return Math.Max(0, printedCost + modifier);
        }

        public ManaState Spend(int amount)
        {
            if (amount < 0 || amount > Current) throw new ArgumentOutOfRangeException(nameof(amount));
            return new ManaState(Maximum, Current - amount);
        }
    }

    public sealed class CardTargetChoice
    {
        private CardTargetChoice(RuntimeEnemyId? enemyId) { EnemyId = enemyId; }
        public RuntimeEnemyId? EnemyId { get; }
        public static CardTargetChoice None() => new CardTargetChoice(null);
        public static CardTargetChoice OneEnemy(RuntimeEnemyId enemyId) => new CardTargetChoice(enemyId);
    }

    public sealed class PlayCardCommand
    {
        public PlayCardCommand(string cardInstanceId, RuntimeParticipantId ownerId, CardTargetChoice target)
        {
            CardInstanceId = cardInstanceId ?? throw new ArgumentNullException(nameof(cardInstanceId));
            OwnerId = ownerId;
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public string CardInstanceId { get; }
        public RuntimeParticipantId OwnerId { get; }
        public CardTargetChoice Target { get; }
    }

    public static class CardPlayRules
    {
        public static CommandResult<CombatState> Apply(CombatState state, PlayCardCommand command)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (command == null) throw new ArgumentNullException(nameof(command));

            if (state.IsTerminal) return Reject(state, "card-play.terminal", "Cards cannot be played after combat reaches a terminal phase.");
            if (state.Phase != CombatPhase.PlayerAction) return Reject(state, "card-play.illegal-phase", "Cards may only be played during PlayerAction.");

            var card = state.Deck.Hand.FirstOrDefault(candidate => candidate.Id == command.CardInstanceId);
            if (card == null) return Reject(state, "card-play.not-in-hand", "The selected card is not in the hand.");
            if (!card.OwnerId.Equals(command.OwnerId)) return Reject(state, "card-play.wrong-owner", "The supplied owner does not own the selected card.");
            if (!HasValidTarget(state, card, command.Target, out var targetId, out var targetError)) return Reject(state, targetError.Code, targetError.Message);

            var finalCost = ManaState.CalculateFinalCost(card.CurrentCost, 0);
            if (finalCost > state.Mana.Current) return Reject(state, "card-play.insufficient-mana", "The current Mana is below the card's final cost.");
            if (!CombatDecks.TryMove(state.Deck, card.Id, CardPile.Hand, CardPile.Resolving, out var nextDeck)) return Reject(state, "card-play.card-precondition", "The card failed its required hand-to-resolving transition.");

            var eventFacts = new Dictionary<string, string>
            {
                { "cardInstanceId", card.Id },
                { "cardDefinitionId", card.DefinitionId },
                { "ownerId", card.OwnerId.Value },
                { "operationKind", card.OperationKind },
                { "finalCost", finalCost.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            };
            var gameEvent = new GameEvent(state.NextEventSequence, "combat.card-played", card.OwnerId.Value, targetId, eventFacts);
            var next = CombatStateMachine.WithCardPlayState(state, nextDeck, state.Mana.Spend(finalCost), state.NextEventSequence + 1);
            var resolution = CombatEffectResolver.ResolveCard(next, card, command.Target.EnemyId);
            var completed = CombatStateMachine.WithCardPlayState(resolution.State, CombatDecks.CompleteResolvingToDiscard(resolution.State.Deck), resolution.State.Mana, resolution.State.NextEventSequence);
            return CommandResult<CombatState>.Accepted(completed, new[] { gameEvent }.Concat(resolution.Events).ToList());
        }

        private static bool HasValidTarget(CombatState state, CardInstance card, CardTargetChoice target, out string targetId, out RejectionDiagnostic error)
        {
            targetId = null;
            error = null;
            if (card.TargetKind == CardTargetKind.Party)
            {
                if (target.EnemyId.HasValue)
                {
                    error = new RejectionDiagnostic("card-play.wrong-target", "A party-target card must not supply an enemy target.");
                    return false;
                }
                return true;
            }

            if (!target.EnemyId.HasValue)
            {
                error = new RejectionDiagnostic("card-play.missing-target", "A one-enemy card requires one complete enemy target choice.");
                return false;
            }
            if (!state.Setup.Enemies.Any(enemy => enemy.RuntimeId.Equals(target.EnemyId.Value)))
            {
                error = new RejectionDiagnostic("card-play.wrong-target", "The selected enemy is not a legal encounter target.");
                return false;
            }
            targetId = target.EnemyId.Value.Value;
            return true;
        }

        private static CommandResult<CombatState> Reject(CombatState state, string code, string message)
        {
            return CommandResult<CombatState>.Rejected(state, new RejectionDiagnostic(code, message));
        }
    }
}
