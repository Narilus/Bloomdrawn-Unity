using System;
using System.IO;
using Bloomdrawn.Application.Persistence;
using Bloomdrawn.Engine.Rng;
using NUnit.Framework;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class SaveRepositoryTests
    {
        private static readonly SaveMetadata Metadata = new SaveMetadata(1, "m0-engine", "m0-content");

        [Test]
        public void Envelope_ValidatesAndRejectsChecksumAndIncompatibleSchema()
        {
            var envelope = SaveEnvelopeCodec.Create(Metadata, new ProfileSavePayload { ProfileId = "fixture-profile" });

            Assert.That(SaveEnvelopeCodec.Validate(envelope, Metadata).IsValid, Is.True);
            StringAssert.Contains("\"saveSchemaVersion\"", SaveEnvelopeCodec.Serialize(envelope));

            envelope.Checksum = "invalid";
            Assert.That(SaveEnvelopeCodec.Validate(envelope, Metadata).DiagnosticCode, Is.EqualTo("save.invalid-checksum"));

            envelope = SaveEnvelopeCodec.Create(Metadata, new ProfileSavePayload { ProfileId = "fixture-profile" });
            envelope.SaveSchemaVersion = 2;
            var incompatible = SaveEnvelopeCodec.Validate(envelope, Metadata);
            Assert.That(incompatible.IsValid, Is.False);
            Assert.That(incompatible.DiagnosticCode, Is.EqualTo("save.incompatible-schema"));
            Assert.That(incompatible.Payload, Is.Null);
        }

        [Test]
        public void InMemoryRepository_SavesAndLoadsProfileAndRunPayloads()
        {
            var repository = new InMemorySaveRepository(Metadata);
            repository.SaveProfile(new ProfileSavePayload { ProfileId = "fixture-profile" });
            repository.SaveRun(new RunSavePayload { RunId = "fixture-run", RngState = new RngState(42UL) });

            var profile = repository.LoadProfile();
            var run = repository.LoadRun();

            Assert.That(profile.IsValid, Is.True);
            Assert.That(profile.Payload.ProfileId, Is.EqualTo("fixture-profile"));
            Assert.That(run.IsValid, Is.True);
            Assert.That(run.Payload.RunId, Is.EqualTo("fixture-run"));
            Assert.That(run.Payload.RngState.State, Is.EqualTo(42UL));
        }

        [Test]
        public void LocalRepository_RoundtripsPayloadAndContinuesRngState()
        {
            var root = CreateTemporaryRoot();
            try
            {
                var repository = new LocalFileRunRepository(new FixedSavePathProvider(root), Metadata);
                var original = new RngState(987UL);
                DeterministicRng.NextUInt64(original);
                repository.SaveRun(new RunSavePayload { RunId = "fixture-run", RngState = original });

                var loaded = repository.LoadRun();

                Assert.That(loaded.IsValid, Is.True);
                Assert.That(loaded.Payload.RunId, Is.EqualTo("fixture-run"));
                Assert.That(DeterministicRng.NextUInt64(loaded.Payload.RngState), Is.EqualTo(DeterministicRng.NextUInt64(original)));
            }
            finally
            {
                DeleteTemporaryRoot(root);
            }
        }

        [Test]
        public void LocalRepository_InvalidPrimaryRecoversPreviousValidSnapshot()
        {
            var root = CreateTemporaryRoot();
            try
            {
                var repository = new LocalFileProfileRepository(new FixedSavePathProvider(root), Metadata);
                repository.SaveProfile(new ProfileSavePayload { ProfileId = "first" });
                repository.SaveProfile(new ProfileSavePayload { ProfileId = "second" });
                File.WriteAllText(Path.Combine(root, "profile.json"), "interrupted replacement");

                var recovered = repository.LoadProfile();

                Assert.That(recovered.IsValid, Is.True);
                Assert.That(recovered.Payload.ProfileId, Is.EqualTo("first"));
            }
            finally
            {
                DeleteTemporaryRoot(root);
            }
        }

        private static string CreateTemporaryRoot()
        {
            var root = Path.Combine(Path.GetTempPath(), "BloomdrawnSaveTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }

        private static void DeleteTemporaryRoot(string root)
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
