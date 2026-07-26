using UnityEngine;
using UnityEngine.UI;

namespace Bloomdrawn.Presentation
{
    public sealed class BootstrapDevShell : MonoBehaviour
    {
        [SerializeField] private Text reducedMotionText;
        [SerializeField] private Text developerStatusText;
        [SerializeField] private bool reducedMotionSeed;

        public bool ReducedMotionSeed => reducedMotionSeed;

        public void Configure(Text reducedMotionStatus, Text developerStatus)
        {
            reducedMotionText = reducedMotionStatus;
            developerStatusText = developerStatus;
            Refresh();
        }

        public void ToggleReducedMotionSeed()
        {
            reducedMotionSeed = !reducedMotionSeed;
            Refresh();
        }

        public void Refresh()
        {
            if (reducedMotionText != null)
                reducedMotionText.text = "Reduced-motion seed: " + (reducedMotionSeed ? "enabled" : "disabled");
            if (developerStatusText != null)
                developerStatusText.text = "Developer status: use bloom.health and bloom.validate-content in the Unity CLI.";
        }

        private void Awake()
        {
            Refresh();
        }
    }
}
