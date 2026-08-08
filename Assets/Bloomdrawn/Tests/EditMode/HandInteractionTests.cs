using System.Collections.Generic;
using System.Linq;
using Bloomdrawn.Presentation;
using NUnit.Framework;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class HandInteractionTests
    {
        [Test]
        public void FanIsBottomCentredStableAndDoesNotUseTemporaryDragGeometry()
        {
            var first=HandFanLayout.Calculate(5,1920); var second=HandFanLayout.Calculate(5,1920);
            Assert.That(first.Select(p=>p.Position.x),Is.EqualTo(second.Select(p=>p.Position.x))); Assert.That(first[2].Position.x,Is.EqualTo(960)); Assert.That(first.Select(p=>p.Depth),Is.EqualTo(new[]{0,1,2,3,4}));
        }

        [Test]
        public void LargerHandsContractWithinTheDeclaredContainerInsteadOfLosingOuterCards()
        {
            var poses = HandFanLayout.Calculate(10, 1040f, 188f, 22f, 8f);
            var minimumCentre = HandFanLayout.DefaultCardWidth * .5f;
            var maximumCentre = 1040f - minimumCentre;

            Assert.That(poses, Has.Count.EqualTo(10));
            Assert.That(poses.Select(p => p.Position.x), Is.Ordered);
            Assert.That(poses.Min(p => p.Position.x), Is.GreaterThanOrEqualTo(minimumCentre - 1f));
            Assert.That(poses.Max(p => p.Position.x), Is.LessThanOrEqualTo(maximumCentre + 1f));
            Assert.That((poses[4].Position.x + poses[5].Position.x) * .5f, Is.EqualTo(520f).Within(.001f));
        }

        [Test]
        public void DragThresholdCancelAndTargetStagingNeverSubmitPartialCommand()
        {
            var sink=new Sink();var controller=new CardInteractionController(sink);controller.BeginDrag("card","owner",true);controller.UpdateArmed(false);Assert.That(controller.Release(),Is.False);Assert.That(sink.Submissions,Is.Empty);
            controller.BeginDrag("card","owner",true);controller.UpdateArmed(true);Assert.That(controller.Release(),Is.False);Assert.That(controller.State,Is.EqualTo(CardInteractionState.TargetSelection));Assert.That(sink.Submissions,Is.Empty);controller.Cancel();Assert.That(controller.State,Is.EqualTo(CardInteractionState.Resting));
        }
        [Test]
        public void CompleteTargetAndNoTargetRoutesSubmitExactlyOneAndRejectionResyncs()
        {
            var sink=new Sink();var controller=new CardInteractionController(sink);controller.BeginDrag("a","o",false);controller.UpdateArmed(true);Assert.That(controller.Release(),Is.True);Assert.That(sink.Submissions,Has.Count.EqualTo(1));
            sink.Accept=false;controller.BeginDrag("b","o",true);controller.UpdateArmed(true);controller.Release();Assert.That(controller.SelectEnemy("enemy"),Is.False);Assert.That(controller.State,Is.EqualTo(CardInteractionState.Resting));Assert.That(sink.Submissions,Has.Count.EqualTo(2));
        }
        private sealed class Sink:ICompleteCardCommandSink { public bool Accept=true; public List<CardCommandSubmission> Submissions=new List<CardCommandSubmission>(); public bool Submit(CardCommandSubmission value){Submissions.Add(value);return Accept;} }
    }
}
