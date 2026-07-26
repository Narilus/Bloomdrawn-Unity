using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Bloomdrawn.Editor.Build
{
    /// <summary>
    /// Provides the narrow Windows bootstrap build invoked by Tools/build-smoke.ps1.
    /// </summary>
    public static class BloomdrawnBuild
    {
        public static void PerformWindowsSmokeBuild()
        {
            var enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (enabledScenes.Length == 0)
            {
                throw new BuildFailedException("The Windows smoke build requires at least one enabled build scene.");
            }

            var outputPath = GetCommandLineArgument("-buildOutput");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.Combine("Builds", "Smoke", "Bloomdrawn.exe");
            }

            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = enabledScenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | BuildOptions.StrictMode
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    string.Format("Windows smoke build failed with result {0}.", report.summary.result));
            }
        }

        private static string GetCommandLineArgument(string name)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.Ordinal))
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }
    }
}
