using Bloomdrawn.Engine.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bloomdrawn.Presentation
{
    public sealed class CombatCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private CombatStageRuntimeBootstrap bootstrap;
        private TextMeshProUGUI label;
        private Image image;
        private bool dragged;
        public string CardId { get; private set; }
        public string OwnerId { get; private set; }
        public bool RequiresEnemyTarget { get; private set; }
        public RectTransform RectTransform => (RectTransform)transform;

        public static CombatCardView Create(Transform parent, CombatStageRuntimeBootstrap bootstrap, CardInstance instance)
        {
            var root = new GameObject("Card " + instance.Id, typeof(RectTransform), typeof(Image), typeof(CombatCardView));
            root.transform.SetParent(parent, false);
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(160, 220);
            var image = root.GetComponent<Image>(); image.color = new Color(.2f, .22f, .34f, 1f);
            var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(root.transform, false); text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF"); text.alignment = TextAlignmentOptions.Center; text.fontSize = 24; text.color = Color.white;
            ((RectTransform)text.transform).anchorMin = Vector2.zero; ((RectTransform)text.transform).anchorMax = Vector2.one; ((RectTransform)text.transform).offsetMin = new Vector2(8, 8); ((RectTransform)text.transform).offsetMax = new Vector2(-8, -8);
            var view = root.GetComponent<CombatCardView>();
            view.bootstrap = bootstrap; view.label = text; view.image = image; view.CardId = instance.Id; view.OwnerId = instance.OwnerId.Value; view.RequiresEnemyTarget = instance.TargetKind == CardTargetKind.OneEnemy;
            text.text = instance.DefinitionId + "\n" + instance.OperationKind + "  " + instance.CurrentCost + " Mana";
            return view;
        }

        public void SetArmed(bool armed) { if (image != null) image.color = armed ? new Color(.42f, .7f, .36f, 1f) : new Color(.2f, .22f, .34f, 1f); }
        public void OnBeginDrag(PointerEventData eventData) { dragged = true; bootstrap.BeginCardDrag(this, eventData); }
        public void OnDrag(PointerEventData eventData) { bootstrap.UpdateCardDrag(this, eventData); }
        public void OnEndDrag(PointerEventData eventData) { bootstrap.ReleaseCardDrag(this); }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right) bootstrap.CancelInteraction();
            else if (!dragged) bootstrap.ClickCard(this);
            dragged = false;
        }
    }
}
