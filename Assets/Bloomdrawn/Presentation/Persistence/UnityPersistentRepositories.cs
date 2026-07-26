using Bloomdrawn.Application.Persistence;
using UnityEngine;

namespace Bloomdrawn.Presentation.Persistence
{
    public sealed class UnityPersistentDataPathProvider : ISavePathProvider
    {
        public string RootPath => UnityEngine.Application.persistentDataPath;
    }

    public static class UnityPersistentRepositories
    {
        public static IProfileRepository CreateProfileRepository(SaveMetadata metadata)
        {
            return new LocalFileProfileRepository(new UnityPersistentDataPathProvider(), metadata);
        }

        public static IRunRepository CreateRunRepository(SaveMetadata metadata)
        {
            return new LocalFileRunRepository(new UnityPersistentDataPathProvider(), metadata);
        }
    }
}
