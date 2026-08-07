using UnityEngine;

namespace Bloomdrawn.Presentation
{
    public sealed class CardDragLayer : MonoBehaviour
    {
        [SerializeField] private RectTransform playArea;
        [SerializeField] private RectTransform dragLayer;
        public RectTransform PlayArea => playArea;
        public RectTransform DragLayer => dragLayer;
        public void Configure(RectTransform area, RectTransform layer) { playArea = area; dragLayer = layer; }
        public bool IsAbovePlayArea(Vector2 screenPoint, Camera eventCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(playArea, screenPoint, eventCamera, out var local);
            return local.y >= playArea.rect.yMin;
        }
        public void ReparentPreservingScreenPosition(RectTransform card, Vector2 screenPoint, Camera eventCamera)
        {
            var worldPosition = card.position;
            var worldRotation = card.rotation;
            card.SetParent(dragLayer, true);
            card.position = worldPosition;
            card.rotation = worldRotation;
            card.SetAsLastSibling();
        }

        public void ReparentPreservingScreenPosition(RectTransform card, Camera eventCamera)
        {
            ReparentPreservingScreenPosition(card, RectTransformUtility.WorldToScreenPoint(eventCamera, card.position), eventCamera);
        }

        public void MoveToScreenPoint(RectTransform card, Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, screenPoint, eventCamera, out var local)) return;
            card.position = dragLayer.TransformPoint(local);
        }

    }
}
