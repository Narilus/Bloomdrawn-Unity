using System;
using System.Collections.Generic;
using System.Linq;
using Bloomdrawn.Engine.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomdrawn.Presentation
{
    public sealed class CombatHudView : MonoBehaviour
    {
        [SerializeField] private RectTransform handContainer;
        [SerializeField] private TextMeshProUGUI survivalText;
        [SerializeField] private TextMeshProUGUI manaText;
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private CombatActorFallbackView[] actorViews;
        [SerializeField] private CombatEnemyTargetView[] enemyTargets;

        private readonly Dictionary<string, CombatCardView> cards = new Dictionary<string, CombatCardView>(StringComparer.Ordinal);
        private CombatStageRuntimeBootstrap bootstrap;

        public int VisibleCardCount => cards.Count;
        public void Configure(RectTransform hand, TextMeshProUGUI survival, TextMeshProUGUI mana, TextMeshProUGUI phase, TextMeshProUGUI log, Button endTurn, CombatActorFallbackView[] actors, CombatEnemyTargetView[] targets)
        { handContainer = hand; survivalText = survival; manaText = mana; phaseText = phase; logText = log; endTurnButton = endTurn; actorViews = actors; enemyTargets = targets; }
        public void Bind(CombatStageRuntimeBootstrap value)
        {
            bootstrap = value ?? throw new ArgumentNullException(nameof(value));
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(bootstrap.EndTurn);
            foreach (var target in enemyTargets) target.Bind(bootstrap);
        }

        public void Refresh(CombatState state, CardInteractionState interactionState, string rejection)
        {
            survivalText.text = $"PARTY  {state.Values.Party.CurrentHp}/{state.Values.Party.MaximumHp}    SHIELD  {state.Values.Party.Shield}";
            manaText.text = $"MANA\n{state.Mana.Current} / {state.Mana.Maximum}";
            phaseText.text = FormatPhase(state.Phase) + "   •   ROUND " + state.RoundNumber;
            logText.text = string.IsNullOrEmpty(rejection)
                ? (interactionState == CardInteractionState.TargetSelection ? "COMBAT LOG\nChoose an enemy target" : "COMBAT LOG\nReady")
                : "COMBAT LOG\nRejected: " + rejection;
            endTurnButton.interactable = state.Phase == CombatPhase.PlayerAction && interactionState != CardInteractionState.TargetSelection;
            foreach (var actor in actorViews) actor.Refresh(state, interactionState == CardInteractionState.TargetSelection, bootstrap.DisplayNameFor);
            RebuildHand(state, interactionState);
        }

        public bool TryGetCard(string cardId, out CombatCardView card) => cards.TryGetValue(cardId, out card);

        public void ClearDetachedCardViews()
        {
            var handIds = new HashSet<string>();
            if (bootstrap != null && bootstrap.CurrentState != null)
                foreach (var instance in bootstrap.CurrentState.Deck.Hand) handIds.Add(instance.Id);
            foreach (var card in FindObjectsByType<CombatCardView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!card.transform.IsChildOf(handContainer) && !handIds.Contains(card.CardId))
                {
                    cards.Remove(card.CardId);
                    Destroy(card.gameObject);
                }
        }

        private void RebuildHand(CombatState state, CardInteractionState interactionState)
        {
            var handIds = new HashSet<string>(state.Deck.Hand.Select(instance => instance.Id));
            foreach (var entry in cards.ToList())
            {
                if (entry.Value != null && handIds.Contains(entry.Key)) continue;
                if (entry.Value != null) Destroy(entry.Value.gameObject);
                cards.Remove(entry.Key);
            }

            var poses = HandFanLayout.Calculate(state.Deck.Hand.Count, handContainer.rect.width <= 1 ? 1040 : handContainer.rect.width, 188f, 22f, 8f);
            var detachedId = interactionState == CardInteractionState.DraggingArmed || interactionState == CardInteractionState.DraggingDisarmed || interactionState == CardInteractionState.TargetSelection
                ? bootstrap.ActiveInteractionCardId
                : null;
            for (var i = 0; i < state.Deck.Hand.Count; i++)
            {
                var instance = state.Deck.Hand[i];
                if (!cards.TryGetValue(instance.Id, out var cardObject) || cardObject == null)
                {
                    cardObject = CombatCardView.Create(
                        handContainer,
                        bootstrap,
                        instance,
                        bootstrap.DisplayNameFor(instance.DefinitionId, "Fixture Card"));
                    cards[instance.Id] = cardObject;
                }

                if (instance.Id == detachedId)
                {
                    if (interactionState == CardInteractionState.TargetSelection) cardObject.SetStaged(true);
                    else cardObject.SetDragging(true);
                    continue;
                }

                cardObject.transform.SetParent(handContainer, false);
                cardObject.SetRestingPose(poses[i]);
                if (interactionState == CardInteractionState.Hovered && bootstrap.ActiveInteractionCardId == instance.Id)
                    cardObject.SetHovered(true);
            }
        }

        private static string FormatPhase(CombatPhase phase)
        {
            switch (phase)
            {
                case CombatPhase.PlayerAction: return "PLAYER ACTION";
                case CombatPhase.EnemyPhaseStart: return "ENEMY PHASE";
                case CombatPhase.EnemyAction: return "ENEMY ACTION";
                case CombatPhase.Victory: return "VICTORY";
                case CombatPhase.Defeat: return "DEFEAT";
                default: return phase.ToString().ToUpperInvariant();
            }
        }
    }
}
