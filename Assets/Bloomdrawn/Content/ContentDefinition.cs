using System;
using System.Collections.Generic;

namespace Bloomdrawn.Content
{
    public enum ContentKind
    {
        Character,
        Card,
        Enemy,
        Encounter,
        FixtureLineup
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

        public bool RequiresPresentationBindingForCurrentMilestone { get; set; }

        public string OwnerId { get; set; }

        public List<string> EnemyIds { get; set; } = new List<string>();

        public int? MaxHp { get; set; }

        public int? Attack { get; set; }

        public int? Defense { get; set; }

        public int? PrintedCost { get; set; }

        public string TargetKind { get; set; }

        public string OperationKind { get; set; }

        public List<string> CharacterIds { get; set; } = new List<string>();

        public List<string> DeckRecipe { get; set; } = new List<string>();

        public string InitialIntentKind { get; set; }

        public int? InitialIntentDamage { get; set; }
    }

    public enum ContentOrigin
    {
        Production,
        Fixture
    }
}
