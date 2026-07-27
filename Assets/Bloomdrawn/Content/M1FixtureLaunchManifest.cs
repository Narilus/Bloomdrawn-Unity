using System;

namespace Bloomdrawn.Content
{
    [Serializable]
    public sealed class M1FixtureLaunchManifest
    {
        public int SchemaVersion { get; set; }
        public string LineupId { get; set; }
        public string EncounterId { get; set; }
        public ulong ProfileSeed { get; set; }
        public ulong RunSeed { get; set; }
    }
}
