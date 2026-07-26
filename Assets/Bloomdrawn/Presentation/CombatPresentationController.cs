using System;
using Bloomdrawn.Application;
using UnityEngine;

namespace Bloomdrawn.Presentation
{
    public sealed class CombatPresentationController : MonoBehaviour
    {
        [SerializeField] private bool reducedMotion;
        [SerializeField] private float playbackSpeed = 1f;
        [SerializeField] private PartyFormationView partyFormation;
        [SerializeField] private EnemyFormationView enemyFormation;
        private CombatSession session;
        private CombatTokenPresenter presenter;
        public bool ReducedMotion => reducedMotion;
        public float PlaybackSpeed => playbackSpeed;
        public bool IsPresenting => session != null && session.IsInputLocked;
        public void Configure(PartyFormationView party, EnemyFormationView enemies) { partyFormation = party; enemyFormation = enemies; }
        public void ConfigurePlayback(bool reduceMotion, float speed) { reducedMotion = reduceMotion; playbackSpeed = Mathf.Max(0f, speed); }
        public void BindSession(CombatSession value)
        {
            session = value ?? throw new ArgumentNullException(nameof(value));
            CombatStageActorBinder.Bind(partyFormation, enemyFormation, session.ActorBindings);
            presenter = new CombatTokenPresenter(new CombatActorLookup(FindObjectsByType<CombatActorView>(FindObjectsSortMode.None)));
        }
        public bool PresentNext()
        {
            if (session == null || presenter == null || !session.TryPeekPresentation(out var token)) return false;
            presenter.Present(token, reducedMotion, playbackSpeed);
            return session.CompletePresentation(token.EventSequence);
        }
    }
}
