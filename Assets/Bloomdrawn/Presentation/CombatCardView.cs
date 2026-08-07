using Bloomdrawn.Engine.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bloomdrawn.Presentation
{
    public sealed class CombatCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private CombatStageRuntimeBootstrap bootstrap;
        private Image image;
        private GameObject armedCue;
        private bool dragged;
        private bool dragging;
        private bool hovered;
        private bool staged;
        private bool suppressHoverUntilExit;
        private Vector2 restingPosition;
        private float restingRotation;
        private int restingDepth;
        public string CardId { get; private set; }
        public string OwnerId { get; private set; }
        public bool RequiresEnemyTarget { get; private set; }
        public RectTransform RectTransform => (RectTransform)transform;

        public static CombatCardView Create(Transform parent, CombatStageRuntimeBootstrap bootstrap, CardInstance instance, string displayName)
        {
            var root = new GameObject("Card " + instance.Id, typeof(RectTransform), typeof(Image), typeof(CombatCardView));
            root.transform.SetParent(parent, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, .5f);
            rect.sizeDelta = new Vector2(180, 246);
            var image = root.GetComponent<Image>();
            image.color = new Color(.16f, .18f, .28f, 1f);

            var title = CreateText(root.transform, "Title", new Vector2(.10f, .57f), new Vector2(.90f, .90f), 27, TextAlignmentOptions.Top);
            title.text = string.IsNullOrWhiteSpace(displayName) ? "Fixture Card" : displayName;
            title.enableAutoSizing = true;
            title.fontSizeMin = 18;
            title.fontSizeMax = 27;

            var operation = CreateText(root.transform, "Operation", new Vector2(.10f, .30f), new Vector2(.90f, .54f), 18, TextAlignmentOptions.Center);
            operation.text = instance.OperationKind.ToUpperInvariant();
            operation.color = new Color(.74f, .80f, .92f, 1f);

            var fixtureTag = CreateText(root.transform, "Fixture Tag", new Vector2(.10f, .06f), new Vector2(.90f, .20f), 13, TextAlignmentOptions.Center);
            fixtureTag.text = "NON-PRODUCTION FIXTURE";
            fixtureTag.color = new Color(.58f, .64f, .76f, 1f);

            var costRoot = new GameObject("Cost Badge", typeof(RectTransform), typeof(Image));
            costRoot.transform.SetParent(root.transform, false);
            var costRect = (RectTransform)costRoot.transform;
            costRect.anchorMin = costRect.anchorMax = new Vector2(.12f, .89f);
            costRect.sizeDelta = new Vector2(52, 52);
            costRoot.GetComponent<Image>().color = new Color(.88f, .60f, .18f, 1f);
            var cost = CreateText(costRoot.transform, "Cost", Vector2.zero, Vector2.one, 32, TextAlignmentOptions.Center);
            cost.text = instance.CurrentCost.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var cue = new GameObject("Armed Cue", typeof(RectTransform), typeof(Image));
            cue.transform.SetParent(root.transform, false);
            var cueRect = (RectTransform)cue.transform;
            cueRect.anchorMin = new Vector2(.08f, .42f);
            cueRect.anchorMax = new Vector2(.92f, .58f);
            cueRect.offsetMin = cueRect.offsetMax = Vector2.zero;
            cue.GetComponent<Image>().color = new Color(.18f, .50f, .24f, .96f);
            var cueText = CreateText(cue.transform, "Label", Vector2.zero, Vector2.one, 16, TextAlignmentOptions.Center);
            cueText.text = "READY • RELEASE TO PLAY";
            cue.SetActive(false);

            var view = root.GetComponent<CombatCardView>();
            view.bootstrap = bootstrap;
            view.image = image;
            view.armedCue = cue;
            view.CardId = instance.Id;
            view.OwnerId = instance.OwnerId.Value;
            view.RequiresEnemyTarget = instance.TargetKind == CardTargetKind.OneEnemy;
            return view;
        }

        public void SetArmed(bool armed)
        {
            if (image != null) image.color = armed ? new Color(.24f, .44f, .25f, 1f) : new Color(.16f, .18f, .28f, 1f);
            if (armedCue != null) armedCue.SetActive(armed);
        }
        public void SetRestingPose(HandFanPose pose)
        {
            dragging = false;
            staged = false;
            hovered = false;
            restingPosition = pose.Position;
            restingRotation = pose.Rotation;
            restingDepth = pose.Depth;
            SetArmed(false);
            ApplyRestingPose();
        }
        public void SetHovered(bool value)
        {
            hovered = value;
            if (dragging || staged) return;
            ApplyRestingPose();
        }
        public void SetDragging(bool value)
        {
            dragging = value;
            if (value)
            {
                hovered = false;
                staged = false;
                transform.SetAsLastSibling();
            }
        }
        public void SuppressHoverUntilExit()
        {
            suppressHoverUntilExit = true;
            SetHovered(false);
        }
        public void SetStaged(bool value)
        {
            staged = value;
            if (value)
            {
                dragging = false;
                hovered = false;
                transform.SetAsLastSibling();
            }
        }
        public void OnBeginDrag(PointerEventData eventData) { dragged = true; bootstrap.BeginCardDrag(this, eventData); }
        public void OnDrag(PointerEventData eventData) { bootstrap.UpdateCardDrag(this, eventData); }
        public void OnEndDrag(PointerEventData eventData) { bootstrap.ReleaseCardDrag(this); }
        public void OnPointerEnter(PointerEventData eventData) { if (!suppressHoverUntilExit) bootstrap.HoverCard(this); }
        public void OnPointerExit(PointerEventData eventData) { suppressHoverUntilExit = false; bootstrap.UnhoverCard(this); }
        public void OnSelect(BaseEventData eventData) { if (!suppressHoverUntilExit) bootstrap.HoverCard(this); }
        public void OnDeselect(BaseEventData eventData) { bootstrap.UnhoverCard(this); }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right) bootstrap.CancelInteraction();
            else if (!dragged) bootstrap.ClickCard(this);
            dragged = false;
        }

        private void ApplyRestingPose()
        {
            RectTransform.anchoredPosition = restingPosition + (hovered ? Vector2.up * 32f : Vector2.zero);
            RectTransform.localRotation = Quaternion.Euler(0, 0, restingRotation);
            if (transform.parent == null) return;
            transform.SetSiblingIndex(hovered ? transform.parent.childCount - 1 : restingDepth);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float size, TextAlignmentOptions alignment)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            var rect = (RectTransform)text.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

    }
}
