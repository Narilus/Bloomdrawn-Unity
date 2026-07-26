using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bloomdrawn.Content;
using Bloomdrawn.Engine.Commands;

namespace Bloomdrawn.Engine.Combat
{
    public sealed class PartyCombatValues
    {
        public PartyCombatValues(int maximumHp, int currentHp, int shield) { MaximumHp = maximumHp; CurrentHp = currentHp; Shield = shield; }
        public int MaximumHp { get; } public int CurrentHp { get; } public int Shield { get; }
    }

    public sealed class EnemyCombatValues
    {
        public EnemyCombatValues(RuntimeEnemyId runtimeId, int maximumHp, int currentHp, int shield) { RuntimeId = runtimeId; MaximumHp = maximumHp; CurrentHp = currentHp; Shield = shield; }
        public RuntimeEnemyId RuntimeId { get; } public int MaximumHp { get; } public int CurrentHp { get; } public int Shield { get; }
    }

    public sealed class CombatValues
    {
        public CombatValues(PartyCombatValues party, IReadOnlyList<EnemyCombatValues> enemies) { Party = party; Enemies = enemies; }
        public PartyCombatValues Party { get; } public IReadOnlyList<EnemyCombatValues> Enemies { get; }
        public static CombatValues Create(CombatSetupResult setup)
        {
            var partyMaximum = setup.Party.Sum(member => member.MaxHp);
            return new CombatValues(new PartyCombatValues(partyMaximum, partyMaximum, 0), setup.Enemies.Select(enemy => new EnemyCombatValues(enemy.RuntimeId, enemy.MaxHp, enemy.MaxHp, 0)).ToList());
        }
        public string CanonicalForm() => Party.MaximumHp.ToString(CultureInfo.InvariantCulture) + "/" + Party.CurrentHp.ToString(CultureInfo.InvariantCulture) + "/" + Party.Shield.ToString(CultureInfo.InvariantCulture) + ";" + string.Join(",", Enemies.Select(enemy => enemy.RuntimeId.Value + ":" + enemy.CurrentHp.ToString(CultureInfo.InvariantCulture) + "/" + enemy.Shield.ToString(CultureInfo.InvariantCulture)));
    }

    public enum AtomicEffectKind { DamageEnemy, DamageParty, GainPartyShield, HealParty, LosePartyHp }

    public sealed class CombatAtomicEffect
    {
        public CombatAtomicEffect(AtomicEffectKind kind, int amount, RuntimeParticipantId ownerId, RuntimeEnemyId? enemyId = null, string sourceKind = "command") { Kind = kind; Amount = amount; OwnerId = ownerId; EnemyId = enemyId; SourceKind = sourceKind; }
        public AtomicEffectKind Kind { get; } public int Amount { get; } public RuntimeParticipantId OwnerId { get; } public RuntimeEnemyId? EnemyId { get; } public string SourceKind { get; }
    }

    public sealed class CombatResolution
    {
        public CombatResolution(CombatState state, IReadOnlyList<GameEvent> events) { State = state; Events = events; }
        public CombatState State { get; } public IReadOnlyList<GameEvent> Events { get; }
    }

    public static class CombatEffectResolver
    {
        public static CombatResolution ResolveCard(CombatState state, CardInstance card, RuntimeEnemyId? target)
        {
            var owner = state.Setup.Party.Single(member => member.RuntimeId.Equals(card.OwnerId));
            if (string.Equals(card.OperationKind, "strike", StringComparison.Ordinal)) return Resolve(state, new[] { new CombatAtomicEffect(AtomicEffectKind.DamageEnemy, owner.Attack, card.OwnerId, target, "card") });
            if (string.Equals(card.OperationKind, "shield", StringComparison.Ordinal)) return Resolve(state, new[] { new CombatAtomicEffect(AtomicEffectKind.GainPartyShield, owner.Defense, card.OwnerId, null, "card") });
            throw new InvalidOperationException("M1 fixture card has no supported operation.");
        }

        public static CombatResolution Resolve(CombatState initial, IReadOnlyList<CombatAtomicEffect> effects)
        {
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            if (effects == null) throw new ArgumentNullException(nameof(effects));
            var state = initial; var events = new List<GameEvent>();
            foreach (var effect in effects)
            {
                if (state.IsTerminal) break;
                state = ApplyAtomic(state, effect, events);
                state = ApplyTerminalCheck(state, events);
            }
            return new CombatResolution(state, events);
        }

