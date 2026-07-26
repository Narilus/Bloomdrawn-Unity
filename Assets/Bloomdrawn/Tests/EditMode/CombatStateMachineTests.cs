using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomdrawn.Content;
using Bloomdrawn.Content.Editor;
using Bloomdrawn.Engine.Combat;
using NUnit.Framework;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class CombatStateMachineTests
    {
        [Test]
        public void DeclaredTransitions_OnlyAllowApprovedPredecessors()
        {
            var expected = new Dictionary<CombatPhase, CombatPhase[]>
            {
                { CombatPhase.PlayerTurnStart, new[] { CombatPhase.CombatSetup, CombatPhase.RoundEnd } },
                { CombatPhase.PlayerAction, new[] { CombatPhase.PlayerTurnStart } },
                { CombatPhase.PlayerCleanup, new[] { CombatPhase.PlayerAction } },
                { CombatPhase.PlayerEnd, new[] { CombatPhase.PlayerCleanup } },
                { CombatPhase.EnemyPhaseStart, new[] { CombatPhase.PlayerEnd } },
                { CombatPhase.EnemyAction, new[] { CombatPhase.EnemyPhaseStart, CombatPhase.EnemyAction } },
                { CombatPhase.EnemyEnd, new[] { CombatPhase.EnemyPhaseStart, CombatPhase.EnemyAction } },
                { CombatPhase.RoundEnd, new[] { CombatPhase.EnemyEnd } }
            };

            foreach (CombatPhase from in Enum.GetValues(typeof(CombatPhase)))
            {
                foreach (CombatPhase to in Enum.GetValues(typeof(CombatPhase)))
                {
                    if (to == CombatPhase.Victory || to == CombatPhase.Defeat)
                        continue;
                    var isExpected = expected.ContainsKey(to) && expected[to].Contains(from);
                    Assert.That(CombatPhaseRules.CanTransition(from, to), Is.EqualTo(isExpected), from + " -> " + to);
                }
            }
        }

        [Test]
        public void BeginCombat_AdvancesInternallyToPlayerActionWithOrderedPhaseEvents()
        {
            var result = CombatStateMachine.Apply(CreateState(), new CombatCommand(CombatCommandKind.BeginCombat));

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.State.Phase, Is.EqualTo(CombatPhase.PlayerAction));
            Assert.That(result.State.RoundNumber, Is.EqualTo(1));
            Assert.That(result.Events.Select(item => item.Sequence), Is.EqualTo(new long[] { 0, 1 }));
            Assert.That(result.Events.Select(item => item.Facts["to"]), Is.EqualTo(new[] { "PlayerTurnStart", "PlayerAction" }));
        }

        [Test]
        public void EndTurn_AdvancesOnlyThroughCurrentM1BPhaseResponsibilities()
        {
            var opening = CombatStateMachine.Apply(CreateState(), new CombatCommand(CombatCommandKind.BeginCombat));
            var result = CombatStateMachine.Apply(opening.State, new CombatCommand(CombatCommandKind.EndTurn));

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.State.Phase, Is.EqualTo(CombatPhase.EnemyPhaseStart));
            Assert.That(result.Events.Select(item => item.Facts["to"]), Is.EqualTo(new[] { "PlayerCleanup", "PlayerEnd", "EnemyPhaseStart" }));
        }

        [Test]
        public void IllegalCommands_RejectWithoutStateOrEvents()
        {
            var setup = CreateState();
            var result = CombatStateMachine.Apply(setup, new CombatCommand(CombatCommandKind.EndTurn));

            Assert.That(result.IsAccepted, Is.False);
            Assert.That(result.State, Is.SameAs(setup));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.Rejection.Code, Is.EqualTo("combat.illegal-phase"));
        }

        [TestCase(CombatPhase.Victory)]
        [TestCase(CombatPhase.Defeat)]
        public void TerminalStates_RejectNormalCombatCommands(CombatPhase terminalPhase)
        {
            var terminal = CombatStateMachine.CreateTerminal(CreateSetup(), terminalPhase);
            var result = CombatStateMachine.Apply(terminal, new CombatCommand(CombatCommandKind.BeginCombat));

            Assert.That(result.IsAccepted, Is.False);
            Assert.That(result.State, Is.SameAs(terminal));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.Rejection.Code, Is.EqualTo("combat.terminal"));
        }

        [Test]
        public void SameSetupAndCommands_ProduceIdenticalPhaseAndEventTrace()
        {
            var first = RunTrace();
            var second = RunTrace();

            Assert.That(second.State.CanonicalForm(), Is.EqualTo(first.State.CanonicalForm()));
            Assert.That(second.Events.Select(item => item.CanonicalForm()), Is.EqualTo(first.Events.Select(item => item.CanonicalForm())));
        }

        private static (CombatState State, IReadOnlyList<Bloomdrawn.Engine.Commands.GameEvent> Events) RunTrace()
        {
            var opening = CombatStateMachine.Apply(CreateState(), new CombatCommand(CombatCommandKind.BeginCombat));
            var ending = CombatStateMachine.Apply(opening.State, new CombatCommand(CombatCommandKind.EndTurn));
            return (ending.State, opening.Events.Concat(ending.Events).ToList());
        }

        private static CombatState CreateState() => CombatStateMachine.Create(CreateSetup());

        private static CombatSetupResult CreateSetup()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            var validation = ContentImportService.ImportDirectory(Path.Combine(projectRoot, "GameContent", "fixtures"), ContentOrigin.Fixture);
            Assert.That(validation.IsValid, Is.True, string.Join(Environment.NewLine, validation.Errors.Select(error => error.Code + ":" + error.Message)));
            return FixtureCombatSetupFactory.Create(FixtureCombatCatalog.Create(validation.Content), new CombatSetupRequest("fixture.m1.lineup.quartet", "fixture.m1.encounter.training"));
        }
    }
}
