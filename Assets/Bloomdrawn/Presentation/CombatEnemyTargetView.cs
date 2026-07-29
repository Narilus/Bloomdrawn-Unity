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
        public void Configure(CombatActorView value, Image graphic) { actor = value; targetGraphic = graphic; }
        public void Bind(CombatStageRuntimeBootstrap value) { bootstrap = value; }
        public void SetTargetable(bool targetable) { if (targetGraphic != null) targetGraphic.color = targetable ? new Color(1f, .7f, .2f, .38f) : new Color(.7f, .18f, .22f, .08f); }
        public void OnPointerClick(PointerEventData eventData) { if (bootstrap != null && actor != null) bootstrap.SelectEnemy(actor.RuntimeId); }
    }
}
