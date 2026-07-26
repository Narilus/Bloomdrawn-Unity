using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Bloomdrawn.Content.Editor
{
    /// <summary>
    /// Editor/build-only canonical content import. Runtime code receives validated DTOs, never YAML text.
    /// </summary>
    public static class ContentImportService
    {
        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        public static ContentValidationResult ImportDirectory(string directoryPath, ContentOrigin origin)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("A content directory path is required.", nameof(directoryPath));
            }

            var definitions = Directory.GetFiles(directoryPath, "*.yaml", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => Deserializer.Deserialize<ContentDefinition>(File.ReadAllText(path)))
                .ToList();

            return ContentValidator.Validate(definitions, origin);
        }

        public static ContentRegistry CreateRegistry(ContentValidationResult validation)
        {
            if (validation == null)
            {
                throw new ArgumentNullException(nameof(validation));
            }

            if (!validation.IsValid)
            {
                throw new InvalidOperationException("Cannot create a runtime registry from invalid content.");
            }

            return ContentRegistry.Create(validation.Content);
        }

        public static void WriteGeneratedRegistry(ContentRegistry registry, string outputPath)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            File.WriteAllText(outputPath, JsonConvert.SerializeObject(registry.OrderedDefinitions, Formatting.Indented));
        }

        public static IReadOnlyList<ContentDefinition> ReadGeneratedRegistry(string inputPath)
        {
            var definitions = JsonConvert.DeserializeObject<List<ContentDefinition>>(File.ReadAllText(inputPath));
            return definitions ?? new List<ContentDefinition>();
        }
    }
}
