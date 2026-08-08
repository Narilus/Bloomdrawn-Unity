using UnityEngine;

namespace Bloomdrawn.Presentation
{
    public sealed class CardDragLayer : MonoBehaviour
    {
        [SerializeField] private RectTransform playArea;
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private float armHysteresis = 8f;
        [SerializeField] private bool keepDraggedCardsOnScreen = true;
        public RectTransform PlayArea => playArea;
        public RectTransform DragLayer => dragLayer;
        public void Configure(RectTransform area, RectTransform layer) { playArea = area; dragLayer = layer; }

        public bool IsAbovePlayArea(Vector2 screenPoint, Camera eventCamera)
        {
            if (playArea == null) return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(playArea, screenPoint, eventCamera, out var local);
            return local.y >= playArea.rect.yMin;
        }

        public bool IsAbovePlayArea(Vector2 screenPoint, Camera eventCamera, bool currentlyArmed)
        {
            if (playArea == null) return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(playArea, screenPoint, eventCamera, out var local);
            var threshold = playArea.rect.yMin + (currentlyArmed ? -HysteresisInLocalUnits() : HysteresisInLocalUnits());
            return local.y >= threshold;
        }

        public void ReparentPreservingScreenPosition(RectTransform card, Vector2 screenPoint, Camera eventCamera)
        {
            if (card == null || dragLayer == null) return;
            var worldPosition = card.position;
            var worldRotation = card.rotation;
            var worldScale = card.lossyScale;
            card.SetParent(dragLayer, true);
            card.position = worldPosition;
            card.rotation = worldRotation;
            var currentWorldScale = card.lossyScale;
            card.localScale = Vector3.Scale(card.localScale, new Vector3(
                SafeRatio(worldScale.x, currentWorldScale.x),
                SafeRatio(worldScale.y, currentWorldScale.y),
                SafeRatio(worldScale.z, currentWorldScale.z)));
            card.SetAsLastSibling();
        }

        public void ReparentPreservingScreenPosition(RectTransform card, Camera eventCamera)
        {
            ReparentPreservingScreenPosition(card, RectTransformUtility.WorldToScreenPoint(eventCamera, card.position), eventCamera);
        }

        public void MoveToScreenPoint(RectTransform card, Vector2 screenPoint, Camera eventCamera)
        {
            if (card == null || dragLayer == null) return;
            screenPoint = ClampToScreenBounds(card, screenPoint, eventCamera);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, screenPoint, eventCamera, out var local)) return;
            var worldPosition = dragLayer.TransformPoint(local);
            var view = card.GetComponent<CombatCardView>();
            if (view != null) view.SetTransientWorldPosition(worldPosition);
            else card.position = worldPosition;
        }

        public void StageCard(CombatCardView card, Camera eventCamera)
        {
            if (card == null || playArea == null) return;
            var worldCentre = playArea.TransformPoint(playArea.rect.center);
            var screenCentre = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCentre);
            MoveToScreenPoint(card.RectTransform, screenCentre, eventCamera);
        }

        private float HysteresisInLocalUnits()
        {
            if (playArea == null) return 0f;
            return Mathf.Max(4f, Mathf.Min(armHysteresis, playArea.rect.height * .08f));
        }

        private Vector2 ClampToScreenBounds(RectTransform card, Vector2 screenPoint, Camera eventCamera)
        {
            if (!keepDraggedCardsOnScreen || card == null || Screen.width <= 0 || Screen.height <= 0) return screenPoint;

            var currentScreen = RectTransformUtility.WorldToScreenPoint(eventCamera, card.position);
            var corners = new Vector3[4];
            card.GetWorldCorners(corners);
            var halfWidth = 0f;
            var halfHeight = 0f;
            for (var index = 0; index < corners.Length; index++)
            {
                var corner = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[index]);
                halfWidth = Mathf.Max(halfWidth, Mathf.Abs(corner.x - currentScreen.x));
                halfHeight = Mathf.Max(halfHeight, Mathf.Abs(corner.y - currentScreen.y));
            }

            var minX = Mathf.Min(halfWidth, Screen.width * .5f);
            var minY = Mathf.Min(halfHeight, Screen.height * .5f);
            return new Vector2(
                Mathf.Clamp(screenPoint.x, minX, Screen.width - minX),
                Mathf.Clamp(screenPoint.y, minY, Screen.height - minY));
        }

        private static float SafeRatio(float numerator, float denominator)
        {
            return Mathf.Abs(denominator) < .0001f ? 1f : numerator / denominator;
        }

    }
}
