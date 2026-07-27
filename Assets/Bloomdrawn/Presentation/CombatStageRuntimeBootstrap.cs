using System;
using Bloomdrawn.Application;
using Bloomdrawn.Engine.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Bloomdrawn.Presentation
{
    /// <summary>Owns only the Player-facing bridge from the generated fixture artifact to existing M1 session contracts.</summary>
    public sealed class CombatStageRuntimeBootstrap : MonoBehaviour, ICompleteCardCommandSink
    {
        [SerializeField] private TextAsset fixtureRuntimeArtifact;
        [SerializeField] private CombatPresentationController presentationController;
        [SerializeField] private CombatHudView hud;
        [SerializeField] private CardDragLayer dragLayer;

        private CombatRuntimeFlow flow;
        private CardInteractionController interaction;
        private string rejection;

        public bool IsBootstrapped => flow != null;
        public CombatRuntimeFlow Flow => flow;
        public CombatState CurrentState => flow?.CurrentState;
        public string LastRejection => rejection;
        public CardInteractionState InteractionState => interaction == null ? CardInteractionState.Resting : interaction.State;
        public string ActiveInteractionCardId => interaction == null ? null : interaction.ActiveCardId;

        public void Configure(TextAsset artifact, CombatPresentationController controller, CombatHudView value, CardDragLayer layer)
        {
            fixtureRuntimeArtifact = artifact;
            presentationController = controller;
            hud = value;
            dragLayer = layer;
        }

        private void Awake()
        {
            if (fixtureRuntimeArtifact == null) throw new InvalidOperationException("CombatStage requires the generated M1 fixture runtime artifact.");
            var inputModule = FindFirstObjectByType<InputSystemUIInputModule>();
            if (inputModule != null && inputModule.actionsAsset == null) inputModule.AssignDefaultActions();
            flow = new CombatRuntimeFlow(M1FixtureRuntimeLoader.CreateSession(fixtureRuntimeArtifact.text));
            interaction = new CardInteractionController(this);
            flow.StateChanged += OnStateChanged;
            presentationController.BindSession(flow.Session);
            hud.Bind(this);
            flow.Begin();
            Refresh();
        }

        private void OnDestroy()
        {
            if (flow != null) flow.StateChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (presentationController.PresentNext())
            {
                Refresh();
                return;
            }

            if (!flow.Session.IsInputLocked)
            {
                var advanced = flow.AdvanceEnemyIfReady();
                if (advanced != null) Refresh();
            }

            HandleKeyboard();
        }

        public bool Submit(CardCommandSubmission submission)
        {
            var result = flow.Play(submission.CardId, submission.OwnerId, submission.EnemyId);
            rejection = result.IsAccepted ? null : result.Rejection?.Code;
            hud.ClearDetachedCardViews();
            Refresh();
            return result.IsAccepted;
        }

        public void BeginCardDrag(CombatCardView card, PointerEventData eventData)
        {
            if (!CanAcceptPlayerInput() || card == null) return;
            interaction.BeginDrag(card.CardId, card.OwnerId, card.RequiresEnemyTarget);
            dragLayer.ReparentPreservingScreenPosition(card.RectTransform, eventData.position, eventData.pressEventCamera);
            card.SetArmed(false);
        }

        public void UpdateCardDrag(CombatCardView card, PointerEventData eventData)
        {
            if (interaction.State != CardInteractionState.DraggingArmed && interaction.State != CardInteractionState.DraggingDisarmed) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer.DragLayer, eventData.position, eventData.pressEventCamera, out var localPoint);
            card.RectTransform.anchoredPosition = localPoint;
            var armed = dragLayer.IsAbovePlayArea(eventData.position, eventData.pressEventCamera);
            interaction.UpdateArmed(armed);
            card.SetArmed(armed);
        }

        public void ReleaseCardDrag(CombatCardView card)
        {
            if (card == null) return;
            interaction.Release();
            Refresh();
        }

        public void ClickCard(CombatCardView card)
        {
            if (!CanAcceptPlayerInput() || card == null) return;
            interaction.BeginDrag(card.CardId, card.OwnerId, card.RequiresEnemyTarget);
            interaction.UpdateArmed(true);
            if (card.RequiresEnemyTarget) card.RectTransform.SetParent(dragLayer.DragLayer, true);
            interaction.Release();
            Refresh();
        }

        public void SelectEnemy(string runtimeId)
        {
            if (interaction.State != CardInteractionState.TargetSelection) return;
            interaction.SelectEnemy(runtimeId);
            Refresh();
        }

        public void CancelInteraction()
        {
            interaction.Cancel();
            rejection = null;
            hud.ClearDetachedCardViews();
            Refresh();
        }

        public void EndTurn()
        {
            if (!CanAcceptPlayerInput()) return;
            var result = flow.EndTurn();
            rejection = result.IsAccepted ? null : result.Rejection?.Code;
            Refresh();
        }

        private void HandleKeyboard()
        {
            if (!HasKeyboard()) return;
            if (WasPressed(keyboard => keyboard.escapeKey)) { CancelInteraction(); return; }
            if (WasPressed(keyboard => keyboard.enterKey) || WasPressed(keyboard => keyboard.spaceKey))
            {
                if (interaction.State == CardInteractionState.TargetSelection && CurrentState.Setup.Enemies.Count > 0) SelectEnemy(CurrentState.Setup.Enemies[0].RuntimeId.Value);
                else EndTurn();
                return;
            }
            if (!CanAcceptPlayerInput()) return;
            var hand = CurrentState.Deck.Hand;
            for (var i = 0; i < hand.Count && i < 5; i++)
            {
                if (!WasPressed(keyboard => i == 0 ? keyboard.digit1Key : i == 1 ? keyboard.digit2Key : i == 2 ? keyboard.digit3Key : i == 3 ? keyboard.digit4Key : keyboard.digit5Key)) continue;
                hud.TryGetCard(hand[i].Id, out var card);
                ClickCard(card);
                return;
            }
        }

        private static bool WasPressed(Func<Keyboard, KeyControl> key)
        {
            foreach (var device in InputSystem.devices) if (device is Keyboard keyboard && key(keyboard).wasPressedThisFrame) return true;
            return false;
        }

        private static bool HasKeyboard()
        {
            foreach (var device in InputSystem.devices) if (device is Keyboard) return true;
            return false;
        }

        private bool CanAcceptPlayerInput() => flow != null && !flow.IsTerminal && !flow.Session.IsInputLocked && CurrentState.Phase == CombatPhase.PlayerAction;

        private void OnStateChanged(CombatSessionSubmission submission)
        {
            if (!submission.IsAccepted) rejection = submission.Rejection?.Code;
            Refresh();
        }

        private void Refresh()
        {
            if (flow != null && hud != null) hud.Refresh(flow.CurrentState, interaction == null ? CardInteractionState.Resting : interaction.State, rejection);
        }
    }
}
