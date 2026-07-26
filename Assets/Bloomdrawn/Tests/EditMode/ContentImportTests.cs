using System;
using System.IO;
using System.Linq;
using Bloomdrawn.Content;
using Bloomdrawn.Content.Editor;
using NUnit.Framework;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class ContentImportTests
    {
        [Test]
        public void FixtureContent_ValidatesBuildsRegistryAndRoundTripsGeneratedJson()
        {
            var validation = ImportFixtures();
            var registry = ContentImportService.CreateRegistry(validation);
            var outputPath = Path.Combine(Path.GetTempPath(), "Bloomdrawn-M0B", "fixture-registry.json");

            ContentImportService.WriteGeneratedRegistry(registry, outputPath);
            var generatedDefinitions = ContentImportService.ReadGeneratedRegistry(outputPath);

            Assert.That(validation.IsValid, Is.True);
            Assert.That(validation.Content.Origin, Is.EqualTo(ContentOrigin.Fixture));
            Assert.That(registry.OrderedDefinitions.Select(item => item.Id), Is.Ordered);
            Assert.That(ContentFingerprint.Compute(generatedDefinitions), Is.EqualTo(registry.ContentHash));
        }

        [Test]
        public void InvalidDefinitions_ReportDuplicateMissingReferenceAndPresentationErrors()
        {
            var definitions = ContentImportService.CreateRegistry(ImportFixtures()).OrderedDefinitions.Select(Clone).ToList();
            definitions.Add(Clone(definitions[0]));
            definitions.First(item => item.Kind == ContentKind.Card).OwnerId = "sample.character.absent";
            definitions.First(item => item.Kind == ContentKind.Card).ContentVersion = string.Empty;
            definitions.First(item => item.Kind == ContentKind.Enemy).PresentationAssetId = "invalid asset";
            definitions.First(item => item.Kind == ContentKind.Encounter).DisplayName = string.Empty;

            var result = ContentValidator.Validate(definitions, ContentOrigin.Fixture);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain("content.duplicate-id"));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain("content.invalid-reference"));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain("content.missing-version"));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain("content.invalid-presentation-id"));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain("content.missing-display-name"));
        }

        [Test]
        public void CanonicalFixtureImport_IsDeterministicAndProductionRequiresValidation()
        {
            var first = ContentImportService.CreateRegistry(ImportFixtures());
            var second = ContentImportService.CreateRegistry(ImportFixtures());
            var invalidProduction = ContentValidator.Validate(new[] { new ContentDefinition() }, ContentOrigin.Production);
            var projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            var emptyProduction = ContentImportService.ImportDirectory(Path.Combine(projectRoot, "GameContent", "production"), ContentOrigin.Production);

            Assert.That(second.ContentHash, Is.EqualTo(first.ContentHash));
            Assert.That(second.OrderedDefinitions.Select(item => item.Id), Is.EqualTo(first.OrderedDefinitions.Select(item => item.Id)));
            Assert.That(invalidProduction.IsValid, Is.False);
            Assert.Throws<ArgumentNullException>(() => ContentRegistry.Create(invalidProduction.Content));
            Assert.That(emptyProduction.IsValid, Is.False);
            Assert.Throws<InvalidOperationException>(() => ContentImportService.CreateRegistry(emptyProduction));
        }

        private static ContentValidationResult ImportFixtures()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            return ContentImportService.ImportDirectory(Path.Combine(projectRoot, "GameContent", "fixtures"), ContentOrigin.Fixture);
        }

        private static ContentDefinition Clone(ContentDefinition source)
        {
            return new ContentDefinition
            {
                ContentVersion = source.ContentVersion,
                Id = source.Id,
                Kind = source.Kind,
                DisplayName = source.DisplayName,
                PresentationAssetId = source.PresentationAssetId,
                OwnerId = source.OwnerId,
                EnemyIds = source.EnemyIds == null ? null : source.EnemyIds.ToList()
            };
        }
    }
}
