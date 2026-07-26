using System;
using System.Collections.Generic;
using System.IO;
using Bloomdrawn.Content;
using Bloomdrawn.Editor.Tooling;
using Bloomdrawn.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class EditorToolingTests
    {
        [Test]
        public void HealthAndContentCommands_ReportFixtureRegistryStatus()
        {
            var health = BloomdrawnEditorCommands.Health();
            var validation = BloomdrawnEditorCommands.ValidateContent();

            Assert.That(health.EditorVersion, Is.Not.Empty);
            Assert.That(health.PipelineReady, Is.True);
            Assert.That(health.EditorReady, Is.True);
            Assert.That(health.CompilationActive, Is.False);
            Assert.That(health.CompileFailed, Is.False);
            Assert.That(health.CompileSucceeded, Is.True);
            Assert.That(health.RegistryValid, Is.True);
            Assert.That(health.DefinitionCount, Is.GreaterThan(0));
            Assert.That(validation.Valid, Is.True);
        }

        [Test]
        public void ContentCommand_RejectsControlledInvalidContent()
        {
            var directory = Path.Combine(Path.GetTempPath(), "BloomdrawnInvalidContent", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "invalid.yaml"), "id: invalid\nkind: Character\ndisplayName: Invalid\npresentationAssetId: presentation.character.invalid");
            try
            {
                Assert.Throws<InvalidOperationException>(() => BloomdrawnEditorCommands.ValidateContent(directory));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void CatalogValidation_DetectsDuplicateWrongRoleTypeAndUnresolvedRequiredBinding()
        {
            var catalog = ScriptableObject.CreateInstance<PresentationAssetCatalog>();
            var texture = new Texture2D(2, 2);
            try
            {
                catalog.SetBindings(new List<PresentationAssetBinding>
                {
                    new PresentationAssetBinding { LogicalId = "presentation.enemy.fixture", Role = PresentationAssetRole.Character, Asset = texture },
                    new PresentationAssetBinding { LogicalId = "presentation.enemy.fixture", Role = PresentationAssetRole.Enemy, Asset = texture }
                });
                var result = PresentationAssetCatalogValidator.Validate(catalog, new[]
                {
                    new ContentDefinition { PresentationAssetId = "presentation.ui.required", RequiresPresentationBindingForCurrentMilestone = true }
                });

                Assert.That(result.Errors, Does.Contain("catalog.duplicate-id:presentation.enemy.fixture"));
                Assert.That(result.Errors, Does.Contain("catalog.wrong-role:presentation.enemy.fixture"));
                Assert.That(result.Errors, Does.Contain("catalog.wrong-asset-type:presentation.enemy.fixture"));
                Assert.That(result.Errors, Does.Contain("catalog.unresolved-required:presentation.ui.required"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }
    }
}