        private static CombatState ApplyAtomic(CombatState state, CombatAtomicEffect effect, ICollection<GameEvent> events)
        {
            if (effect.Amount < 0) throw new ArgumentOutOfRangeException(nameof(effect));
            var party = state.Values.Party; var enemies = state.Values.Enemies.ToList();
            var shieldAbsorbed = 0; var hpDamage = 0; var targetId = string.Empty; var eventKind = string.Empty;
            switch (effect.Kind)
            {
                case AtomicEffectKind.DamageEnemy:
                    if (!effect.EnemyId.HasValue) throw new InvalidOperationException("Enemy damage requires a stable enemy target.");
                    var enemyIndex = enemies.FindIndex(enemy => enemy.RuntimeId.Equals(effect.EnemyId.Value));
                    if (enemyIndex < 0) throw new InvalidOperationException("Enemy damage target is not part of this encounter.");
                    var enemy = enemies[enemyIndex]; shieldAbsorbed = Math.Min(enemy.Shield, effect.Amount); hpDamage = Math.Min(enemy.CurrentHp, effect.Amount - shieldAbsorbed);
                    enemies[enemyIndex] = new EnemyCombatValues(enemy.RuntimeId, enemy.MaximumHp, enemy.CurrentHp - hpDamage, enemy.Shield - shieldAbsorbed); targetId = enemy.RuntimeId.Value; eventKind = "combat.damage-dealt"; break;
                case AtomicEffectKind.DamageParty:
                    shieldAbsorbed = Math.Min(party.Shield, effect.Amount); hpDamage = Math.Min(party.CurrentHp, effect.Amount - shieldAbsorbed);
                    party = new PartyCombatValues(party.MaximumHp, party.CurrentHp - hpDamage, party.Shield - shieldAbsorbed); targetId = "combat.party"; eventKind = "combat.damage-dealt"; break;
                case AtomicEffectKind.GainPartyShield:
                    party = new PartyCombatValues(party.MaximumHp, party.CurrentHp, party.Shield + effect.Amount); targetId = "combat.party"; eventKind = "combat.shield-gained"; break;
                case AtomicEffectKind.HealParty:
                    party = new PartyCombatValues(party.MaximumHp, Math.Min(party.MaximumHp, party.CurrentHp + effect.Amount), party.Shield); targetId = "combat.party"; eventKind = "combat.healing-applied"; break;
                case AtomicEffectKind.LosePartyHp:
                    hpDamage = Math.Min(party.CurrentHp, effect.Amount); party = new PartyCombatValues(party.MaximumHp, party.CurrentHp - hpDamage, party.Shield); targetId = "combat.party"; eventKind = "combat.hp-loss-applied"; break;
                default: throw new InvalidOperationException("Unknown atomic effect kind.");
            }
            var values = new CombatValues(party, enemies);
            events.Add(new GameEvent(state.NextEventSequence, eventKind, effect.OwnerId.Value, targetId, new Dictionary<string, string> { { "sourceKind", effect.SourceKind }, { "ownerId", effect.OwnerId.Value }, { "shieldAbsorbed", shieldAbsorbed.ToString(CultureInfo.InvariantCulture) }, { "hpDamageDealt", hpDamage.ToString(CultureInfo.InvariantCulture) }, { "affectedId", targetId } }));
            return CombatStateMachine.WithCardPlayState(state, state.Deck, state.Mana, state.NextEventSequence + 1, values);
        }

        private static CombatState ApplyTerminalCheck(CombatState state, ICollection<GameEvent> events)
        {
            var terminal = state.Values.Party.CurrentHp <= 0 ? CombatPhase.Defeat : state.Values.Enemies.All(enemy => enemy.CurrentHp <= 0) ? CombatPhase.Victory : (CombatPhase?)null;
            if (!terminal.HasValue) return state;
            events.Add(new GameEvent(state.NextEventSequence, terminal == CombatPhase.Defeat ? "combat.defeat" : "combat.victory"));
            return CombatStateMachine.WithCardPlayState(state, state.Deck, state.Mana, state.NextEventSequence + 1, state.Values, terminal);
        }
    }
}
