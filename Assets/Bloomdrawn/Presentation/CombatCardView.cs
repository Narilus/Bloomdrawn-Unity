using Bloomdrawn.Engine.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bloomdrawn.Presentation
{
    public sealed class CombatCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private const float HoverRise = 32f;
        private const float HoverScale = 1.055f;
        private const float StagedScale = 1.08f;
        private const float HoverInMotionDuration = .075f;
        private const float HoverOutMotionDuration = .025f;
        private const float LayoutMotionDuration = .10f;
        // Return is intentionally a short, eased micro-tween: it removes the drag
        // transform without leaving a visible snap while keeping the hand responsive.
        private const float ReturnMotionDuration = .014f;
        private const float DragResponseTime = .012f;
        private const float StageResponseTime = .08f;

        private CombatStageRuntimeBootstrap bootstrap;
        private Image image;
        private GameObject armedCue;
        private Image armedCueImage;
        private TextMeshProUGUI armedCueText;
        private bool dragged;
        private bool dragging;
        private bool hovered;
        private bool staged;
        private bool suppressHoverUntilExit;
        private bool poseInitialized;
        private bool localMotion;
        private bool worldMotion;
        private bool armed;
        private bool hasPointerDownScale;
        private bool freezePointerDownScale;
        private Vector3 pointerDownWorldScale;
        private Vector2 restingPosition;
        private float restingRotation;
        private int restingDepth;
        private Vector2 localMotionStartPosition;
        private float localMotionStartRotation;
        private Vector3 localMotionStartScale;
        private Vector2 localMotionTargetPosition;
        private float localMotionTargetRotation;
        private Vector3 localMotionTargetScale;
        private float localMotionElapsed;
        private float localMotionDuration;
        private Vector3 worldMotionTargetPosition;
        private Quaternion worldMotionTargetRotation;
        private float worldMotionResponseTime = DragResponseTime;
        private Vector3 worldScaleTarget = Vector3.one;
        private bool preserveWorldScale;
        private Vector3 restingScale = Vector3.one;
        private Vector3 targetScale = Vector3.one;

        public string CardId { get; private set; }
        public string OwnerId { get; private set; }
        public bool RequiresEnemyTarget { get; private set; }
        public RectTransform RectTransform => (RectTransform)transform;
        public bool IsStaged => staged;
        public bool IsDragging => dragging;
        public bool IsHovered => hovered;
        public Vector3 DragWorldScale => hasPointerDownScale ? pointerDownWorldScale : RectTransform.lossyScale;

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
            var cueImage = cue.GetComponent<Image>();
            cueImage.color = new Color(.18f, .50f, .24f, .96f);
            cueImage.raycastTarget = false;
            var cueText = CreateText(cue.transform, "Label", Vector2.zero, Vector2.one, 16, TextAlignmentOptions.Center);
            cueText.text = "READY • RELEASE TO PLAY";
            cue.SetActive(false);

            var view = root.GetComponent<CombatCardView>();
            view.bootstrap = bootstrap;
            view.image = image;
            view.armedCue = cue;
            view.armedCueImage = cueImage;
            view.armedCueText = cueText;
            view.CardId = instance.Id;
            view.OwnerId = instance.OwnerId.Value;
            view.RequiresEnemyTarget = instance.TargetKind == CardTargetKind.OneEnemy;
            view.restingScale = root.transform.localScale;
            view.targetScale = view.restingScale;
            return view;
        }

        public void SetArmed(bool value)
        {
            armed = value;
            if (image != null) image.color = value ? new Color(.24f, .44f, .25f, 1f) : new Color(.16f, .18f, .28f, 1f);
            if (armedCue != null)
            {
                armedCue.SetActive(value);
                if (value)
                {
                    if (armedCueImage != null) armedCueImage.color = new Color(.18f, .50f, .24f, .96f);
                    if (armedCueText != null) armedCueText.text = "READY • RELEASE TO PLAY";
                }
                if (!value) armedCue.transform.localScale = Vector3.one;
            }
        }

        public void SetRestingPose(HandFanPose pose)
        {
            var wasInteractive = dragging || staged || worldMotion;
            dragging = false;
            staged = false;
            hovered = false;
            preserveWorldScale = false;
            freezePointerDownScale = false;
            restingPosition = pose.Position;
            restingRotation = pose.Rotation;
            restingDepth = pose.Depth;
            SetArmed(false);
            RequestLocalPose(
                restingPosition,
                restingRotation,
                restingScale,
                wasInteractive ? ReturnMotionDuration : LayoutMotionDuration,
                !poseInitialized);
            ApplySiblingIndex(false);
        }

        public void SetHovered(bool value)
        {
            hovered = value;
            if (dragging || staged) return;
            RequestLocalPose(
                restingPosition + (value ? Vector2.up * HoverRise : Vector2.zero),
                restingRotation,
                value ? restingScale * HoverScale : restingScale,
                value ? HoverInMotionDuration : HoverOutMotionDuration);
            ApplySiblingIndex(value);
        }

        public void SetDragging(bool value)
        {
            if (value && dragging)
            {
                transform.SetAsLastSibling();
                return;
            }

            dragging = value;
            if (value)
            {
                hovered = false;
                staged = false;
                localMotion = false;
                worldMotion = true;
                worldMotionTargetPosition = transform.position;
                // Preserve the fan rotation through the grab. Straightening here would
                // change lossy scale on a scaled Canvas and read as a reparenting jump.
                // Target staging straightens the card once the drag commitment is clear.
                worldMotionTargetRotation = transform.rotation;
                worldMotionResponseTime = DragResponseTime;
                targetScale = transform.localScale;
                preserveWorldScale = false;
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
            if (!value)
            {
                staged = false;
                return;
            }

            var wasStaged = staged;
            staged = true;
            dragging = false;
            hovered = false;
            if (!wasStaged)
            {
                localMotion = false;
                worldMotion = true;
                worldMotionTargetPosition = transform.position;
                worldMotionTargetRotation = Quaternion.identity;
                worldMotionResponseTime = StageResponseTime;
                preserveWorldScale = false;
                transform.SetAsLastSibling();
            }
            armed = false;
            if (armedCue != null)
            {
                armedCue.SetActive(true);
                if (armedCueImage != null) armedCueImage.color = new Color(.18f, .34f, .56f, .96f);
                if (armedCueText != null) armedCueText.text = "SELECT A LEGAL TARGET";
            }
            targetScale = restingScale * StagedScale;
        }

        public void PreserveWorldScale(Vector3 worldScale)
        {
            worldScaleTarget = worldScale;
            preserveWorldScale = true;
            freezePointerDownScale = false;
            transform.localScale = LocalScaleForWorldScale(worldScaleTarget);
        }

        public void SetTransientWorldPosition(Vector3 worldPosition)
        {
            if (!worldMotion)
            {
                worldMotion = true;
                localMotion = false;
                worldMotionTargetRotation = transform.rotation;
            }

            worldMotionTargetPosition = worldPosition;
            worldMotionResponseTime = staged ? StageResponseTime : DragResponseTime;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerDownWorldScale = RectTransform.lossyScale;
            hasPointerDownScale = true;
            freezePointerDownScale = true;
        }
        public void OnBeginDrag(PointerEventData eventData) { dragged = true; bootstrap.BeginCardDrag(this, eventData); }
        public void OnDrag(PointerEventData eventData) { bootstrap.UpdateCardDrag(this, eventData); }
        public void OnEndDrag(PointerEventData eventData) { bootstrap.ReleaseCardDrag(this); hasPointerDownScale = false; freezePointerDownScale = false; }
        public void OnPointerEnter(PointerEventData eventData) { if (!suppressHoverUntilExit) bootstrap.HoverCard(this); }
        public void OnPointerExit(PointerEventData eventData) { suppressHoverUntilExit = false; bootstrap.UnhoverCard(this); }
        public void OnSelect(BaseEventData eventData) { if (!suppressHoverUntilExit) bootstrap.HoverCard(this); }
        public void OnDeselect(BaseEventData eventData) { bootstrap.UnhoverCard(this); }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right) bootstrap.CancelInteraction();
            else if (!dragged) bootstrap.ClickCard(this);
            dragged = false;
            hasPointerDownScale = false;
            freezePointerDownScale = false;
        }

        private void Update()
        {
            var deltaTime = Mathf.Clamp(Time.unscaledDeltaTime, 1f / 240f, .05f);
            if (worldMotion)
            {
                var alpha = 1f - Mathf.Exp(-deltaTime / Mathf.Max(.001f, worldMotionResponseTime));
                transform.position = Vector3.Lerp(transform.position, worldMotionTargetPosition, alpha);
                transform.rotation = Quaternion.Slerp(transform.rotation, worldMotionTargetRotation, alpha);
                transform.localScale = preserveWorldScale
                    ? LocalScaleForWorldScale(worldScaleTarget)
                    : Vector3.Lerp(transform.localScale, targetScale, alpha);
            }
            else if (localMotion)
            {
                localMotionElapsed += deltaTime;
                var linear = Mathf.Clamp01(localMotionElapsed / Mathf.Max(.001f, localMotionDuration));
                var eased = 1f - Mathf.Pow(1f - linear, 3f);
                RectTransform.anchoredPosition = Vector2.LerpUnclamped(localMotionStartPosition, localMotionTargetPosition, eased);
                RectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(localMotionStartRotation, localMotionTargetRotation, eased));
                transform.localScale = freezePointerDownScale
                    ? LocalScaleForWorldScale(pointerDownWorldScale)
                    : Vector3.LerpUnclamped(localMotionStartScale, localMotionTargetScale, eased);
                if (linear >= 1f)
                {
                    RectTransform.anchoredPosition = localMotionTargetPosition;
                    RectTransform.localRotation = Quaternion.Euler(0f, 0f, localMotionTargetRotation);
                    transform.localScale = localMotionTargetScale;
                    localMotion = false;
                }
            }

            if (armed && armedCue != null && armedCue.activeSelf)
            {
                var pulse = 1f + Mathf.Sin(Time.unscaledTime * 8f) * .025f;
                armedCue.transform.localScale = Vector3.one * pulse;
            }
        }

        private void RequestLocalPose(Vector2 position, float rotation, Vector3 scale, float duration, bool snap = false)
        {
            if (snap)
            {
                worldMotion = false;
                localMotion = false;
                RectTransform.anchoredPosition = position;
                RectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
                transform.localScale = scale;
                poseInitialized = true;
                return;
            }

            if (localMotion &&
                Vector2.Distance(localMotionTargetPosition, position) < .01f &&
                Mathf.Abs(Mathf.DeltaAngle(localMotionTargetRotation, rotation)) < .01f &&
                Vector3.Distance(localMotionTargetScale, scale) < .001f)
                return;

            localMotionStartPosition = RectTransform.anchoredPosition;
            localMotionStartRotation = RectTransform.localEulerAngles.z;
            localMotionStartScale = transform.localScale;
            localMotionTargetPosition = position;
            localMotionTargetRotation = rotation;
            localMotionTargetScale = scale;
            localMotionElapsed = 0f;
            localMotionDuration = Mathf.Max(.001f, duration);
            localMotion = true;
            worldMotion = false;
            preserveWorldScale = false;
            poseInitialized = true;
        }

        private Vector3 LocalScaleForWorldScale(Vector3 worldScale)
        {
            var parentScale = transform.parent == null ? Vector3.one : transform.parent.lossyScale;
            return new Vector3(
                SafeDivide(worldScale.x, parentScale.x),
                SafeDivide(worldScale.y, parentScale.y),
                SafeDivide(worldScale.z, parentScale.z));
        }

        private static float SafeDivide(float numerator, float denominator)
        {
            return Mathf.Abs(denominator) < .0001f ? numerator : numerator / denominator;
        }

        private void ApplySiblingIndex(bool focus)
        {
            if (transform.parent == null) return;
            var maximum = transform.parent.childCount - 1;
            var index = focus ? maximum : Mathf.Clamp(restingDepth, 0, maximum);
            transform.SetSiblingIndex(index);
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
