using Bloomdrawn.Application;
using UnityEngine;

namespace Bloomdrawn.Presentation
{
    public sealed class CombatActorTokenReaction : MonoBehaviour
    {
        private Vector3 idlePosition;
        private bool hasIdlePosition;
        public PresentationReaction LastReaction { get; private set; }
        public int ReactionCount { get; private set; }
        public void React(PresentationReaction reaction, bool reducedMotion, float playbackSpeed)
        {
            if (!hasIdlePosition) { idlePosition = transform.localPosition; hasIdlePosition = true; }
            LastReaction = reaction; ReactionCount++;
            if (!reducedMotion && playbackSpeed > 0f && reaction != PresentationReaction.None) transform.localPosition = idlePosition + new Vector3(0f, .035f, 0f);
            transform.localPosition = idlePosition;
        }
    }
}
