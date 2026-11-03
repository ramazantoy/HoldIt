using System;
using HoldIt.Levels;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class LevelCursorTests
    {
        private LevelDefinition _first;
        private LevelDefinition _second;
        private LevelDefinition _third;
        private LevelList _list;
        private FakeLevelProgress _progress;

        [SetUp]
        public void CreateLevels()
        {
            _first = TestLevels.Level(4);
            _second = TestLevels.Level(6);
            _third = TestLevels.Level(8);
            _list = TestLevels.List(_first, _second, _third);
            _progress = new FakeLevelProgress { Unlocked = 3 };
        }

        [TearDown]
        public void DestroyLevels()
        {
            UnityEngine.Object.DestroyImmediate(_list);
            UnityEngine.Object.DestroyImmediate(_first);
            UnityEngine.Object.DestroyImmediate(_second);
            UnityEngine.Object.DestroyImmediate(_third);
        }

        [Test]
        public void New_IsOnTheFirstLevel()
        {
            var cursor = new LevelCursor(_list, _progress);

            Assert.AreEqual(0, cursor.Index);
            Assert.AreEqual(1, cursor.Number);
            Assert.AreEqual(3, cursor.Count);
            Assert.AreSame(_first, cursor.Current);
            Assert.IsTrue(cursor.HasNext);
            Assert.IsTrue(cursor.IsNextOpen);
        }

        [Test]
        public void Next_MovesOnToTheLast()
        {
            var cursor = new LevelCursor(_list, _progress);

            cursor.Next();
            cursor.Next();

            Assert.AreEqual(2, cursor.Index);
            Assert.AreEqual(3, cursor.Number);
            Assert.AreSame(_third, cursor.Current);
            Assert.IsFalse(cursor.HasNext);
            Assert.IsFalse(cursor.IsNextOpen);
        }

        [Test]
        public void Next_OnTheLast_ThrowsAndStays()
        {
            var cursor = new LevelCursor(_list, _progress);
            cursor.Select(2);

            Assert.Throws<InvalidOperationException>(() => cursor.Next());
            Assert.AreEqual(2, cursor.Index);
        }

        [Test]
        public void Next_OntoALockedLevel_ThrowsAndStays()
        {
            _progress.Unlocked = 1;
            var cursor = new LevelCursor(_list, _progress);

            Assert.IsTrue(cursor.HasNext);
            Assert.IsFalse(cursor.IsNextOpen);
            Assert.Throws<InvalidOperationException>(() => cursor.Next());
            Assert.AreEqual(0, cursor.Index);
        }

        [Test]
        public void IsNextOpen_FollowsTheProgress()
        {
            _progress.Unlocked = 1;
            var cursor = new LevelCursor(_list, _progress);

            _progress.Unlocked = 2;

            Assert.IsTrue(cursor.IsNextOpen);
        }

        [Test]
        public void Select_MovesToAnOpenLevel()
        {
            _progress.Unlocked = 2;
            var cursor = new LevelCursor(_list, _progress);

            cursor.Select(1);

            Assert.AreEqual(1, cursor.Index);
            Assert.AreSame(_second, cursor.Current);
        }

        [Test]
        public void Select_ALockedLevelOrOneOutOfRange_ThrowsAndStays()
        {
            _progress.Unlocked = 2;
            var cursor = new LevelCursor(_list, _progress);
            cursor.Select(1);

            Assert.Throws<InvalidOperationException>(() => cursor.Select(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => cursor.Select(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => cursor.Select(-1));
            Assert.AreEqual(1, cursor.Index);
        }

        [Test]
        public void New_WithoutLevelsOrProgress_Throws()
        {
            var empty = TestLevels.List();
            try
            {
                Assert.Throws<ArgumentException>(() => new LevelCursor(empty, _progress));
                Assert.Throws<ArgumentNullException>(() => new LevelCursor(null, _progress));
                Assert.Throws<ArgumentNullException>(() => new LevelCursor(_list, null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(empty);
            }
        }
    }
}
