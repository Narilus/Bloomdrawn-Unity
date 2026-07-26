using System;
using System.Collections.Generic;

namespace Bloomdrawn.Content
{
    public enum ContentKind
    {
        Character,
        Card,
        Enemy,
        Encounter
    }

    /// <summary>
    /// Versioned, stable-ID content input shared by editor import and runtime registry generation.
    /// </summary>
    public sealed class ContentDefinition
    {
        public string ContentVersion { get; set; }

        public string Id { get; set; }

        public ContentKind Kind { get; set; }

        public string DisplayName { get; set; }

        public string PresentationAssetId { get; set; }

        public string OwnerId { get; set; }

        public List<string> EnemyIds { get; set; } = new List<string>();
    }

    public enum ContentOrigin
    {
        Production,
        Fixture
    }
}
