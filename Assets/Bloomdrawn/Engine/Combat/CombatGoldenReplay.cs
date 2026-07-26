using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bloomdrawn.Content;
using Bloomdrawn.Engine.Commands;
using Bloomdrawn.Engine.Rng;

namespace Bloomdrawn.Engine.Combat
{
    public enum CombatReplayCommandKind { BeginCombat, PlayCard, EndTurn, AdvanceEnemyAction }
    public sealed class CombatReplayCommand
    {
        public CombatReplayCommand(CombatReplayCommandKind kind, string cardId = null, string ownerId = null, string enemyId = null) { Kind = kind; CardId = cardId; OwnerId = ownerId; EnemyId = enemyId; }
        public CombatReplayCommandKind Kind { get; } public string CardId { get; } public string OwnerId { get; } public string EnemyId { get; }
    }
    public sealed class CombatGoldenReplay
    {
        public CombatGoldenReplay(CombatSetupResult setup, AuthoritativeRngState rng, IReadOnlyList<CombatReplayCommand> commands, IReadOnlyList<GameEvent> events, string checksum)
        { Setup = setup; Rng = rng; Commands = commands; Events = events; Checksum = checksum; }
        public CombatSetupResult Setup { get; } public AuthoritativeRngState Rng { get; } public IReadOnlyList<CombatReplayCommand> Commands { get; } public IReadOnlyList<GameEvent> Events { get; } public string Checksum { get; }
    }
    public static class CombatGoldenReplayRunner
    {
        public static CombatGoldenReplay RecordFixture(CombatSetupResult setup, AuthoritativeRngState rng)
        {
            var state = CombatStateMachine.Create(setup, rng.Clone()); var commands = new List<CombatReplayCommand>(); var events = new List<GameEvent>();
            Execute(state, new CombatReplayCommand(CombatReplayCommandKind.BeginCombat), events, out state); commands.Add(new CombatReplayCommand(CombatReplayCommandKind.BeginCombat));
            for (var guard = 0; !state.IsTerminal && guard < 64; guard++)
            {
                if (state.Phase == CombatPhase.PlayerAction)
                {
                    foreach (var card in state.Deck.Hand.Where(card => card.TargetKind == CardTargetKind.OneEnemy).ToList())
                    {
                        var command = new CombatReplayCommand(CombatReplayCommandKind.PlayCard, card.Id, card.OwnerId.Value, state.Setup.Enemies[0].RuntimeId.Value);
                        Execute(state, command, events, out state); commands.Add(command); if (state.IsTerminal) break;
                    }
                    if (!state.IsTerminal) { var command = new CombatReplayCommand(CombatReplayCommandKind.EndTurn); Execute(state, command, events, out state); commands.Add(command); }
                }
                else if (state.Phase == CombatPhase.EnemyPhaseStart || state.Phase == CombatPhase.EnemyAction)
                { var command = new CombatReplayCommand(CombatReplayCommandKind.AdvanceEnemyAction); Execute(state, command, events, out state); commands.Add(command); }
                else throw new InvalidOperationException("Fixture replay reached an unsupported non-terminal phase: " + state.Phase);
            }
            if (!state.IsTerminal) throw new InvalidOperationException("Fixture replay did not reach a terminal state.");
            return new CombatGoldenReplay(setup, rng.Clone(), commands, events, Checksum(state, events));
        }
        public static string Replay(CombatGoldenReplay replay, out CombatState finalState, out IReadOnlyList<GameEvent> events)
        {
            if (replay == null) throw new ArgumentNullException(nameof(replay)); var state = CombatStateMachine.Create(replay.Setup, replay.Rng.Clone()); var emitted = new List<GameEvent>();
            foreach (var command in replay.Commands) Execute(state, command, emitted, out state);
            finalState = state; events = emitted; return Checksum(state, emitted);
        }
        private static void Execute(CombatState current, CombatReplayCommand command, ICollection<GameEvent> events, out CombatState next)
        {
            CommandResult<CombatState> result = command.Kind == CombatReplayCommandKind.PlayCard ? CardPlayRules.Apply(current, new PlayCardCommand(command.CardId, new RuntimeParticipantId(command.OwnerId), CardTargetChoice.OneEnemy(new RuntimeEnemyId(command.EnemyId)))) : CombatStateMachine.Apply(current, new CombatCommand(command.Kind == CombatReplayCommandKind.BeginCombat ? CombatCommandKind.BeginCombat : command.Kind == CombatReplayCommandKind.EndTurn ? CombatCommandKind.EndTurn : CombatCommandKind.AdvanceEnemyAction));
            if (!result.IsAccepted) throw new InvalidOperationException("Golden replay command rejected: " + result.Rejection.Code); next = result.State; foreach (var gameEvent in result.Events) events.Add(gameEvent);
        }
        private static string Checksum(CombatState state, IEnumerable<GameEvent> events)
        { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(state.CanonicalForm() + "\n" + string.Join("\n", events.Select(gameEvent => gameEvent.CanonicalForm()))))).Replace("-", string.Empty).ToLowerInvariant(); }
    }
}
