using System.Collections.Generic;
using UnityEngine;

namespace Bloomdrawn.Presentation
{
    public sealed class EnemyFormationView : MonoBehaviour
    {
        [SerializeField] private List<CombatActorView> actors = new List<CombatActorView>();
        public IReadOnlyList<CombatActorView> Actors => actors;
        public void Configure(List<CombatActorView> value) { actors = value; }
    }
}
