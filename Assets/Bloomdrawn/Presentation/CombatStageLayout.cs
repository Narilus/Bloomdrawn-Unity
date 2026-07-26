using UnityEngine;

namespace Bloomdrawn.Presentation
{
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
