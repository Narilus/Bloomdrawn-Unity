using System;
using System.IO;
using System.Linq;
using Bloomdrawn.Application;
using Bloomdrawn.Content;
using Bloomdrawn.Content.Editor;
using Newtonsoft.Json;
using UnityEditor;

namespace Bloomdrawn.Editor.Tooling
{
    public static class M1RuntimeFixtureArtifactGenerator
    {
        public const string ArtifactPath = "Assets/Bloomdrawn/RuntimeData/Fixtures/M1FixtureRuntimeRegistry.json";

        [MenuItem("Bloomdrawn/Generate M1 Runtime Fixture Artifact")]
        public static void Generate()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            var fixtureDirectory = Path.Combine(projectRoot, "GameContent", "fixtures");
            var validation = ContentImportService.ImportDirectory(fixtureDirectory, ContentOrigin.Fixture);
            if (!validation.IsValid) throw new InvalidOperationException("Cannot generate M1 runtime artifact from invalid fixture content.");
            var registry = ContentRegistry.Create(validation.Content);
            var artifact = new M1FixtureRuntimeArtifact
            {
                Origin = ContentOrigin.Fixture.ToString(),
                ContentHash = registry.ContentHash,
                Definitions = registry.OrderedDefinitions.ToList(),
                Launch = ContentImportService.ReadM1FixtureLaunchManifest(fixtureDirectory)
            };
            var absolutePath = Path.Combine(projectRoot, ArtifactPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllText(absolutePath, JsonConvert.SerializeObject(artifact, Formatting.Indented));
            AssetDatabase.ImportAsset(ArtifactPath, ImportAssetOptions.ForceUpdate);
        }
    }
}
