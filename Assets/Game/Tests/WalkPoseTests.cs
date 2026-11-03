using HoldIt.Characters;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class WalkPoseTests
    {
        private const float Tolerance = 1e-4f;
        private static readonly WalkStyle _style = new WalkStyle(0.8f, 40f, 30f, 0.04f, 4f, 10f);

        [Test]
        public void At_NoBlend_IsTheRestPose()
        {
            foreach (var phase in new[] { 0f, 0.125f, 0.25f, 0.6f })
            {
                var pose = WalkPose.At(phase, 0f, _style);

                Assert.AreEqual(0f, pose.LegLeft, Tolerance);
                Assert.AreEqual(0f, pose.LegRight, Tolerance);
                Assert.AreEqual(0f, pose.ArmLeft, Tolerance);
                Assert.AreEqual(0f, pose.ArmRight, Tolerance);
                Assert.AreEqual(0f, pose.BodyRise, Tolerance);
                Assert.AreEqual(0f, pose.BodyRock, Tolerance);
            }
        }

        [Test]
        public void At_AQuarterCycle_HasTheLeftLegFullyAheadAndTheLeftArmBack()
        {
            var pose = WalkPose.At(0.25f, 1f, _style);

            Assert.AreEqual(_style.LegSwing, pose.LegLeft, Tolerance);
            Assert.AreEqual(-_style.LegSwing, pose.LegRight, Tolerance);
            Assert.AreEqual(-_style.ArmSwing, pose.ArmLeft, Tolerance);
            Assert.AreEqual(_style.ArmSwing, pose.ArmRight, Tolerance);
        }

        [Test]
        public void At_ThreeQuarters_SwapsTheSides()
        {
            var pose = WalkPose.At(0.75f, 1f, _style);

            Assert.AreEqual(-_style.LegSwing, pose.LegLeft, Tolerance);
            Assert.AreEqual(_style.ArmSwing, pose.ArmLeft, Tolerance);
        }

        [Test]
        public void At_TheBodyRisesWhileTheLegsPassAndSinksAtTheFullStride()
        {
            Assert.AreEqual(_style.Bob, WalkPose.At(0f, 1f, _style).BodyRise, Tolerance);
            Assert.AreEqual(_style.Bob, WalkPose.At(0.5f, 1f, _style).BodyRise, Tolerance);
            Assert.AreEqual(0f, WalkPose.At(0.25f, 1f, _style).BodyRise, Tolerance);
        }

        [Test]
        public void At_TheRockPeaksBetweenTheRiseAndTheStride()
        {
            Assert.AreEqual(_style.Sway, WalkPose.At(0.125f, 1f, _style).BodyRock, Tolerance);
            Assert.Less(WalkPose.At(0f, 1f, _style).BodyRock, _style.Sway - Tolerance);
            Assert.Less(WalkPose.At(0.25f, 1f, _style).BodyRock, _style.Sway - Tolerance);
        }

        [Test]
        public void At_HalfBlend_HalvesEverything()
        {
            var full = WalkPose.At(0.2f, 1f, _style);
            var half = WalkPose.At(0.2f, 0.5f, _style);

            Assert.AreEqual(full.LegLeft * 0.5f, half.LegLeft, Tolerance);
            Assert.AreEqual(full.ArmRight * 0.5f, half.ArmRight, Tolerance);
            Assert.AreEqual(full.BodyRise * 0.5f, half.BodyRise, Tolerance);
            Assert.AreEqual(full.BodyRock * 0.5f, half.BodyRock, Tolerance);
        }

        [Test]
        public void At_ABlendOutOfRange_IsClamped()
        {
            Assert.AreEqual(WalkPose.At(0.25f, 1f, _style).LegLeft, WalkPose.At(0.25f, 3f, _style).LegLeft, Tolerance);
            Assert.AreEqual(0f, WalkPose.At(0.25f, -1f, _style).LegLeft, Tolerance);
        }
    }
}
