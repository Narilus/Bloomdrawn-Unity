using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bloomdrawn.Application.Persistence
{
    public sealed class SaveMetadata
    {
        public SaveMetadata(int saveSchemaVersion, string engineVersion, string contentVersion)
        {
            SaveSchemaVersion = saveSchemaVersion;
            EngineVersion = engineVersion;
            ContentVersion = contentVersion;
        }

        public int SaveSchemaVersion { get; }
        public string EngineVersion { get; }
        public string ContentVersion { get; }
    }

    public sealed class SaveEnvelope<TPayload>
    {
        [JsonProperty("saveSchemaVersion")]
        public int SaveSchemaVersion { get; set; }

        [JsonProperty("engineVersion")]
        public string EngineVersion { get; set; }

        [JsonProperty("contentVersion")]
        public string ContentVersion { get; set; }

        [JsonProperty("checksum")]
        public string Checksum { get; set; }

        [JsonProperty("payload")]
        public TPayload Payload { get; set; }
    }

    public sealed class SaveLoadResult<TPayload>
    {
        private SaveLoadResult(bool isValid, TPayload payload, string diagnosticCode)
        {
            IsValid = isValid;
            Payload = payload;
            DiagnosticCode = diagnosticCode;
        }

        public bool IsValid { get; }
        public TPayload Payload { get; }
        public string DiagnosticCode { get; }

        public static SaveLoadResult<TPayload> Valid(TPayload payload)
        {
            return new SaveLoadResult<TPayload>(true, payload, null);
        }

        public static SaveLoadResult<TPayload> Invalid(string diagnosticCode)
        {
            return new SaveLoadResult<TPayload>(false, default(TPayload), diagnosticCode);
        }
    }

    public static class SaveEnvelopeCodec
    {
        public static SaveEnvelope<TPayload> Create<TPayload>(SaveMetadata metadata, TPayload payload)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            var envelope = new SaveEnvelope<TPayload>
            {
                SaveSchemaVersion = metadata.SaveSchemaVersion,
                EngineVersion = metadata.EngineVersion,
                ContentVersion = metadata.ContentVersion,
                Payload = payload
            };
            envelope.Checksum = ComputeChecksum(envelope);
            return envelope;
        }

        public static SaveLoadResult<TPayload> Validate<TPayload>(SaveEnvelope<TPayload> envelope, SaveMetadata expectedMetadata)
        {
            if (envelope == null)
            {
                return SaveLoadResult<TPayload>.Invalid("save.missing-envelope");
            }

            if (expectedMetadata == null)
            {
                throw new ArgumentNullException(nameof(expectedMetadata));
            }

            if (envelope.SaveSchemaVersion != expectedMetadata.SaveSchemaVersion)
            {
                return SaveLoadResult<TPayload>.Invalid("save.incompatible-schema");
            }

            if (!string.Equals(envelope.EngineVersion, expectedMetadata.EngineVersion, StringComparison.Ordinal))
            {
                return SaveLoadResult<TPayload>.Invalid("save.incompatible-engine");
            }

            if (!string.Equals(envelope.ContentVersion, expectedMetadata.ContentVersion, StringComparison.Ordinal))
            {
                return SaveLoadResult<TPayload>.Invalid("save.incompatible-content");
            }

            if (!string.Equals(envelope.Checksum, ComputeChecksum(envelope), StringComparison.Ordinal))
            {
                return SaveLoadResult<TPayload>.Invalid("save.invalid-checksum");
            }

            return SaveLoadResult<TPayload>.Valid(envelope.Payload);
        }

        public static string Serialize<TPayload>(SaveEnvelope<TPayload> envelope)
        {
            return JsonConvert.SerializeObject(envelope, Formatting.None);
        }

        public static SaveEnvelope<TPayload> Deserialize<TPayload>(string json)
        {
            return JsonConvert.DeserializeObject<SaveEnvelope<TPayload>>(json);
        }

        private static string ComputeChecksum<TPayload>(SaveEnvelope<TPayload> envelope)
        {
            var canonical = new JObject
            {
                { "saveSchemaVersion", envelope.SaveSchemaVersion },
                { "engineVersion", envelope.EngineVersion ?? string.Empty },
                { "contentVersion", envelope.ContentVersion ?? string.Empty },
                { "payload", ReferenceEquals(envelope.Payload, null) ? JValue.CreateNull() : JToken.FromObject(envelope.Payload) }
            }.ToString(Formatting.None);
            using (var hash = SHA256.Create())
            {
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
