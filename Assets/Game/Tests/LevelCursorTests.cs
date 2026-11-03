using System;
using HoldIt.Levels;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class LevelCursorTests
    {
        private LevelDefinition _first;
        private LevelDefinition _second;
        private LevelList _list;

        [SetUp]
        public void CreateLevels()
        {
            _first = TestLevels.Level(4);
            _second = TestLevels.Level(6);
            _list = TestLevels.List(_first, _second);
        }

        [TearDown]
        public void DestroyLevels()
        {
            UnityEngine.Object.DestroyImmediate(_list);
            UnityEngine.Object.DestroyImmediate(_first);
            UnityEngine.Object.DestroyImmediate(_second);
        }

        [Test]
        public void New_IsOnTheFirstLevel()
        {
            var cursor = new LevelCursor(_list);

            Assert.AreEqual(0, cursor.Index);
            Assert.AreEqual(1, cursor.Number);
            Assert.AreEqual(2, cursor.Count);
            Assert.AreSame(_first, cursor.Current);
            Assert.IsTrue(cursor.HasNext);
        }

        [Test]
        public void Next_MovesOnToTheLast()
        {
            var cursor = new LevelCursor(_list);

            cursor.Next();

            Assert.AreEqual(1, cursor.Index);
            Assert.AreEqual(2, cursor.Number);
            Assert.AreSame(_second, cursor.Current);
            Assert.IsFalse(cursor.HasNext);
        }

        [Test]
        public void Next_OnTheLast_ThrowsAndStays()
        {
            var cursor = new LevelCursor(_list);
            cursor.Next();

            Assert.Throws<InvalidOperationException>(() => cursor.Next());
            Assert.AreEqual(1, cursor.Index);
        }

        [Test]
        public void New_WithoutLevels_Throws()
        {
            var empty = TestLevels.List();
            try
            {
                Assert.Throws<ArgumentException>(() => new LevelCursor(empty));
                Assert.Throws<ArgumentNullException>(() => new LevelCursor(null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(empty);
            }
        }
    }
}
