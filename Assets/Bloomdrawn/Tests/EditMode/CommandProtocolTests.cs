using System;
using System.Linq;
using Bloomdrawn.Engine.Commands;
using NUnit.Framework;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class CommandProtocolTests
    {
        [Test]
        public void AcceptedCommand_ChangesStateAndEmitsOrderedSemanticEvent()
        {
            var result = SmokeCommandFixture.Execute(new SmokeCommandState(2, 9, 0xA11CEUL), true);

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.State.Revision, Is.EqualTo(3));
            Assert.That(result.Events.Count, Is.EqualTo(1));
            Assert.That(result.Events[0].Sequence, Is.EqualTo(9));
            Assert.That(result.Events[0].Kind, Is.EqualTo("fixture.state-advanced"));
        }

        [Test]
        public void RejectedCommand_ReturnsUnchangedStateAndDiagnostic()
        {
            var initial = new SmokeCommandState(2, 9, 0xA11CEUL);
            var result = SmokeCommandFixture.Execute(initial, false);

            Assert.That(result.IsAccepted, Is.False);
            Assert.That(result.State, Is.SameAs(initial));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.Rejection.Code, Is.EqualTo("fixture.rejected"));
        }

        [Test]
        public void GoldenFixtureChecksum_IsStableAndIndependentOfPresentationState()
        {
            var initial = new SmokeCommandState(0, 0, 0x5EEDUL);
            var commands = new[] { true, false, true };
            SmokeCommandState firstState;
            System.Collections.Generic.IReadOnlyList<GameEvent> firstEvents;
            SmokeCommandState secondState;
            System.Collections.Generic.IReadOnlyList<GameEvent> secondEvents;

            var firstChecksum = GoldenFixtureRunner.Run(initial, commands, out firstState, out firstEvents);
            var secondChecksum = GoldenFixtureRunner.Run(new SmokeCommandState(0, 0, 0x5EEDUL), commands, out secondState, out secondEvents);
            var fixture = new GoldenFixture(initial, commands, firstEvents, firstChecksum);

            Assert.That(secondChecksum, Is.EqualTo(firstChecksum));
            Assert.That(secondState.CanonicalForm(), Is.EqualTo(firstState.CanonicalForm()));
            Assert.That(secondEvents.Select(item => item.CanonicalForm()), Is.EqualTo(firstEvents.Select(item => item.CanonicalForm())));
            Assert.That(fixture.ExpectedEvents.Select(item => item.CanonicalForm()), Is.EqualTo(firstEvents.Select(item => item.CanonicalForm())));
            Assert.That(fixture.Checksum, Is.EqualTo(firstChecksum));
            Assert.That(firstChecksum, Is.Not.EqualTo(string.Empty));
        }
    }
}
