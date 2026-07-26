using System;
using System.Collections.Generic;
using Bloomdrawn.Application;
using UnityEngine;

namespace Bloomdrawn.Presentation
{
    public sealed class CombatActorLookup
    {
        private readonly Dictionary<string, CombatActorView> actors = new Dictionary<string, CombatActorView>(StringComparer.Ordinal);
        public CombatActorLookup(IEnumerable<CombatActorView> views)
        {
            if (views == null) throw new ArgumentNullException(nameof(views));
            foreach (var view in views)
            {
                if (view == null || string.IsNullOrEmpty(view.RuntimeId)) continue;
                if (actors.ContainsKey(view.RuntimeId)) throw new InvalidOperationException("Combat actor runtime IDs must be unique.");
                actors.Add(view.RuntimeId, view);
            }
        }
        public bool TryGet(string runtimeId, out CombatActorView view)
        {
            view = null;
            return !string.IsNullOrEmpty(runtimeId) && actors.TryGetValue(runtimeId, out view);
        }
    }

    public sealed class CombatTokenPresenter
    {
        private readonly CombatActorLookup actorLookup;
        public CombatTokenPresenter(CombatActorLookup lookup) { actorLookup = lookup ?? throw new ArgumentNullException(nameof(lookup)); }
        public void Present(PresentationToken token, bool reducedMotion, float playbackSpeed)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            Present(token.SourceRuntimeId, token.SourceReaction, reducedMotion, playbackSpeed);
            Present(token.TargetRuntimeId, token.TargetReaction, reducedMotion, playbackSpeed);
        }
        private void Present(string runtimeId, PresentationReaction reaction, bool reducedMotion, float playbackSpeed)
        {
            if (reaction == PresentationReaction.None || !actorLookup.TryGet(runtimeId, out var actor)) return;
            var fallback = actor.GetComponent<CombatActorTokenReaction>();
            if (fallback != null) fallback.React(reaction, reducedMotion, playbackSpeed);
        }
    }

    public static class CombatStageActorBinder
    {
        public static void Bind(PartyFormationView party, EnemyFormationView enemies, CombatActorBindingPlan plan)
        {
            if (party == null || enemies == null || plan == null) throw new ArgumentNullException("Combat stage actor binding requires formations and a setup-derived plan.");
            if (party.Actors.Count != plan.PartyRuntimeIds.Count || enemies.Actors.Count != plan.EnemyRuntimeIds.Count) throw new InvalidOperationException("Combat stage actor counts do not match the authoritative setup.");
            for (var index = 0; index < party.Actors.Count; index++) party.Actors[index].SetRuntimeId(plan.PartyRuntimeIds[index]);
            for (var index = 0; index < enemies.Actors.Count; index++) enemies.Actors[index].SetRuntimeId(plan.EnemyRuntimeIds[index]);
        }
    }
}
