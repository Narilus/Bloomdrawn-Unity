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
        public static IReadOnlyList<HandFanPose> Calculate(int count, float width, float spacing = 130f, float arcHeight = 32f, float maxAngle = 14f)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count)); if (count == 0) return Array.Empty<HandFanPose>();
            var poses = new List<HandFanPose>(); var center = (count - 1) * .5f;
            for (var index = 0; index < count; index++) { var normalized = center == 0 ? 0 : (index - center) / center; poses.Add(new HandFanPose(new Vector2(width * .5f + (index - center) * spacing, arcHeight * normalized * normalized), -maxAngle * normalized, index)); }
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
