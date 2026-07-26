using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bloomdrawn.Presentation
{
    public enum CombatActorAnchorRole { Visual, Target, Selection, Status, Vfx, Intent }

    public sealed class CombatActorView : MonoBehaviour
    {
        [SerializeField] private string runtimeId;
        [SerializeField] private Transform visualAnchor;
        [SerializeField] private Transform targetAnchor;
        [SerializeField] private Transform selectionAnchor;
        [SerializeField] private Transform statusAnchor;
        [SerializeField] private Transform vfxAnchor;
        [SerializeField] private Transform intentAnchor;
        public string RuntimeId => runtimeId;
        public void Configure(string id, Transform visual, Transform target, Transform selection, Transform status, Transform vfx, Transform intent)
        { runtimeId = id; visualAnchor = visual; targetAnchor = target; selectionAnchor = selection; statusAnchor = status; vfxAnchor = vfx; intentAnchor = intent; }
        public Transform Anchor(CombatActorAnchorRole role) => role == CombatActorAnchorRole.Visual ? visualAnchor : role == CombatActorAnchorRole.Target ? targetAnchor : role == CombatActorAnchorRole.Selection ? selectionAnchor : role == CombatActorAnchorRole.Status ? statusAnchor : role == CombatActorAnchorRole.Vfx ? vfxAnchor : intentAnchor;
    }

    public sealed class PartyFormationView : MonoBehaviour { [SerializeField] private List<CombatActorView> actors = new List<CombatActorView>(); public IReadOnlyList<CombatActorView> Actors => actors; public void Configure(List<CombatActorView> value) { actors = value; } }
    public sealed class EnemyFormationView : MonoBehaviour { [SerializeField] private List<CombatActorView> actors = new List<CombatActorView>(); public IReadOnlyList<CombatActorView> Actors => actors; public void Configure(List<CombatActorView> value) { actors = value; } }
    public sealed class CombatStageLayout : MonoBehaviour
    {
        public const float WidthHeightMatch = .5f; public const int ReferenceWidth = 1920, ReferenceHeight = 1080;
        [SerializeField] private RectTransform handSafeArea;
        [SerializeField] private RectTransform sharedSurvivalLane;
        [SerializeField] private RectTransform enemyTargetLane;
        [SerializeField] private RectTransform endTurnControl;
        [SerializeField] private RectTransform combatLogOverlay;
        public RectTransform HandSafeArea => handSafeArea; public RectTransform SharedSurvivalLane => sharedSurvivalLane; public RectTransform EnemyTargetLane => enemyTargetLane; public RectTransform EndTurnControl => endTurnControl; public RectTransform CombatLogOverlay => combatLogOverlay;
        public void Configure(RectTransform hand, RectTransform survival, RectTransform enemy, RectTransform endTurn, RectTransform log) { handSafeArea=hand; sharedSurvivalLane=survival; enemyTargetLane=enemy; endTurnControl=endTurn; combatLogOverlay=log; }
    }
}
