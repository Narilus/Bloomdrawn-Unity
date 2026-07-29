using System;
using System.Collections.Generic;
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
            RebuildHand(state);
        }

        public bool TryGetCard(string cardId, out CombatCardView card) => cards.TryGetValue(cardId, out card);

        public void ClearDetachedCardViews()
        {
            foreach (var card in FindObjectsByType<CombatCardView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!card.transform.IsChildOf(handContainer)) Destroy(card.gameObject);
        }

        private void RebuildHand(CombatState state)
        {
            foreach (Transform child in handContainer) Destroy(child.gameObject);
            cards.Clear();
            var poses = HandFanLayout.Calculate(state.Deck.Hand.Count, handContainer.rect.width <= 1 ? 1040 : handContainer.rect.width, 188f, 22f, 8f);
            for (var i = 0; i < state.Deck.Hand.Count; i++)
            {
                var instance = state.Deck.Hand[i];
                var cardObject = CombatCardView.Create(
                    handContainer,
                    bootstrap,
                    instance,
                    bootstrap.DisplayNameFor(instance.DefinitionId, "Fixture Card"));
                cardObject.RectTransform.anchoredPosition = poses[i].Position;
                cardObject.RectTransform.localRotation = Quaternion.Euler(0, 0, poses[i].Rotation);
                cardObject.transform.SetSiblingIndex(poses[i].Depth);
                cards.Add(instance.Id, cardObject);
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
