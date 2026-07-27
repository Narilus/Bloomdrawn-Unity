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
            return playArea.rect.Contains(local);
        }
        public void ReparentPreservingScreenPosition(RectTransform card, Vector2 screenPoint, Camera eventCamera)
        {
            card.SetParent(dragLayer, false);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, screenPoint, eventCamera, out var local);
            card.anchoredPosition = local;
        }

        public void ReparentPreservingScreenPosition(RectTransform card, Camera eventCamera)
        {
            ReparentPreservingScreenPosition(card, RectTransformUtility.WorldToScreenPoint(eventCamera, card.position), eventCamera);
        }
    }
}
