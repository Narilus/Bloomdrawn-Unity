using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bloomdrawn.Content;
using Bloomdrawn.Engine.Commands;

namespace Bloomdrawn.Engine.Combat
{
    public sealed class VisibleEnemyIntent
    {
        public VisibleEnemyIntent(string kind, int damage) { Kind = kind; Damage = damage; }
        public string Kind { get; } public int Damage { get; }
    }

    public sealed class EnemySlot
    {
        public EnemySlot(int slotIndex, RuntimeEnemyId enemyId, VisibleEnemyIntent intent) { SlotIndex = slotIndex; EnemyId = enemyId; Intent = intent; }
        public int SlotIndex { get; } public RuntimeEnemyId EnemyId { get; } public VisibleEnemyIntent Intent { get; }
    }

    public static class EnemySlots
    {
        public static IReadOnlyList<EnemySlot> Create(CombatSetupResult setup)
        {
            return setup.Enemies.Select((enemy, index) => new EnemySlot(index, enemy.RuntimeId, new VisibleEnemyIntent(enemy.InitialIntent.Kind, enemy.InitialIntent.Damage))).ToList();
        }
    }

    public static class EnemyActionSequence
    {
        public static CommandResult<CombatState> Advance(CombatState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.IsTerminal) return Reject(state, "enemy-action.terminal", "Enemy actions are rejected after combat reaches a terminal phase.");
            if (state.Phase != CombatPhase.EnemyPhaseStart && state.Phase != CombatPhase.EnemyAction) return Reject(state, "enemy-action.illegal-phase", "Enemy action advancement is only legal during the enemy phase.");
            if (state.NextEnemySlotIndex >= state.EnemySlots.Count) return FinishEnemyPhase(state);

            var slot = state.EnemySlots[state.NextEnemySlotIndex];
            var events = new List<GameEvent>
            {
                new GameEvent(state.NextEventSequence, "combat.enemy-action-started", slot.EnemyId.Value, "combat.party", new Dictionary<string, string>
                {
                    { "slotIndex", slot.SlotIndex.ToString(CultureInfo.InvariantCulture) }, { "enemyId", slot.EnemyId.Value },
                    { "intentKind", slot.Intent.Kind }, { "intentDamage", slot.Intent.Damage.ToString(CultureInfo.InvariantCulture) },
                    { "sequenceBoundary", "enemy-action" }
                })
            };
            var actionState = CombatStateMachine.WithCardPlayState(state, state.Deck, state.Mana, state.NextEventSequence + 1, phase: CombatPhase.EnemyAction, nextEnemySlotIndex: state.NextEnemySlotIndex + 1);
            var resolution = CombatEffectResolver.Resolve(actionState, new[] { new CombatAtomicEffect(AtomicEffectKind.DamageParty, slot.Intent.Damage, new RuntimeParticipantId(slot.EnemyId.Value), sourceKind: "enemy-intent") });
            events.AddRange(resolution.Events);
            if (resolution.State.IsTerminal) return CommandResult<CombatState>.Accepted(resolution.State, events);
            var completed = CombatStateMachine.WithCardPlayState(resolution.State, resolution.State.Deck, resolution.State.Mana, resolution.State.NextEventSequence, nextEnemySlotIndex: actionState.NextEnemySlotIndex);
            return completed.NextEnemySlotIndex >= completed.EnemySlots.Count ? AcceptFinish(completed, events) : CommandResult<CombatState>.Accepted(completed, events);
        }

        private static CommandResult<CombatState> FinishEnemyPhase(CombatState state) => AcceptFinish(state, new List<GameEvent>());
        private static CommandResult<CombatState> AcceptFinish(CombatState state, ICollection<GameEvent> events)
        {
            var regenerated = state.EnemySlots.Select(slot => new EnemySlot(slot.SlotIndex, slot.EnemyId, new VisibleEnemyIntent(slot.Intent.Kind, slot.Intent.Damage))).ToList();
            events.Add(new GameEvent(state.NextEventSequence, "combat.enemy-intents-regenerated", facts: new Dictionary<string, string> { { "sequenceBoundary", "enemy-end" }, { "slotCount", regenerated.Count.ToString(CultureInfo.InvariantCulture) } }));
            var end = CombatStateMachine.WithCardPlayState(state, state.Deck, state.Mana, state.NextEventSequence + 1, phase: CombatPhase.EnemyEnd, enemySlots: regenerated, nextEnemySlotIndex: 0);
            return CombatStateMachine.AdvanceFromEnemyEnd(end, events);
        }
        private static CommandResult<CombatState> Reject(CombatState state, string code, string message) => CommandResult<CombatState>.Rejected(state, new RejectionDiagnostic(code, message));
    }
}
