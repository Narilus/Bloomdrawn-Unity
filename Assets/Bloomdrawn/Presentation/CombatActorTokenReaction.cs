using Bloomdrawn.Application;
using UnityEngine;

namespace Bloomdrawn.Presentation
{
    /// <summary>
    /// Small presentation-only reactions for the generic fixture/fallback actor. The
    /// authoritative event has already been accepted before this component is called.
    /// </summary>
    public sealed class CombatActorTokenReaction : MonoBehaviour
    {
        private Vector3 idlePosition;
        private Quaternion idleRotation;
        private Vector3 idleScale;
        private bool hasIdlePose;
        private bool animating;
        private bool reducedMotion;
        private float playbackSpeed;
        private float elapsed;
        private float duration;
        private PresentationReaction reaction;

        public PresentationReaction LastReaction { get; private set; }
        public int ReactionCount { get; private set; }

        public void React(PresentationReaction value, bool reduceMotion, float speed)
        {
            CaptureIdlePose();
            LastReaction = value;
            ReactionCount++;
            reaction = value;
            reducedMotion = reduceMotion;
            playbackSpeed = Mathf.Max(0f, speed);
            elapsed = 0f;

            if (value == PresentationReaction.None || reducedMotion || playbackSpeed <= 0f)
            {
                animating = false;
                RestoreIdlePose();
                return;
            }

            duration = .26f;
            animating = true;
        }

        private void Update()
        {
            if (!hasIdlePose || !animating) return;
            if (reducedMotion || playbackSpeed <= 0f)
            {
                animating = false;
                RestoreIdlePose();
                return;
            }

            elapsed += Mathf.Clamp(Time.unscaledDeltaTime, 1f / 240f, .05f) * playbackSpeed;
            var normalized = Mathf.Clamp01(elapsed / Mathf.Max(.001f, duration));
            var envelope = Mathf.Sin(normalized * Mathf.PI);
            var offset = Vector3.zero;
            var rotation = 0f;
            var scale = 1f;

            switch (reaction)
            {
                case PresentationReaction.OwnerAcknowledgement:
                    offset = Vector3.up * (10f * envelope);
                    scale = 1f + .025f * envelope;
                    break;
                case PresentationReaction.Act:
                    offset = Vector3.up * (16f * envelope);
                    scale = 1f + .04f * envelope;
                    break;
                case PresentationReaction.Hit:
                    offset = Vector3.right * (10f * Mathf.Sin(normalized * Mathf.PI * 5f) * (1f - normalized * .25f));
                    rotation = Mathf.Sin(normalized * Mathf.PI * 4f) * 2.5f;
                    scale = 1f - .035f * envelope;
                    break;
                case PresentationReaction.ShieldGain:
                    offset = Vector3.up * (7f * envelope);
                    scale = 1f + .035f * envelope;
                    break;
                case PresentationReaction.Victory:
                    offset = Vector3.up * (13f * envelope);
                    rotation = Mathf.Sin(normalized * Mathf.PI) * 3f;
                    scale = 1f + .045f * envelope;
                    break;
                case PresentationReaction.Defeat:
                    offset = Vector3.down * (8f * envelope);
                    rotation = Mathf.Sin(normalized * Mathf.PI) * -4f;
                    scale = 1f - .04f * envelope;
                    break;
            }

            transform.localPosition = idlePosition + offset;
            transform.localRotation = idleRotation * Quaternion.Euler(0f, 0f, rotation);
            transform.localScale = idleScale * scale;
            if (normalized >= 1f)
            {
                animating = false;
                RestoreIdlePose();
            }
        }

        private void CaptureIdlePose()
        {
            if (hasIdlePose) return;
            idlePosition = transform.localPosition;
            idleRotation = transform.localRotation;
            idleScale = transform.localScale;
            hasIdlePose = true;
        }

        private void RestoreIdlePose()
        {
            if (!hasIdlePose) return;
            transform.localPosition = idlePosition;
            transform.localRotation = idleRotation;
            transform.localScale = idleScale;
        }
    }
}
