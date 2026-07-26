using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bloomdrawn.Content
{
    public sealed class ContentValidationError
    {
        public ContentValidationError(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; }

        public string Message { get; }
    }

    public sealed class ValidatedContent
    {
        internal ValidatedContent(ContentOrigin origin, IReadOnlyList<ContentDefinition> definitions)
        {
            Origin = origin;
            Definitions = definitions;
        }

        public ContentOrigin Origin { get; }

        public IReadOnlyList<ContentDefinition> Definitions { get; }
    }

    public sealed class ContentValidationResult
    {
        internal ContentValidationResult(IReadOnlyList<ContentValidationError> errors, ValidatedContent content)
        {
            Errors = errors;
            Content = content;
        }

        public IReadOnlyList<ContentValidationError> Errors { get; }

        public ValidatedContent Content { get; }

        public bool IsValid => Errors.Count == 0;
    }

    public static class ContentValidator
    {
        private static readonly Regex StableIdPattern = new Regex("^[a-z][a-z0-9]*(?:[.-][a-z0-9]+)*$", RegexOptions.Compiled);
        private static readonly Regex PresentationIdPattern = new Regex("^presentation\\.(?:character|card|enemy|encounter|background|ui)\\.[a-z][a-z0-9]*(?:[.-][a-z0-9]+)*$", RegexOptions.Compiled);
        private const string M1FixturePrefix = "fixture.m1.";

        public static ContentValidationResult Validate(IEnumerable<ContentDefinition> definitions, ContentOrigin origin)
        {
            var source = definitions == null ? new List<ContentDefinition>() : definitions.Where(item => item != null).ToList();
            var errors = new List<ContentValidationError>();

            if (definitions == null || source.Count == 0)
            {
                errors.Add(new ContentValidationError("content.empty", "At least one content definition is required."));
            }

            if (definitions != null && source.Count != definitions.Count())
            {
                errors.Add(new ContentValidationError("content.null", "Content definitions cannot contain null entries."));
            }

            foreach (var definition in source)
            {
                ValidateDefinition(definition, origin, errors);
            }

            foreach (var duplicate in source.Where(item => !string.IsNullOrWhiteSpace(item.Id))
                         .GroupBy(item => item.Id, StringComparer.Ordinal)
                         .Where(group => group.Count() > 1))
            {
                errors.Add(new ContentValidationError("content.duplicate-id", string.Format("Duplicate content ID '{0}'.", duplicate.Key)));
            }

            var byId = source.Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .GroupBy(item => item.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            foreach (var definition in source)
            {
                ValidateReferences(definition, byId, errors);
            }

            var ordered = source.OrderBy(item => item.Id, StringComparer.Ordinal).ToList();
            return new ContentValidationResult(errors, errors.Count == 0 ? new ValidatedContent(origin, ordered) : null);
        }

        private static void ValidateDefinition(ContentDefinition definition, ContentOrigin origin, ICollection<ContentValidationError> errors)
        {
            if (string.IsNullOrWhiteSpace(definition.ContentVersion))
            {
                errors.Add(new ContentValidationError("content.missing-version", "Content version is required."));
            }

            if (string.IsNullOrWhiteSpace(definition.Id) || !StableIdPattern.IsMatch(definition.Id))
            {
                errors.Add(new ContentValidationError("content.invalid-id", "Content ID must be a lowercase stable ID."));
            }

            if (!Enum.IsDefined(typeof(ContentKind), definition.Kind))
            {
                errors.Add(new ContentValidationError("content.invalid-kind", "Content kind is invalid."));
            }

            if (string.IsNullOrWhiteSpace(definition.DisplayName))
            {
                errors.Add(new ContentValidationError("content.missing-display-name", "Display name is required."));
            }

            if (string.IsNullOrWhiteSpace(definition.PresentationAssetId) || !PresentationIdPattern.IsMatch(definition.PresentationAssetId))
            {
                errors.Add(new ContentValidationError("content.invalid-presentation-id", "Presentation asset ID is malformed or uses an unsupported role."));
            }

            if (definition.Kind == ContentKind.FixtureLineup && origin != ContentOrigin.Fixture)
            {
                errors.Add(new ContentValidationError("content.fixture-only", "Fixture lineups may only be validated from fixture content."));
            }

            if (IsM1Fixture(definition))
            {
                ValidateM1FixtureFields(definition, errors);
            }
        }

        private static void ValidateReferences(ContentDefinition definition, IReadOnlyDictionary<string, ContentDefinition> byId, ICollection<ContentValidationError> errors)
        {
            if (definition.Kind == ContentKind.Card)
            {
                ValidateReference(definition.Id, definition.OwnerId, ContentKind.Character, "owner", byId, errors);
            }

            if (definition.Kind == ContentKind.Encounter)
            {
                if (definition.EnemyIds == null || definition.EnemyIds.Count == 0)
                {
                    errors.Add(new ContentValidationError("content.missing-enemies", "Encounter requires at least one enemy reference."));
                    return;
                }

                foreach (var enemyId in definition.EnemyIds)
                {
                    ValidateReference(definition.Id, enemyId, ContentKind.Enemy, "enemy", byId, errors);
                }
            }

            if (definition.Kind == ContentKind.FixtureLineup)
            {
                ValidateFixtureLineupReferences(definition, byId, errors);
            }
        }

        private static void ValidateM1FixtureFields(ContentDefinition definition, ICollection<ContentValidationError> errors)
        {
            switch (definition.Kind)
            {
                case ContentKind.Character:
                    if (!IsPositive(definition.MaxHp) || !IsNonNegative(definition.Attack) || !IsNonNegative(definition.Defense))
                        errors.Add(new ContentValidationError("content.fixture-invalid-stats", "M1 fixture character requires positive Max HP and non-negative Attack/Defense."));
                    break;
                case ContentKind.Card:
                    if (!IsNonNegative(definition.PrintedCost) || !IsOneOf(definition.TargetKind, "oneEnemy", "party") || !IsOneOf(definition.OperationKind, "strike", "shield"))
                        errors.Add(new ContentValidationError("content.fixture-invalid-card", "M1 fixture card requires non-negative printed cost, supported target kind, and supported operation kind."));
                    break;
                case ContentKind.Enemy:
                    if (!IsPositive(definition.MaxHp) || !IsOneOf(definition.InitialIntentKind, "attack") || !IsNonNegative(definition.InitialIntentDamage))
                        errors.Add(new ContentValidationError("content.fixture-invalid-enemy", "M1 fixture enemy requires positive Max HP and attack initial intent data."));
                    break;
                case ContentKind.FixtureLineup:
                    if (definition.CharacterIds == null || definition.CharacterIds.Count != 4 || definition.CharacterIds.Distinct(StringComparer.Ordinal).Count() != 4 || definition.DeckRecipe == null || definition.DeckRecipe.Count != 8)
                        errors.Add(new ContentValidationError("content.fixture-invalid-lineup", "M1 fixture lineup requires exactly four unique characters and eight deck recipe entries."));
                    break;
            }
        }

        private static void ValidateFixtureLineupReferences(ContentDefinition definition, IReadOnlyDictionary<string, ContentDefinition> byId, ICollection<ContentValidationError> errors)
        {
            foreach (var characterId in definition.CharacterIds ?? Enumerable.Empty<string>())
            {
                ValidateReference(definition.Id, characterId, ContentKind.Character, "lineup character", byId, errors);
            }

            foreach (var cardId in definition.DeckRecipe ?? Enumerable.Empty<string>())
            {
                ValidateReference(definition.Id, cardId, ContentKind.Card, "deck card", byId, errors);
                if (!string.IsNullOrWhiteSpace(cardId) && byId.TryGetValue(cardId, out var card) && card.Kind == ContentKind.Card &&
                    !(definition.CharacterIds ?? new List<string>()).Contains(card.OwnerId))
                {
                    errors.Add(new ContentValidationError("content.fixture-invalid-deck-owner", string.Format("Lineup '{0}' deck card '{1}' owner is not in the lineup.", definition.Id, cardId)));
                }
            }
        }

        private static bool IsM1Fixture(ContentDefinition definition)
        {
            return definition.Id != null && definition.Id.StartsWith(M1FixturePrefix, StringComparison.Ordinal);
        }

        private static bool IsPositive(int? value) => value.HasValue && value.Value > 0;
        private static bool IsNonNegative(int? value) => value.HasValue && value.Value >= 0;
        private static bool IsOneOf(string value, params string[] allowed) => allowed.Contains(value, StringComparer.Ordinal);

        private static void ValidateReference(string ownerId, string referenceId, ContentKind expectedKind, string referenceName, IReadOnlyDictionary<string, ContentDefinition> byId, ICollection<ContentValidationError> errors)
        {
            ContentDefinition target;
            if (string.IsNullOrWhiteSpace(referenceId) || !byId.TryGetValue(referenceId, out target) || target.Kind != expectedKind)
            {
                errors.Add(new ContentValidationError("content.invalid-reference", string.Format("Content '{0}' has invalid {1} reference '{2}'.", ownerId, referenceName, referenceId)));
            }
        }
    }
}
