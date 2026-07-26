using System;
using System.Collections.Generic;
using System.Linq;

namespace Bloomdrawn.Content
{
    public readonly struct RuntimeParticipantId : IEquatable<RuntimeParticipantId>
    {
        public RuntimeParticipantId(string value) { Value = value ?? throw new ArgumentNullException(nameof(value)); }
        public string Value { get; }
        public bool Equals(RuntimeParticipantId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is RuntimeParticipantId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public readonly struct RuntimeEnemyId : IEquatable<RuntimeEnemyId>
    {
        public RuntimeEnemyId(string value) { Value = value ?? throw new ArgumentNullException(nameof(value)); }
        public string Value { get; }
        public bool Equals(RuntimeEnemyId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is RuntimeEnemyId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public sealed class CombatSetupRequest
    {
        public CombatSetupRequest(string lineupId, string encounterId)
        {
            LineupId = lineupId ?? throw new ArgumentNullException(nameof(lineupId));
            EncounterId = encounterId ?? throw new ArgumentNullException(nameof(encounterId));
        }

        public string LineupId { get; }
        public string EncounterId { get; }
    }

    public sealed class FixtureCombatCatalog
    {
        private FixtureCombatCatalog(ContentRegistry registry) { Registry = registry; }
        public ContentRegistry Registry { get; }

        public static FixtureCombatCatalog Create(ValidatedContent validatedContent)
        {
            if (validatedContent == null) throw new ArgumentNullException(nameof(validatedContent));
            if (validatedContent.Origin != ContentOrigin.Fixture) throw new InvalidOperationException("M1 fixture combat setup requires fixture-origin validated content.");
            return new FixtureCombatCatalog(ContentRegistry.Create(validatedContent));
        }
    }

    public sealed class FixturePartyMember
    {
        public FixturePartyMember(RuntimeParticipantId runtimeId, string definitionId, int maxHp, int attack, int defense)
        {
            RuntimeId = runtimeId;
            DefinitionId = definitionId;
            MaxHp = maxHp;
            Attack = attack;
            Defense = defense;
        }

        public RuntimeParticipantId RuntimeId { get; }
        public string DefinitionId { get; }
        public int MaxHp { get; }
        public int Attack { get; }
        public int Defense { get; }
    }

    public sealed class FixtureDeckRecipeEntry
    {
        public FixtureDeckRecipeEntry(int order, string cardDefinitionId, RuntimeParticipantId ownerId)
        {
            Order = order;
            CardDefinitionId = cardDefinitionId;
            OwnerId = ownerId;
        }

        public int Order { get; }
        public string CardDefinitionId { get; }
        public RuntimeParticipantId OwnerId { get; }
    }

    public sealed class InitialEnemyIntent
    {
        public InitialEnemyIntent(string kind, int damage) { Kind = kind; Damage = damage; }
        public string Kind { get; }
        public int Damage { get; }
    }

    public sealed class FixtureEnemySetup
    {
        public FixtureEnemySetup(RuntimeEnemyId runtimeId, string definitionId, int maxHp, InitialEnemyIntent initialIntent)
        {
            RuntimeId = runtimeId;
            DefinitionId = definitionId;
            MaxHp = maxHp;
            InitialIntent = initialIntent;
        }

        public RuntimeEnemyId RuntimeId { get; }
        public string DefinitionId { get; }
        public int MaxHp { get; }
        public InitialEnemyIntent InitialIntent { get; }
    }

    public sealed class CombatSetupResult
    {
        public CombatSetupResult(string lineupId, string encounterId, IReadOnlyList<FixturePartyMember> party, IReadOnlyList<FixtureDeckRecipeEntry> deckRecipe, IReadOnlyList<FixtureEnemySetup> enemies)
        {
            LineupId = lineupId;
            EncounterId = encounterId;
            Party = party;
            DeckRecipe = deckRecipe;
            Enemies = enemies;
        }

        public string LineupId { get; }
        public string EncounterId { get; }
        public IReadOnlyList<FixturePartyMember> Party { get; }
        public IReadOnlyList<FixtureDeckRecipeEntry> DeckRecipe { get; }
        public IReadOnlyList<FixtureEnemySetup> Enemies { get; }
    }

    public static class FixtureCombatSetupFactory
    {
        public static CombatSetupResult Create(FixtureCombatCatalog catalog, CombatSetupRequest request)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var lineup = Require(catalog.Registry, request.LineupId, ContentKind.FixtureLineup);
            var encounter = Require(catalog.Registry, request.EncounterId, ContentKind.Encounter);
            var party = lineup.CharacterIds.Select((id, index) =>
            {
                var definition = Require(catalog.Registry, id, ContentKind.Character);
                return new FixturePartyMember(new RuntimeParticipantId("combat.party." + lineup.Id + "." + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + definition.Id), definition.Id, definition.MaxHp.Value, definition.Attack.Value, definition.Defense.Value);
            }).ToList();
            var partyByDefinition = party.ToDictionary(member => member.DefinitionId, member => member, StringComparer.Ordinal);
            var deck = lineup.DeckRecipe.Select((id, index) =>
            {
                var definition = Require(catalog.Registry, id, ContentKind.Card);
                return new FixtureDeckRecipeEntry(index, definition.Id, partyByDefinition[definition.OwnerId].RuntimeId);
            }).ToList();
            var enemies = encounter.EnemyIds.Select((id, index) =>
            {
                var definition = Require(catalog.Registry, id, ContentKind.Enemy);
                return new FixtureEnemySetup(new RuntimeEnemyId("combat.enemy." + encounter.Id + "." + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + definition.Id), definition.Id, definition.MaxHp.Value, new InitialEnemyIntent(definition.InitialIntentKind, definition.InitialIntentDamage.Value));
            }).ToList();
            return new CombatSetupResult(lineup.Id, encounter.Id, party, deck, enemies);
        }

        private static ContentDefinition Require(ContentRegistry registry, string id, ContentKind expectedKind)
        {
            if (!registry.TryGet(id, out var definition) || definition.Kind != expectedKind)
                throw new InvalidOperationException("Fixture setup reference '" + id + "' is not a valid " + expectedKind + ".");
            return definition;
        }
    }
}
