using System;
using HoldIt.Spawning;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class SpawnScheduleTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void GapAfter_TheFirstArrival_IsTheStart()
        {
            Assert.AreEqual(4f, SpawnSchedule.GapAfter(1, 5, 4f, 2f), Tolerance);
        }

        [Test]
        public void GapAfter_TheLastButOne_IsTheEnd()
        {
            Assert.AreEqual(2f, SpawnSchedule.GapAfter(4, 5, 4f, 2f), Tolerance);
        }

        [Test]
        public void GapAfter_InBetween_ShrinksEvenly()
        {
            Assert.AreEqual(4f - 2f / 3f, SpawnSchedule.GapAfter(2, 5, 4f, 2f), Tolerance);
            Assert.AreEqual(4f - 4f / 3f, SpawnSchedule.GapAfter(3, 5, 4f, 2f), Tolerance);
        }

        [Test]
        public void GapAfter_ACrowdOfTwo_HasOnlyTheStart()
        {
            Assert.AreEqual(4f, SpawnSchedule.GapAfter(1, 2, 4f, 2f), Tolerance);
        }

        [Test]
        public void GapAfter_PastTheCrowd_StaysAtTheEnd()
        {
            Assert.AreEqual(2f, SpawnSchedule.GapAfter(9, 5, 4f, 2f), Tolerance);
        }

        [Test]
        public void GapAfter_NoArrivalYet_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SpawnSchedule.GapAfter(0, 5, 4f, 2f));
        }
    }
}
