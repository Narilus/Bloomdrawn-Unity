using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bloomdrawn.Presentation
{
    public sealed class CombatEnemyTargetView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CombatActorView actor;
        [SerializeField] private Image targetGraphic;
        private CombatStageRuntimeBootstrap bootstrap;
        private Outline outline;
        private bool targetable;
        public void Configure(CombatActorView value, Image graphic) { actor = value; targetGraphic = graphic; }
        public void Bind(CombatStageRuntimeBootstrap value) { bootstrap = value; }
        public void SetTargetable(bool value)
        {
            targetable = value;
            if (targetGraphic == null) return;
            targetGraphic.color = value ? new Color(1f, .7f, .2f, .38f) : new Color(.7f, .18f, .22f, .08f);
            targetGraphic.transform.localScale = value ? Vector3.one * 1.06f : Vector3.one;
            if (outline == null) outline = targetGraphic.gameObject.AddComponent<Outline>();
            outline.enabled = value;
            outline.effectColor = new Color(1f, .82f, .35f, .95f);
            outline.effectDistance = new Vector2(3f, 3f);
            outline.useGraphicAlpha = false;
        }
        public void OnPointerClick(PointerEventData eventData) { if (targetable && bootstrap != null && actor != null) bootstrap.SelectEnemy(actor.RuntimeId); }
    }
}
