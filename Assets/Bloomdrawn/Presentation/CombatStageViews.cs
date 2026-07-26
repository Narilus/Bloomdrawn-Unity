using System;
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
        public void SetRuntimeId(string id) { runtimeId = string.IsNullOrEmpty(id) ? throw new ArgumentException("Runtime ID is required.", nameof(id)) : id; }
        public Transform Anchor(CombatActorAnchorRole role) => role == CombatActorAnchorRole.Visual ? visualAnchor : role == CombatActorAnchorRole.Target ? targetAnchor : role == CombatActorAnchorRole.Selection ? selectionAnchor : role == CombatActorAnchorRole.Status ? statusAnchor : role == CombatActorAnchorRole.Vfx ? vfxAnchor : intentAnchor;
    }

}
