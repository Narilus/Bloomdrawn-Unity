using System;
using System.Collections.Generic;
using System.Linq;
using Bloomdrawn.Content;
using Bloomdrawn.Presentation;
using UnityEngine;

namespace Bloomdrawn.Editor.Tooling
{
    public sealed class PresentationCatalogValidationResult
    {
        public PresentationCatalogValidationResult(IReadOnlyList<string> errors) { Errors = errors; }
        public IReadOnlyList<string> Errors { get; }
        public bool IsValid => Errors.Count == 0;
    }

    public static class PresentationAssetCatalogValidator
    {
        public static PresentationCatalogValidationResult Validate(PresentationAssetCatalog catalog, IEnumerable<ContentDefinition> content)
        {
            var errors = new List<string>();
            var bindings = catalog == null ? new List<PresentationAssetBinding>() : catalog.Bindings.Where(item => item != null).ToList();
            foreach (var duplicate in bindings.Where(item => !string.IsNullOrWhiteSpace(item.LogicalId)).GroupBy(item => item.LogicalId, StringComparer.Ordinal).Where(group => group.Count() > 1))
                errors.Add("catalog.duplicate-id:" + duplicate.Key);
            foreach (var binding in bindings)
            {
                if (!RoleMatches(binding.LogicalId, binding.Role)) errors.Add("catalog.wrong-role:" + binding.LogicalId);
                if (!(binding.Asset is Sprite)) errors.Add("catalog.wrong-asset-type:" + binding.LogicalId);
            }
            var ids = new HashSet<string>(bindings.Where(item => item.Asset is Sprite).Select(item => item.LogicalId), StringComparer.Ordinal);
            foreach (var definition in content ?? Enumerable.Empty<ContentDefinition>())
                if (definition != null && definition.RequiresPresentationBindingForCurrentMilestone && !ids.Contains(definition.PresentationAssetId))
                    errors.Add("catalog.unresolved-required:" + definition.PresentationAssetId);
            return new PresentationCatalogValidationResult(errors);
        }

        private static bool RoleMatches(string id, PresentationAssetRole role)
        {
            return !string.IsNullOrWhiteSpace(id) && id.StartsWith("presentation." + role.ToString().ToLowerInvariant() + ".", StringComparison.Ordinal);
        }
    }
}
