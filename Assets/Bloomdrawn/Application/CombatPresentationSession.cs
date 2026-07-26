using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Bloomdrawn.Engine.Commands;
using Bloomdrawn.Engine.Combat;

namespace Bloomdrawn.Application
{
    public enum PresentationTokenKind { PhaseEntered, CardPlayed, Damage, ShieldGain, Healing, HpLoss, EnemyAction, EnemyIntentsRegenerated, Victory, Defeat, Fallback }
    public enum PresentationReaction { None, OwnerAcknowledgement, Act, Hit, ShieldGain, Victory, Defeat }

    public sealed class PresentationToken
    {
        public PresentationToken(long eventSequence, PresentationTokenKind kind, string sourceRuntimeId, string targetRuntimeId, PresentationReaction sourceReaction, PresentationReaction targetReaction, IReadOnlyDictionary<string, string> facts)
        {
            EventSequence = eventSequence; Kind = kind; SourceRuntimeId = sourceRuntimeId; TargetRuntimeId = targetRuntimeId;
            SourceReaction = sourceReaction; TargetReaction = targetReaction;
            Facts = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(facts ?? new Dictionary<string, string>(), StringComparer.Ordinal));
        }
        public long EventSequence { get; }
        public PresentationTokenKind Kind { get; }
        public string SourceRuntimeId { get; }
        public string TargetRuntimeId { get; }
        public PresentationReaction SourceReaction { get; }
        public PresentationReaction TargetReaction { get; }
        public IReadOnlyDictionary<string, string> Facts { get; }
    }

    public static class GameEventPresentationTokenMapper
    {
        public static PresentationToken Map(GameEvent gameEvent)
        {
            if (gameEvent == null) throw new ArgumentNullException(nameof(gameEvent));
            switch (gameEvent.Kind)
            {
                case "combat.card-played": return Token(gameEvent, PresentationTokenKind.CardPlayed, PresentationReaction.OwnerAcknowledgement, PresentationReaction.None);
                case "combat.damage-dealt": return Token(gameEvent, PresentationTokenKind.Damage, PresentationReaction.Act, PresentationReaction.Hit);
                case "combat.shield-gained": return Token(gameEvent, PresentationTokenKind.ShieldGain, PresentationReaction.ShieldGain, PresentationReaction.ShieldGain);
                case "combat.healing-applied": return Token(gameEvent, PresentationTokenKind.Healing, PresentationReaction.None, PresentationReaction.None);
                case "combat.hp-loss-applied": return Token(gameEvent, PresentationTokenKind.HpLoss, PresentationReaction.None, PresentationReaction.Hit);
                case "combat.enemy-action-started": return Token(gameEvent, PresentationTokenKind.EnemyAction, PresentationReaction.Act, PresentationReaction.None);
                case "combat.enemy-intents-regenerated": return Token(gameEvent, PresentationTokenKind.EnemyIntentsRegenerated, PresentationReaction.None, PresentationReaction.None);
                case "combat.victory": return Token(gameEvent, PresentationTokenKind.Victory, PresentationReaction.None, PresentationReaction.Victory);
                case "combat.defeat": return Token(gameEvent, PresentationTokenKind.Defeat, PresentationReaction.None, PresentationReaction.Defeat);
                case "combat.phase-entered": return Token(gameEvent, PresentationTokenKind.PhaseEntered, PresentationReaction.None, PresentationReaction.None);
                default: return Token(gameEvent, PresentationTokenKind.Fallback, PresentationReaction.None, PresentationReaction.None);
            }
        }

        private static PresentationToken Token(GameEvent gameEvent, PresentationTokenKind kind, PresentationReaction sourceReaction, PresentationReaction targetReaction)
        {
            return new PresentationToken(gameEvent.Sequence, kind, gameEvent.SourceId, gameEvent.TargetId, sourceReaction, targetReaction, gameEvent.Facts);
        }
    }

    public sealed class CombatActorBindingPlan
    {
        private CombatActorBindingPlan(IReadOnlyList<string> partyRuntimeIds, IReadOnlyList<string> enemyRuntimeIds) { PartyRuntimeIds = partyRuntimeIds; EnemyRuntimeIds = enemyRuntimeIds; }
        public IReadOnlyList<string> PartyRuntimeIds { get; }
        public IReadOnlyList<string> EnemyRuntimeIds { get; }
        public static CombatActorBindingPlan From(CombatState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new CombatActorBindingPlan(state.Setup.Party.Select(member => member.RuntimeId.Value).ToList(), state.Setup.Enemies.Select(enemy => enemy.RuntimeId.Value).ToList());
        }
    }

    public sealed class CombatSessionSubmission
    {
        internal CombatSessionSubmission(bool accepted, CombatState state, IReadOnlyList<GameEvent> events, IReadOnlyList<PresentationToken> tokens, RejectionDiagnostic rejection)
        { IsAccepted = accepted; State = state; Events = events; Tokens = tokens; Rejection = rejection; }
        public bool IsAccepted { get; }
        public CombatState State { get; }
        public IReadOnlyList<GameEvent> Events { get; }
        public IReadOnlyList<PresentationToken> Tokens { get; }
        public RejectionDiagnostic Rejection { get; }
    }

    public sealed class CombatSession
    {
        private readonly Queue<PresentationToken> pendingTokens = new Queue<PresentationToken>();
        private readonly List<GameEvent> eventHistory = new List<GameEvent>();
        private CombatState currentState;

        public CombatSession(CombatState initialState) { currentState = initialState ?? throw new ArgumentNullException(nameof(initialState)); }
        public CombatState CurrentState => currentState;
        public CombatActorBindingPlan ActorBindings => CombatActorBindingPlan.From(currentState);
        public IReadOnlyList<GameEvent> EventHistory => eventHistory;
        public IReadOnlyCollection<PresentationToken> PendingTokens => pendingTokens.ToList().AsReadOnly();
        public bool IsInputLocked => pendingTokens.Count > 0;

        public CombatSessionSubmission Submit(CombatCommand command)
        {
            if (IsInputLocked) return PresentationLocked();
            return Record(CombatStateMachine.Apply(currentState, command));
        }
        public CombatSessionSubmission Submit(PlayCardCommand command)
        {
            if (IsInputLocked) return PresentationLocked();
            return Record(CardPlayRules.Apply(currentState, command));
        }
        public bool CompletePresentation(long eventSequence)
        {
            if (pendingTokens.Count == 0 || pendingTokens.Peek().EventSequence != eventSequence) return false;
            pendingTokens.Dequeue();
            return true;
        }
        public bool TryPeekPresentation(out PresentationToken token)
        {
            token = pendingTokens.Count > 0 ? pendingTokens.Peek() : null;
            return token != null;
        }

        private CombatSessionSubmission PresentationLocked()
        {
            return new CombatSessionSubmission(false, currentState, Array.Empty<GameEvent>(), Array.Empty<PresentationToken>(), new RejectionDiagnostic("combat.presentation-locked", "Input is locked while accepted presentation tokens are resolving."));
        }

        private CombatSessionSubmission Record(CommandResult<CombatState> result)
        {
            if (!result.IsAccepted)
                return new CombatSessionSubmission(false, currentState, result.Events, Array.Empty<PresentationToken>(), result.Rejection);

            currentState = result.State;
            eventHistory.AddRange(result.Events);
            var tokens = result.Events.OrderBy(gameEvent => gameEvent.Sequence).Select(GameEventPresentationTokenMapper.Map).ToList();
            foreach (var token in tokens) pendingTokens.Enqueue(token);
            return new CombatSessionSubmission(true, currentState, result.Events, tokens, null);
        }
    }
}
