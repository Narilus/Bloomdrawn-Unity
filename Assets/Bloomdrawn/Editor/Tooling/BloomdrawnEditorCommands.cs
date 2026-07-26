using System;
using System.IO;
using System.Linq;
using Bloomdrawn.Application;
using Bloomdrawn.Content;
using Bloomdrawn.Content.Editor;
using Bloomdrawn.Engine.Combat;
using Bloomdrawn.Presentation;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bloomdrawn.Editor.Tooling
{
    public sealed class BloomHealthResult
    {
        public string ProjectPath { get; set; }
        public string EditorVersion { get; set; }
        public bool PipelineReady { get; set; }
        public bool EditorReady { get; set; }
        public bool CompilationActive { get; set; }
        public bool CompileFailed { get; set; }
        public bool CompileSucceeded { get; set; }
        public bool RegistryValid { get; set; }
        public int DefinitionCount { get; set; }
    }
    public sealed class ContentValidationSummary { public bool Valid { get; set; } public int DefinitionCount { get; set; } }
    public sealed class BloomSceneSummary { public string Name { get; set; } public string Path { get; set; } public int RootObjectCount { get; set; } public bool IsDirty { get; set; } }
    public sealed class CombatFixtureCommandSummary { public bool Loaded { get; set; } public string Phase { get; set; } public int PartyCount { get; set; } public int EnemyCount { get; set; } public bool InputLocked { get; set; } public int PendingTokenCount { get; set; } public string CanonicalState { get; set; } }
    public sealed class CombatLayoutValidationSummary { public bool Valid { get; set; } public int PartyActorCount { get; set; } public int EnemyActorCount { get; set; } public int IndependentActorCount { get; set; } }

    public static class BloomdrawnEditorCommands
    {
        private static CombatSession combatFixtureSession;
        [CliCommand("bloom.health", "Report Bloomdrawn project, Editor, compilation, and fixture-registry health.")]
        public static BloomHealthResult Health()
        {
            var validation = ValidateFixtureContent();
            var compilationActive = EditorApplication.isCompiling;
            var compileFailed = EditorUtility.scriptCompilationFailed;
            return new BloomHealthResult
            {
                ProjectPath = Directory.GetParent(UnityEngine.Application.dataPath).FullName,
                EditorVersion = UnityEngine.Application.unityVersion,
                PipelineReady = true,
                EditorReady = !compilationActive && !EditorApplication.isUpdating,
                CompilationActive = compilationActive,
                CompileFailed = compileFailed,
                CompileSucceeded = !compilationActive && !compileFailed,
                RegistryValid = validation.IsValid,
                DefinitionCount = validation.IsValid ? validation.Content.Definitions.Count : 0
            };
        }

        [CliCommand("bloom.validate-content", "Validate the canonical Bloomdrawn fixture content and return concise status.")]
        public static ContentValidationSummary ValidateContent([CliArg("directory", "Optional content directory to validate; defaults to canonical fixture content.")] string directory = null)
        {
            var validation = ValidateContentDirectory(directory ?? FixtureDirectory());
            if (!validation.IsValid) throw new InvalidOperationException("Fixture content validation failed: " + validation.Errors.Count + " error(s).");
            return new ContentValidationSummary { Valid = true, DefinitionCount = validation.Content.Definitions.Count };
        }

        [CliCommand("bloom.scene-summary", "Report the active scene name, path, and root-object count without gameplay mutation.")]
        public static BloomSceneSummary SceneSummary()
        {
            var scene = SceneManager.GetActiveScene();
            return new BloomSceneSummary { Name = scene.name, Path = scene.path, RootObjectCount = scene.rootCount, IsDirty = scene.isDirty };
        }

        [CliCommand("bloom.load-combat-fixture", "Load a registry-derived M1 fixture combat session without changing gameplay state.")]
        public static CombatFixtureCommandSummary LoadCombatFixture()
        {
            combatFixtureSession = new CombatSession(CombatStateMachine.Create(CreateFixtureSetup()));
            return CombatFixtureSummary(combatFixtureSession);
        }

        [CliCommand("bloom.reset-combat-fixture", "Reset the loaded M1 fixture session to registry-derived initial combat state.")]
        public static CombatFixtureCommandSummary ResetCombatFixture()
        {
            RequireFixtureSession();
            combatFixtureSession = new CombatSession(CombatStateMachine.Create(CreateFixtureSetup()));
            return CombatFixtureSummary(combatFixtureSession);
        }

        [CliCommand("bloom.dump-combat-state", "Report the loaded fixture combat state without applying a command or presentation token.")]
        public static CombatFixtureCommandSummary DumpCombatState() => CombatFixtureSummary(RequireFixtureSession());

        [CliCommand("bloom.validate-combat-layout", "Validate independent CombatStage actors against the loaded fixture session's stable runtime IDs.")]
        public static CombatLayoutValidationSummary ValidateCombatLayout()
        {
            var session = RequireFixtureSession();
            const string combatStagePath = "Assets/Scenes/CombatStage.unity";
            if (!File.Exists(Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, combatStagePath))) throw new InvalidOperationException("CombatStage scene asset is unavailable for layout validation.");
            EditorSceneManager.OpenScene(combatStagePath, OpenSceneMode.Single);
            var stage = UnityEngine.Object.FindFirstObjectByType<CombatPresentationController>();
            if (stage == null) throw new InvalidOperationException("CombatStage does not contain its presentation controller.");
            stage.BindSession(session);
            var all = UnityEngine.Object.FindObjectsByType<CombatActorView>(FindObjectsSortMode.None);
            var bindings = session.ActorBindings;
            var expected = bindings.PartyRuntimeIds.Concat(bindings.EnemyRuntimeIds).ToList();
            if (all.Length != expected.Count || all.Select(actor => actor.RuntimeId).Distinct(StringComparer.Ordinal).Count() != expected.Count || expected.Any(id => all.All(actor => actor.RuntimeId != id)))
                throw new InvalidOperationException("CombatStage actor runtime IDs do not match the loaded fixture setup.");
            return new CombatLayoutValidationSummary { Valid = true, PartyActorCount = bindings.PartyRuntimeIds.Count, EnemyActorCount = bindings.EnemyRuntimeIds.Count, IndependentActorCount = all.Length };
        }

        public static Bloomdrawn.Content.ContentValidationResult ValidateFixtureContent()
        {
            return ValidateContentDirectory(FixtureDirectory());
        }

        private static CombatSetupResult CreateFixtureSetup()
        {
            var validation = ValidateFixtureContent();
            if (!validation.IsValid) throw new InvalidOperationException("Fixture content validation failed: " + validation.Errors.Count + " error(s).");
            return FixtureCombatSetupFactory.Create(FixtureCombatCatalog.Create(validation.Content), new CombatSetupRequest("fixture.m1.lineup.quartet", "fixture.m1.encounter.training"));
        }
        private static CombatSession RequireFixtureSession()
        {
            if (combatFixtureSession == null) throw new InvalidOperationException("No fixture combat session is loaded. Run bloom.load-combat-fixture first.");
            return combatFixtureSession;
        }
        private static CombatFixtureCommandSummary CombatFixtureSummary(CombatSession session)
        {
            return new CombatFixtureCommandSummary { Loaded = true, Phase = session.CurrentState.Phase.ToString(), PartyCount = session.CurrentState.Setup.Party.Count, EnemyCount = session.CurrentState.Setup.Enemies.Count, InputLocked = session.IsInputLocked, PendingTokenCount = session.PendingTokens.Count, CanonicalState = session.CurrentState.CanonicalForm() };
        }

        public static Bloomdrawn.Content.ContentValidationResult ValidateContentDirectory(string directory) => ContentImportService.ImportDirectory(directory, Bloomdrawn.Content.ContentOrigin.Fixture);
        private static string FixtureDirectory() => Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, "GameContent", "fixtures");
    }
}
