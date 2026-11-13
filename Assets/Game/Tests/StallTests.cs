using System;
using HoldIt.Restrooms;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class StallTests
    {
        private const float Busy = 4.5f;

        [Test]
        public void New_IsFree()
        {
            Assert.AreEqual(StallState.Free, new Stall().State);
        }

        [Test]
        public void Take_AFreeStall_IsTaken()
        {
            var stall = new Stall();

            stall.Take();

            Assert.AreEqual(StallState.Taken, stall.State);
        }

        [Test]
        public void Take_ATakenOrShutStall_Throws()
        {
            var stall = new Stall();
            stall.Take();
            Assert.Throws<InvalidOperationException>(() => stall.Take());

            stall.Shut();
            Assert.Throws<InvalidOperationException>(() => stall.Take());
        }

        [Test]
        public void Shut_OnlyATakenStall_Shuts()
        {
            var stall = new Stall();
            Assert.Throws<InvalidOperationException>(() => stall.Shut());

            stall.Take();
            stall.Shut();

            Assert.AreEqual(StallState.Shut, stall.State);
            Assert.Throws<InvalidOperationException>(() => stall.Shut());
        }

        [Test]
        public void Advance_AFreeStall_StaysFree()
        {
            var stall = new Stall();

            Assert.IsFalse(stall.Advance(100f, Busy));
            Assert.AreEqual(StallState.Free, stall.State);
        }

        [Test]
        public void Advance_ATakenStallPastItsTime_DoesNotFreeBeforeTheVisitorIsIn()
        {
            var stall = new Stall();
            stall.Take();

            Assert.IsFalse(stall.Advance(Busy * 3f, Busy));
            Assert.AreEqual(StallState.Taken, stall.State);
        }

        [Test]
        public void Advance_ShutAfterTheTimeHasPassed_FreesAtOnce()
        {
            var stall = new Stall();
            stall.Take();
            stall.Advance(Busy + 1f, Busy);

            stall.Shut();

            Assert.IsTrue(stall.Advance(0f, Busy), "The time counts from when it was taken.");
            Assert.AreEqual(StallState.Free, stall.State);
        }

        [Test]
        public void Advance_ShutBeforeTheTime_FreesWhenItIsUp()
        {
            var stall = new Stall();
            stall.Take();
            stall.Advance(1f, Busy);
            stall.Shut();

            Assert.IsFalse(stall.Advance(3f, Busy));
            Assert.IsTrue(stall.Advance(0.5f, Busy));
            Assert.AreEqual(StallState.Free, stall.State);
        }

        [Test]
        public void Take_AfterItFreed_StartsANewRound()
        {
            var stall = new Stall();
            stall.Take();
            stall.Shut();
            stall.Advance(Busy, Busy);

            stall.Take();
            stall.Shut();

            Assert.IsFalse(stall.Advance(Busy - 0.5f, Busy), "The previous round's time must not carry over.");
        }
    }
}
