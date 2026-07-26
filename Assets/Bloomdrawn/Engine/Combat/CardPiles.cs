using System;
using System.Collections.Generic;
using System.Linq;
using Bloomdrawn.Content;
using Bloomdrawn.Engine.Rng;

namespace Bloomdrawn.Engine.Combat
{
    public enum CardPile { Draw, Hand, Discard, Graveyard, Resolving }
    public enum CardTargetKind { Party, OneEnemy }
    [Flags] public enum CardTags { None = 0, Retain = 1 }

    public sealed class CardInstance
    {
        public CardInstance(string id, RuntimeParticipantId ownerId, string definitionId, int baseCost, CardTags tags, CardTargetKind targetKind, string operationKind, bool generated = false, bool combatScoped = false, bool spentOnce = false, bool copyProhibited = false, string upgradeId = null)
        { Id = id; OwnerId = ownerId; DefinitionId = definitionId; BaseCost = baseCost; CurrentCost = baseCost; Tags = tags; TargetKind = targetKind; OperationKind = operationKind; Generated = generated; CombatScoped = combatScoped; SpentOnce = spentOnce; CopyProhibited = copyProhibited; UpgradeId = upgradeId; }
        public string Id { get; } public RuntimeParticipantId OwnerId { get; } public string DefinitionId { get; } public int BaseCost { get; } public int CurrentCost { get; } public CardTags Tags { get; } public CardTargetKind TargetKind { get; } public string OperationKind { get; } public bool Generated { get; } public bool CombatScoped { get; } public bool SpentOnce { get; } public bool CopyProhibited { get; } public string UpgradeId { get; }
    }

    public sealed class CombatDeckState
    {
        internal CombatDeckState(IReadOnlyList<CardInstance> draw, IReadOnlyList<CardInstance> hand, IReadOnlyList<CardInstance> discard, IReadOnlyList<CardInstance> graveyard, IReadOnlyList<CardInstance> resolving)
        { Draw = draw; Hand = hand; Discard = discard; Graveyard = graveyard; Resolving = resolving; }
        public IReadOnlyList<CardInstance> Draw { get; } public IReadOnlyList<CardInstance> Hand { get; } public IReadOnlyList<CardInstance> Discard { get; } public IReadOnlyList<CardInstance> Graveyard { get; } public IReadOnlyList<CardInstance> Resolving { get; }
    }

    public static class CombatDecks
    {
        public const int HandTarget = 5, MaximumHand = 10;
        public static CombatDeckState Create(CombatSetupResult setup)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            var draw = setup.DeckRecipe.OrderBy(x => x.Order).Select(x => new CardInstance("combat.card." + setup.LineupId + "." + x.Order, x.OwnerId, x.CardDefinitionId, x.BaseCost, CardTags.None, ParseTargetKind(x.TargetKind), x.OperationKind)).ToList();
            return new CombatDeckState(draw, Array.Empty<CardInstance>(), Array.Empty<CardInstance>(), Array.Empty<CardInstance>(), Array.Empty<CardInstance>());
        }
        public static bool TryDrawToTarget(CombatDeckState state, AuthoritativeRngState rng, out CombatDeckState next)
        {
            if (state == null) throw new ArgumentNullException(nameof(state)); if (rng == null) throw new ArgumentNullException(nameof(rng));
            var draw = state.Draw.ToList(); var hand = state.Hand.ToList(); var discard = state.Discard.ToList();
            var needed = Math.Min(HandTarget, MaximumHand) - hand.Count; if (needed <= 0) { next = state; return true; }
            if (draw.Count < needed && discard.Count > 0) { Shuffle(discard, rng); draw.AddRange(discard); discard.Clear(); }
            var count = Math.Min(needed, draw.Count); hand.AddRange(draw.Take(count)); draw.RemoveRange(0, count);
            next = new CombatDeckState(draw, hand, discard, state.Graveyard, state.Resolving); return true;
        }
        public static bool TryMove(CombatDeckState state, string cardId, CardPile from, CardPile to, out CombatDeckState next)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var source = Pile(state, from); var card = source.FirstOrDefault(x => x.Id == cardId);
            if (card == null || from == to) { next = state; return false; }
            var draw = state.Draw.ToList(); var hand = state.Hand.ToList(); var discard = state.Discard.ToList(); var graveyard = state.Graveyard.ToList(); var resolving = state.Resolving.ToList();
            List<CardInstance> From(CardPile pile) => pile == CardPile.Draw ? draw : pile == CardPile.Hand ? hand : pile == CardPile.Discard ? discard : pile == CardPile.Graveyard ? graveyard : resolving;
            From(from).Remove(card); From(to).Add(card);
            next = new CombatDeckState(draw, hand, discard, graveyard, resolving); return true;
        }
        public static CombatDeckState CompleteResolvingToDiscard(CombatDeckState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new CombatDeckState(state.Draw, state.Hand, state.Discard.Concat(state.Resolving).ToList(), state.Graveyard, Array.Empty<CardInstance>());
        }
        public static CombatDeckState DiscardNonRetainedHand(CombatDeckState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var retained = state.Hand.Where(card => (card.Tags & CardTags.Retain) != 0).ToList();
            var discarded = state.Hand.Where(card => (card.Tags & CardTags.Retain) == 0).ToList();
            return new CombatDeckState(state.Draw, retained, state.Discard.Concat(discarded).ToList(), state.Graveyard, state.Resolving);
        }
        private static IReadOnlyList<CardInstance> Pile(CombatDeckState state, CardPile pile) => pile == CardPile.Draw ? state.Draw : pile == CardPile.Hand ? state.Hand : pile == CardPile.Discard ? state.Discard : pile == CardPile.Graveyard ? state.Graveyard : state.Resolving;
        private static CardTargetKind ParseTargetKind(string targetKind)
        {
            return string.Equals(targetKind, "party", StringComparison.Ordinal) ? CardTargetKind.Party : CardTargetKind.OneEnemy;
        }
        private static void Shuffle(IList<CardInstance> cards, AuthoritativeRngState rng) { for (var i = cards.Count - 1; i > 0; --i) { var j = (int)(rng.NextUInt64(AuthoritativeRngStreams.CombatShuffle) % (ulong)(i + 1)); var t = cards[i]; cards[i] = cards[j]; cards[j] = t; } }
    }
}
