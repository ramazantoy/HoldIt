using HoldIt.Menu;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class LevelSlotsTests
    {
        [Test]
        public void Build_ShowsEveryLevel_OpenOrLocked_WithItsStars()
        {
            var progress = new FakeLevelProgress { Unlocked = 3 };
            progress.Stars[0] = 3;
            progress.Stars[1] = 1;

            var slots = LevelSlots.Build(progress, 5);

            Assert.AreEqual(5, slots.Count);
            for (var i = 0; i < 5; i++)
                Assert.AreEqual(i + 1, slots[i].Number);
            Assert.AreEqual(3, slots[0].Stars);
            Assert.AreEqual(1, slots[1].Stars);
            Assert.AreEqual(0, slots[2].Stars);
            CollectionAssert.AreEqual(new[] { true, true, true, false, false }, new[]
            {
                slots[0].IsOpen, slots[1].IsOpen, slots[2].IsOpen, slots[3].IsOpen, slots[4].IsOpen,
            });
        }

        [Test]
        public void Build_MarksTheLastOpenLevelAsTheNewest()
        {
            var progress = new FakeLevelProgress { Unlocked = 2 };

            var slots = LevelSlots.Build(progress, 3);

            Assert.IsFalse(slots[0].IsNewest);
            Assert.IsTrue(slots[1].IsNewest);
            Assert.IsFalse(slots[2].IsNewest);
        }

        [Test]
        public void Build_WithEveryLevelOpen_TheLastIsTheNewest()
        {
            var progress = new FakeLevelProgress { Unlocked = 3 };

            var slots = LevelSlots.Build(progress, 3);

            Assert.IsTrue(slots[2].IsNewest);
            Assert.IsTrue(slots[2].IsOpen);
        }
    }
}
