using System;
using System.IO;
using Bloomdrawn.Content.Editor;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bloomdrawn.Editor.Tooling
{
    public sealed class BloomHealthResult { public string ProjectPath { get; set; } public string EditorVersion { get; set; } public bool IsCompiling { get; set; } public bool RegistryValid { get; set; } public int DefinitionCount { get; set; } }
    public sealed class ContentValidationSummary { public bool Valid { get; set; } public int DefinitionCount { get; set; } }
    public sealed class BloomSceneSummary { public string Name { get; set; } public string Path { get; set; } public int RootObjectCount { get; set; } public bool IsDirty { get; set; } }

    public static class BloomdrawnEditorCommands
    {
        [CliCommand("bloom.health", "Report Bloomdrawn project, Editor, compilation, and fixture-registry health.")]
        public static BloomHealthResult Health()
        {
            var validation = ValidateFixtureContent();
            return new BloomHealthResult { ProjectPath = Directory.GetParent(UnityEngine.Application.dataPath).FullName, EditorVersion = UnityEngine.Application.unityVersion, IsCompiling = EditorApplication.isCompiling, RegistryValid = validation.IsValid, DefinitionCount = validation.IsValid ? validation.Content.Definitions.Count : 0 };
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

        public static Bloomdrawn.Content.ContentValidationResult ValidateFixtureContent()
        {
            return ValidateContentDirectory(FixtureDirectory());
        }

        public static Bloomdrawn.Content.ContentValidationResult ValidateContentDirectory(string directory) => ContentImportService.ImportDirectory(directory, Bloomdrawn.Content.ContentOrigin.Fixture);
        private static string FixtureDirectory() => Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, "GameContent", "fixtures");
    }
}
