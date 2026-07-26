using System;
using System.IO;
using Bloomdrawn.Engine.Rng;

namespace Bloomdrawn.Application.Persistence
{
    public sealed class ProfileSavePayload
    {
        public string ProfileId { get; set; }
    }

    public sealed class RunSavePayload
    {
        public string RunId { get; set; }
        public RngState RngState { get; set; }
    }

    public interface IProfileRepository
    {
        SaveLoadResult<ProfileSavePayload> LoadProfile();
        void SaveProfile(ProfileSavePayload profile);
    }

    public interface IRunRepository
    {
        SaveLoadResult<RunSavePayload> LoadRun();
        void SaveRun(RunSavePayload run);
    }

    public sealed class InMemorySaveRepository : IProfileRepository, IRunRepository
    {
        private readonly SaveMetadata metadata;
        private SaveEnvelope<ProfileSavePayload> profileEnvelope;
        private SaveEnvelope<RunSavePayload> runEnvelope;

        public InMemorySaveRepository(SaveMetadata metadata)
        {
            this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }

        public SaveLoadResult<ProfileSavePayload> LoadProfile()
        {
            return SaveEnvelopeCodec.Validate(profileEnvelope, metadata);
        }

        public void SaveProfile(ProfileSavePayload profile)
        {
            profileEnvelope = SaveEnvelopeCodec.Create(metadata, profile);
        }

        public SaveLoadResult<RunSavePayload> LoadRun()
        {
            return SaveEnvelopeCodec.Validate(runEnvelope, metadata);
        }

        public void SaveRun(RunSavePayload run)
        {
            runEnvelope = SaveEnvelopeCodec.Create(metadata, run);
        }
    }

    public interface ISavePathProvider
    {
        string RootPath { get; }
    }

    public sealed class FixedSavePathProvider : ISavePathProvider
    {
        public FixedSavePathProvider(string rootPath)
        {
            RootPath = rootPath;
        }

        public string RootPath { get; }
    }

    public sealed class LocalFileProfileRepository : IProfileRepository
    {
        private readonly LocalFileSaveStore<ProfileSavePayload> store;

        public LocalFileProfileRepository(ISavePathProvider pathProvider, SaveMetadata metadata)
        {
            store = new LocalFileSaveStore<ProfileSavePayload>(pathProvider, metadata, "profile.json");
        }

        public SaveLoadResult<ProfileSavePayload> LoadProfile()
        {
            return store.Load();
        }

        public void SaveProfile(ProfileSavePayload profile)
        {
            store.Save(profile);
        }
    }

    public sealed class LocalFileRunRepository : IRunRepository
    {
        private readonly LocalFileSaveStore<RunSavePayload> store;

        public LocalFileRunRepository(ISavePathProvider pathProvider, SaveMetadata metadata)
        {
            store = new LocalFileSaveStore<RunSavePayload>(pathProvider, metadata, "run.json");
        }

        public SaveLoadResult<RunSavePayload> LoadRun()
        {
            return store.Load();
        }

        public void SaveRun(RunSavePayload run)
        {
            store.Save(run);
        }
    }

    internal sealed class LocalFileSaveStore<TPayload>
    {
        private readonly SaveMetadata metadata;
        private readonly string primaryPath;
        private readonly string previousPath;
        private readonly string temporaryPath;

        public LocalFileSaveStore(ISavePathProvider pathProvider, SaveMetadata metadata, string fileName)
        {
            if (pathProvider == null)
            {
                throw new ArgumentNullException(nameof(pathProvider));
            }

            if (string.IsNullOrWhiteSpace(pathProvider.RootPath))
            {
                throw new ArgumentException("A save root path is required.", nameof(pathProvider));
            }

            this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            primaryPath = Path.Combine(pathProvider.RootPath, fileName);
            previousPath = primaryPath + ".previous";
            temporaryPath = primaryPath + ".tmp";
        }

        public SaveLoadResult<TPayload> Load()
        {
            var primary = LoadFile(primaryPath);
            if (primary.IsValid)
            {
                return primary;
            }

            var previous = LoadFile(previousPath);
            return previous.IsValid ? previous : primary;
        }

        public void Save(TPayload payload)
        {
            var directory = Path.GetDirectoryName(primaryPath);
            Directory.CreateDirectory(directory);
            File.WriteAllText(temporaryPath, SaveEnvelopeCodec.Serialize(SaveEnvelopeCodec.Create(metadata, payload)));

            if (File.Exists(primaryPath))
            {
                File.Replace(temporaryPath, primaryPath, previousPath, true);
                return;
            }

            File.Move(temporaryPath, primaryPath);
        }

        private SaveLoadResult<TPayload> LoadFile(string path)
        {
            if (!File.Exists(path))
            {
                return SaveLoadResult<TPayload>.Invalid("save.not-found");
            }

            try
            {
                return SaveEnvelopeCodec.Validate(SaveEnvelopeCodec.Deserialize<TPayload>(File.ReadAllText(path)), metadata);
            }
            catch (Exception)
            {
                return SaveLoadResult<TPayload>.Invalid("save.invalid-json");
            }
        }
    }
}
