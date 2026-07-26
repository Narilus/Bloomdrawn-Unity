using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Bloomdrawn.Content
{
    public sealed class ContentRegistry
    {
        private readonly IReadOnlyDictionary<string, ContentDefinition> definitions;

        private ContentRegistry(IReadOnlyList<ContentDefinition> orderedDefinitions)
        {
            definitions = orderedDefinitions.ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);
            OrderedDefinitions = orderedDefinitions;
            ContentHash = ContentFingerprint.Compute(orderedDefinitions);
        }

        public IReadOnlyList<ContentDefinition> OrderedDefinitions { get; }

        public string ContentHash { get; }

        public static ContentRegistry Create(ValidatedContent validatedContent)
        {
            if (validatedContent == null)
            {
                throw new ArgumentNullException(nameof(validatedContent));
            }

            return new ContentRegistry(validatedContent.Definitions);
        }

        public bool TryGet(string id, out ContentDefinition definition)
        {
            return definitions.TryGetValue(id, out definition);
        }
    }

    public static class ContentFingerprint
    {
        public static string Compute(IEnumerable<ContentDefinition> definitions)
        {
            var canonicalLines = definitions
                .OrderBy(item => item.Id, StringComparer.Ordinal)
                .Select(item => string.Join("|", new[]
                {
                    item.ContentVersion ?? string.Empty,
                    item.Kind.ToString(),
                    item.Id ?? string.Empty,
                    item.DisplayName ?? string.Empty,
                    item.PresentationAssetId ?? string.Empty,
                    item.OwnerId ?? string.Empty,
                    string.Join(",", (item.EnemyIds ?? new List<string>()).OrderBy(enemy => enemy, StringComparer.Ordinal))
                }));

            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", canonicalLines)));
                return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
