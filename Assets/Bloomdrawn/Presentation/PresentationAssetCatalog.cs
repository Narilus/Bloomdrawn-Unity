using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bloomdrawn.Presentation
{
    public enum PresentationAssetRole { Character, Enemy, Card, Background, Ui }

    [Serializable]
    public sealed class PresentationAssetBinding
    {
        public string LogicalId;
        public PresentationAssetRole Role;
        public UnityEngine.Object Asset;
    }

    [CreateAssetMenu(menuName = "Bloomdrawn/Presentation Asset Catalog")]
    public sealed class PresentationAssetCatalog : ScriptableObject
    {
        [SerializeField] private List<PresentationAssetBinding> bindings = new List<PresentationAssetBinding>();
        public IReadOnlyList<PresentationAssetBinding> Bindings => bindings;

        public void SetBindings(IEnumerable<PresentationAssetBinding> value)
        {
            bindings = value == null ? new List<PresentationAssetBinding>() : new List<PresentationAssetBinding>(value);
        }
    }
}
