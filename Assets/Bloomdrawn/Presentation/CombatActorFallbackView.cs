using System;
using System.Linq;
using Bloomdrawn.Engine.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomdrawn.Presentation
{
    public sealed class CombatActorFallbackView : MonoBehaviour
    {
        [SerializeField] private CombatActorView actor;
        [SerializeField] private bool enemy;
        [SerializeField] private Image visual;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private CombatEnemyTargetView target;
        public CombatActorView Actor => actor;
        public CombatEnemyTargetView Target => target;
        public void Configure(CombatActorView value, bool isEnemy, Image image, TextMeshProUGUI text, CombatEnemyTargetView targetView)
        { actor = value; enemy = isEnemy; visual = image; label = text; target = targetView; }
        public void Refresh(CombatState state, bool targetSelection, Func<string, string, string> displayNameFor)
        {
            if (enemy)
            {
                var values = state.Values.Enemies.FirstOrDefault(value => value.RuntimeId.Value == actor.RuntimeId);
                var setup = state.Setup.Enemies.FirstOrDefault(value => value.RuntimeId.Value == actor.RuntimeId);
                var name = displayNameFor(setup?.DefinitionId, "Fixture Enemy");
                label.text = values == null ? name : name + "\nHP " + values.CurrentHp + "/" + values.MaximumHp + "   SHIELD " + values.Shield;
                target.SetTargetable(targetSelection && values != null && values.CurrentHp > 0);
                visual.color = values != null && values.CurrentHp <= 0 ? new Color(.24f, .24f, .24f, 1f) : new Color(.7f, .18f, .22f, 1f);
            }
            else
            {
                var setup = state.Setup.Party.FirstOrDefault(value => value.RuntimeId.Value == actor.RuntimeId);
                label.text = displayNameFor(setup?.DefinitionId, "Fixture Party") + "\nFIXTURE PARTY";
                visual.color = new Color(.18f, .42f, .7f, 1f);
            }
        }
    }
}
