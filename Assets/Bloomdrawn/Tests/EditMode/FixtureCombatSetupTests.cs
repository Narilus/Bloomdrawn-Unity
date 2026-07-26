using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomdrawn.Content;
using Bloomdrawn.Content.Editor;
using NUnit.Framework;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class FixtureCombatSetupTests
    {
        [Test]
        public void ValidFixtureContent_ProducesRegistryDerivedExactFourCombatSetup()
        {
            var catalog = FixtureCombatCatalog.Create(ImportFixtures().Content);
            var setup = FixtureCombatSetupFactory.Create(catalog, new CombatSetupRequest("fixture.m1.lineup.quartet", "fixture.m1.encounter.training"));

            Assert.That(setup.Party, Has.Count.EqualTo(4));
            Assert.That(setup.DeckRecipe, Has.Count.EqualTo(8));
            Assert.That(setup.Enemies, Has.Count.EqualTo(1));
            Assert.That(setup.Party.Select(member => member.DefinitionId), Is.EqualTo(new[]
            {
                "fixture.m1.character.alpha",
                "fixture.m1.character.beta",
                "fixture.m1.character.gamma",
                "fixture.m1.character.delta"
            }));
            Assert.That(setup.DeckRecipe.All(entry => setup.Party.Any(member => member.RuntimeId.Equals(entry.OwnerId))), Is.True);
            Assert.That(setup.Enemies[0].InitialIntent.Kind, Is.EqualTo("attack"));
            Assert.That(setup.Enemies[0].InitialIntent.Damage, Is.EqualTo(7));
        }

        [Test]
        public void SameRegistryAndRequest_ProduceStableRuntimeIdsDeckRecipeAndInitialIntent()
        {
            var validation = ImportFixtures();
            var first = FixtureCombatSetupFactory.Create(FixtureCombatCatalog.Create(validation.Content), new CombatSetupRequest("fixture.m1.lineup.quartet", "fixture.m1.encounter.training"));
            var second = FixtureCombatSetupFactory.Create(FixtureCombatCatalog.Create(validation.Content), new CombatSetupRequest("fixture.m1.lineup.quartet", "fixture.m1.encounter.training"));

            Assert.That(second.Party.Select(member => member.RuntimeId.Value), Is.EqualTo(first.Party.Select(member => member.RuntimeId.Value)));
            Assert.That(second.Enemies.Select(enemy => enemy.RuntimeId.Value), Is.EqualTo(first.Enemies.Select(enemy => enemy.RuntimeId.Value)));
            Assert.That(second.DeckRecipe.Select(entry => entry.CardDefinitionId + "|" + entry.OwnerId.Value), Is.EqualTo(first.DeckRecipe.Select(entry => entry.CardDefinitionId + "|" + entry.OwnerId.Value)));
            Assert.That(second.Enemies[0].InitialIntent.Kind + "|" + second.Enemies[0].InitialIntent.Damage, Is.EqualTo(first.Enemies[0].InitialIntent.Kind + "|" + first.Enemies[0].InitialIntent.Damage));
        }

        [Test]
        public void InvalidFixtureReferencesAndLineupRules_ReportDeterministicDiagnostics()
        {
            var definitions = ImportFixtures().Content.Definitions.Select(Clone).ToList();
            definitions.Single(item => item.Id == "fixture.m1.card.alpha-strike").OwnerId = "fixture.m1.character.absent";
            definitions.Single(item => item.Id == "fixture.m1.lineup.quartet").CharacterIds.RemoveAt(0);
            definitions.Single(item => item.Id == "fixture.m1.card.beta-shield").TargetKind = "unknown";

            var result = ContentValidator.Validate(definitions, ContentOrigin.Fixture);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain("content.invalid-reference"));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain("content.fixture-invalid-lineup"));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain("content.fixture-invalid-card"));
        }

        [Test]
        public void FixtureDefinitions_CannotValidateAsProductionContent()
        {
            var definitions = ImportFixtures().Content.Definitions.Select(Clone).ToList();
            var result = ContentValidator.Validate(definitions, ContentOrigin.Production);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain("content.fixture-only"));
        }

        [Test]
        public void ChangingFixtureStat_ChangesRegistryDerivedSetupWithoutEngineOrUiBranch()
        {
            var definitions = ImportFixtures().Content.Definitions.Select(Clone).ToList();
            definitions.Single(item => item.Id == "fixture.m1.character.alpha").Attack = 13;
            var validation = ContentValidator.Validate(definitions, ContentOrigin.Fixture);
            var setup = FixtureCombatSetupFactory.Create(FixtureCombatCatalog.Create(validation.Content), new CombatSetupRequest("fixture.m1.lineup.quartet", "fixture.m1.encounter.training"));

            Assert.That(validation.IsValid, Is.True);
            Assert.That(setup.Party.Single(member => member.DefinitionId == "fixture.m1.character.alpha").Attack, Is.EqualTo(13));
        }

        private static ContentValidationResult ImportFixtures()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            var result = ContentImportService.ImportDirectory(Path.Combine(projectRoot, "GameContent", "fixtures"), ContentOrigin.Fixture);
            Assert.That(result.IsValid, Is.True, string.Join(Environment.NewLine, result.Errors.Select(error => error.Code + ":" + error.Message)));
            return result;
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
                RequiresPresentationBindingForCurrentMilestone = source.RequiresPresentationBindingForCurrentMilestone,
                OwnerId = source.OwnerId,
                EnemyIds = source.EnemyIds == null ? null : source.EnemyIds.ToList(),
                MaxHp = source.MaxHp,
                Attack = source.Attack,
                Defense = source.Defense,
                PrintedCost = source.PrintedCost,
                TargetKind = source.TargetKind,
                OperationKind = source.OperationKind,
                CharacterIds = source.CharacterIds == null ? null : source.CharacterIds.ToList(),
                DeckRecipe = source.DeckRecipe == null ? null : source.DeckRecipe.ToList(),
                InitialIntentKind = source.InitialIntentKind,
                InitialIntentDamage = source.InitialIntentDamage
            };
        }
    }
}
