using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Drawing;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class MarkerDiffTests
    {
        private static readonly CharacterId _a = new CharacterId(1);
        private static readonly CharacterId _b = new CharacterId(2);

        private MarkerDiff _diff;

        [SetUp]
        public void SetUp() => _diff = new MarkerDiff();

        private static MarkerKey Key(CharacterId owner, int col, int row) => new MarkerKey(owner, new Cell(col, row));

        private static List<(MarkerKey, MarkerLook)> Wanted(params (MarkerKey, MarkerLook)[] marks) =>
            new List<(MarkerKey, MarkerLook)>(marks);

        [Test]
        public void Update_FirstTime_AddsEverythingInOrder()
        {
            _diff.Update(Wanted((Key(_a, 1, 1), MarkerLook.Male), (Key(_a, 1, 2), MarkerLook.Male)));

            Assert.AreEqual(2, _diff.Added.Count);
            Assert.AreEqual(Key(_a, 1, 1), _diff.Added[0].Key);
            Assert.AreEqual(Key(_a, 1, 2), _diff.Added[1].Key);
            Assert.IsEmpty(_diff.Removed);
            Assert.IsEmpty(_diff.Changed);
        }

        [Test]
        public void Update_SameMarks_ChangesNothing()
        {
            var marks = Wanted((Key(_a, 1, 1), MarkerLook.Male));
            _diff.Update(marks);

            _diff.Update(marks);

            Assert.IsEmpty(_diff.Added);
            Assert.IsEmpty(_diff.Removed);
            Assert.IsEmpty(_diff.Changed);
        }

        [Test]
        public void Update_AMarkLeaves_IsRemovedOnce()
        {
            _diff.Update(Wanted((Key(_a, 1, 1), MarkerLook.Male), (Key(_a, 1, 2), MarkerLook.Male)));

            _diff.Update(Wanted((Key(_a, 1, 1), MarkerLook.Male)));

            Assert.AreEqual(1, _diff.Removed.Count);
            Assert.AreEqual(Key(_a, 1, 2), _diff.Removed[0]);
            _diff.Update(Wanted((Key(_a, 1, 1), MarkerLook.Male)));
            Assert.IsEmpty(_diff.Removed);
        }

        [Test]
        public void Update_ALookChanges_IsReportedWithTheNewLook()
        {
            _diff.Update(Wanted((Key(_a, 1, 1), MarkerLook.Male)));

            _diff.Update(Wanted((Key(_a, 1, 1), MarkerLook.Blocked)));

            Assert.AreEqual(1, _diff.Changed.Count);
            Assert.AreEqual(MarkerLook.Blocked, _diff.Changed[0].Look);
            Assert.IsEmpty(_diff.Added);
            Assert.IsEmpty(_diff.Removed);
        }

        [Test]
        public void Update_TheSameCellForAnotherCharacter_IsAnotherMark()
        {
            _diff.Update(Wanted((Key(_a, 1, 1), MarkerLook.Male)));

            _diff.Update(Wanted((Key(_a, 1, 1), MarkerLook.Male), (Key(_b, 1, 1), MarkerLook.Female)));

            Assert.AreEqual(1, _diff.Added.Count);
            Assert.AreEqual(Key(_b, 1, 1), _diff.Added[0].Key);
            Assert.IsEmpty(_diff.Removed);
        }

        [Test]
        public void Update_AKeyListedTwice_CountsOnceAndTheFirstLookWins()
        {
            _diff.Update(Wanted((Key(_a, 1, 1), MarkerLook.Male), (Key(_a, 1, 1), MarkerLook.Blocked)));

            Assert.AreEqual(1, _diff.Added.Count);
            Assert.AreEqual(MarkerLook.Male, _diff.Added[0].Look);
            Assert.AreEqual(1, _diff.Count);
        }

        [Test]
        public void Update_ADrawnPathBecomesAWalk_KeepsItsMarks()
        {
            // The drawer's own cell leaves, the rest carry on under the same keys.
            _diff.Update(Wanted((Key(_a, 1, 1), MarkerLook.Male), (Key(_a, 1, 2), MarkerLook.Male), (Key(_a, 1, 3), MarkerLook.Male)));

            _diff.Update(Wanted((Key(_a, 1, 2), MarkerLook.Male), (Key(_a, 1, 3), MarkerLook.Male)));

            Assert.IsEmpty(_diff.Added);
            Assert.AreEqual(1, _diff.Removed.Count);
            Assert.AreEqual(Key(_a, 1, 1), _diff.Removed[0]);
        }

        [Test]
        public void Update_AMarkReturnsAfterLeaving_IsAddedAgain()
        {
            _diff.Update(Wanted((Key(_a, 1, 1), MarkerLook.Male)));
            _diff.Update(Wanted());

            _diff.Update(Wanted((Key(_a, 1, 1), MarkerLook.Male)));

            Assert.AreEqual(1, _diff.Added.Count);
        }
    }
}
