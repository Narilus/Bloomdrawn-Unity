using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bloomdrawn.Presentation
{
    public readonly struct HandFanPose
    {
        public HandFanPose(Vector2 position, float rotation, int depth) { Position = position; Rotation = rotation; Depth = depth; }
        public Vector2 Position { get; } public float Rotation { get; } public int Depth { get; }
    }
    public static class HandFanLayout
    {
        public const float DefaultCardWidth = 180f;

        public static IReadOnlyList<HandFanPose> Calculate(int count, float width, float spacing = 130f, float arcHeight = 32f, float maxAngle = 14f)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return Array.Empty<HandFanPose>();

            // Keep the fan centred while contracting the overlap as the hand grows.  The
            // hand container is presentation input, so this calculation deliberately does
            // not depend on any temporary card transform or pointer position.
            var safeWidth = Mathf.Max(0f, width);
            var centre = (count - 1) * .5f;
            var availableSpacing = count == 1
                ? 0f
                : Mathf.Max(0f, (safeWidth - DefaultCardWidth) / (count - 1));
            var effectiveSpacing = count == 1
                ? 0f
                : Mathf.Min(Mathf.Max(0f, spacing), availableSpacing);
            var poses = new List<HandFanPose>(count);
            for (var index = 0; index < count; index++)
            {
                var normalized = centre == 0 ? 0 : (index - centre) / centre;
                poses.Add(new HandFanPose(
                    new Vector2(safeWidth * .5f + (index - centre) * effectiveSpacing, arcHeight * normalized * normalized),
                    -maxAngle * normalized,
                    index));
            }

            return poses;
        }
    }
    public enum CardInteractionState { Resting, Hovered, DraggingDisarmed, DraggingArmed, TargetSelection }
    public sealed class CardCommandSubmission { public CardCommandSubmission(string cardId, string ownerId, string enemyId) { CardId=cardId; OwnerId=ownerId; EnemyId=enemyId; } public string CardId { get; } public string OwnerId { get; } public string EnemyId { get; } }
    public interface ICompleteCardCommandSink { bool Submit(CardCommandSubmission submission); }
    public sealed class CardInteractionController
    {
        private readonly ICompleteCardCommandSink sink;
        public CardInteractionController(ICompleteCardCommandSink sink) { this.sink=sink ?? throw new ArgumentNullException(nameof(sink)); }
        public CardInteractionState State { get; private set; } = CardInteractionState.Resting;
        public string ActiveCardId { get; private set; } public string OwnerId { get; private set; } public bool RequiresEnemyTarget { get; private set; }
        public void Hover(string cardId)
        {
            if (State == CardInteractionState.Resting || State == CardInteractionState.Hovered)
            {
                ActiveCardId = cardId;
                State = CardInteractionState.Hovered;
            }
        }
        public void ExitHover(string cardId)
        {
            if (State == CardInteractionState.Hovered && ActiveCardId == cardId) Cancel();
        }
        public void BeginDrag(string cardId, string ownerId, bool requiresEnemyTarget) { if (State == CardInteractionState.DraggingArmed || State == CardInteractionState.DraggingDisarmed || State == CardInteractionState.TargetSelection) throw new InvalidOperationException("Only one card interaction session is allowed."); ActiveCardId=cardId; OwnerId=ownerId; RequiresEnemyTarget=requiresEnemyTarget; State=CardInteractionState.DraggingDisarmed; }
        public void UpdateArmed(bool abovePlayArea) { if (State == CardInteractionState.DraggingArmed || State == CardInteractionState.DraggingDisarmed) State=abovePlayArea?CardInteractionState.DraggingArmed:CardInteractionState.DraggingDisarmed; }
        public bool Release() { if (State != CardInteractionState.DraggingArmed) { Cancel(); return false; } if (RequiresEnemyTarget) { State=CardInteractionState.TargetSelection; return false; } return Submit(null); }
        public bool SelectEnemy(string enemyId) { return State == CardInteractionState.TargetSelection && !string.IsNullOrEmpty(enemyId) && Submit(enemyId); }
        public void Cancel() { State=CardInteractionState.Resting; ActiveCardId=null; OwnerId=null; RequiresEnemyTarget=false; }
        private bool Submit(string enemyId) { var accepted=sink.Submit(new CardCommandSubmission(ActiveCardId,OwnerId,enemyId)); Cancel(); return accepted; }
    }

}
