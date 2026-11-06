using HoldIt.Characters;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class KnockPoseTests
    {
        private const float Tolerance = 1e-3f;

        // Three knocks 0.16 s apart (0.48 s), then 1.1 s of rest: a row every 1.58 s. Arm raised 75°, pulled back 18°.
        private static readonly KnockStyle _style = KnockStyle.Default;

        [Test]
        public void CycleSeconds_IsTheRowPlusTheRest()
        {
            Assert.AreEqual(1.58f, KnockPose.CycleSeconds(_style), Tolerance);
        }

        [Test]
        public void Arm_WhenAKnockLands_IsFullyRaised()
        {
            Assert.AreEqual(75f, KnockPose.Arm(0f, _style), Tolerance);
            Assert.AreEqual(75f, KnockPose.Arm(0.16f, _style), Tolerance);
            Assert.AreEqual(75f, KnockPose.Arm(0.32f, _style), Tolerance);
        }

        [Test]
        public void Arm_BetweenTwoKnocks_IsPulledBack()
        {
            Assert.AreEqual(57f, KnockPose.Arm(0.08f, _style), Tolerance);
            Assert.AreEqual(57f, KnockPose.Arm(0.24f, _style), Tolerance);
        }

        [Test]
        public void Arm_InTheMiddleOfTheRest_IsDown()
        {
            Assert.AreEqual(0f, KnockPose.Arm(0.48f + 0.55f, _style), Tolerance);
        }

        [Test]
        public void Arm_AroundTheRest_ComesDownAndGoesBackUpWithoutAJump()
        {
            Assert.AreEqual(75f, KnockPose.Arm(0.48f, _style), Tolerance);
            Assert.Less(KnockPose.Arm(0.48f + 0.1f, _style), 75f);
            Assert.Greater(KnockPose.Arm(0.48f + 0.1f, _style), 0f);
            Assert.AreEqual(75f, KnockPose.Arm(1.58f - 1e-3f, _style), 0.5f);
        }

        [Test]
        public void Arm_RepeatsEveryRow()
        {
            Assert.AreEqual(KnockPose.Arm(0.05f, _style), KnockPose.Arm(0.05f + 1.58f, _style), Tolerance);
            Assert.AreEqual(KnockPose.Arm(1.0f, _style), KnockPose.Arm(1.0f + 3 * 1.58f, _style), Tolerance);
        }

        [Test]
        public void Hits_TheFirstKnockLandsAtTimeZero()
        {
            Assert.AreEqual(1, KnockPose.Hits(-1e-4f, 0.01f, _style));
        }

        [Test]
        public void Hits_ARowHasOneKnockPerBeatAndNoneInTheRest()
        {
            Assert.AreEqual(3, KnockPose.Hits(-1e-4f, 0.5f, _style));
            Assert.AreEqual(3, KnockPose.Hits(-1e-4f, 1.5f, _style));
            Assert.AreEqual(0, KnockPose.Hits(0.5f, 1.5f, _style));
        }

        [Test]
        public void Hits_OnlyCountsKnocksInsideTheInterval()
        {
            Assert.AreEqual(1, KnockPose.Hits(0.1f, 0.2f, _style));
            Assert.AreEqual(0, KnockPose.Hits(0.17f, 0.3f, _style));
        }

        [Test]
        public void Hits_NextRowStartsWithAKnock()
        {
            Assert.AreEqual(7, KnockPose.Hits(-1e-4f, 2 * 1.58f + 0.01f, _style));
        }

        [Test]
        public void Hits_NoTimeNoKnocks()
        {
            Assert.AreEqual(0, KnockPose.Hits(0.16f, 0.16f, _style));
            Assert.AreEqual(0, KnockPose.Hits(0.7f, 0.7f, _style));
        }

        [Test]
        public void Hits_SmallStepsAddUpToTheWholeInterval()
        {
            var total = 0;
            var time = -1e-4f;
            while (time < 5f)
            {
                var next = time + 0.0137f;
                total += KnockPose.Hits(time, next, _style);
                time = next;
            }

            Assert.AreEqual(KnockPose.Hits(-1e-4f, time, _style), total);
        }
    }
}
