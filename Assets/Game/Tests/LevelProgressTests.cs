using System;
using System.Collections.Generic;
using AF.Save;
using HoldIt.Levels;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class LevelProgressTests
    {
        private LevelDefinition[] _levels;
        private LevelList _list;
        private LevelProgress _progress;

        [SetUp]
        public void CreateProgress()
        {
            _levels = new[] { TestLevels.Level(4), TestLevels.Level(5), TestLevels.Level(6) };
            _list = TestLevels.List(_levels);
            _progress = new LevelProgress(_list);
        }

        [TearDown]
        public void DestroyLevels()
        {
            UnityEngine.Object.DestroyImmediate(_list);
            foreach (var level in _levels)
                UnityEngine.Object.DestroyImmediate(level);
        }

        [Test]
        public void New_HasTheFirstLevelOpenAndNoStars()
        {
            Assert.AreEqual(1, _progress.Unlocked);
            Assert.IsTrue(_progress.IsUnlocked(0));
            Assert.IsFalse(_progress.IsUnlocked(1));
            Assert.IsFalse(_progress.IsUnlocked(-1));
            Assert.AreEqual(0, _progress.BestStars(0));
            Assert.AreEqual(0, _progress.BestStars(7));
        }

        [Test]
        public void Savable_IsTheGlobalProgressSection()
        {
            ISavable savable = _progress;

            Assert.AreEqual("game.progress", savable.Key);
            Assert.AreEqual(SaveDocument.Global, savable.Document);
            Assert.AreEqual(typeof(LevelProgressData), savable.DataType);
        }

        [Test]
        public void RecordWin_OpensTheNextLevel()
        {
            _progress.RecordWin(0, 2);

            Assert.AreEqual(2, _progress.Unlocked);
            Assert.IsTrue(_progress.IsUnlocked(1));
            Assert.IsFalse(_progress.IsUnlocked(2));
            Assert.AreEqual(2, _progress.BestStars(0));
        }

        [Test]
        public void RecordWin_KeepsTheBestStars()
        {
            _progress.RecordWin(0, 2);
            _progress.RecordWin(0, 1);
            Assert.AreEqual(2, _progress.BestStars(0), "A worse win keeps the best.");

            _progress.RecordWin(0, 3);
            Assert.AreEqual(3, _progress.BestStars(0));
        }

        [Test]
        public void RecordWin_OnTheLastLevel_OpensNothingPastIt()
        {
            _progress.UnlockAll();

            _progress.RecordWin(2, 3);

            Assert.AreEqual(3, _progress.Unlocked);
            Assert.AreEqual(3, ((LevelProgressData)((ISavable)_progress).CaptureData()).unlocked);
        }

        [Test]
        public void RecordWin_OfAnEarlierLevel_KeepsTheLaterOnesOpen()
        {
            _progress.RecordWin(0, 1);
            _progress.RecordWin(1, 1);

            _progress.RecordWin(0, 3);

            Assert.AreEqual(3, _progress.Unlocked);
            Assert.AreEqual(1, _progress.BestStars(1));
        }

        [Test]
        public void RecordWin_RefusesWhatCannotHappen()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _progress.RecordWin(3, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _progress.RecordWin(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _progress.RecordWin(0, 0));
            Assert.AreEqual(1, _progress.Unlocked);
        }

        [Test]
        public void CaptureAndRestore_KeepTheProgress()
        {
            _progress.RecordWin(0, 3);
            _progress.RecordWin(1, 2);
            var data = ((ISavable)_progress).CaptureData();

            var loaded = new LevelProgress(_list);
            ((ISavable)loaded).RestoreData(data);

            Assert.AreEqual(3, loaded.Unlocked);
            Assert.AreEqual(3, loaded.BestStars(0));
            Assert.AreEqual(2, loaded.BestStars(1));
            Assert.AreEqual(0, loaded.BestStars(2));
        }

        [Test]
        public void Restore_ReplacesTheWholeProgress()
        {
            _progress.RecordWin(0, 3);

            ((ISavable)_progress).RestoreData(((ISavable)_progress).CreateDefaultData());

            Assert.AreEqual(1, _progress.Unlocked);
            Assert.AreEqual(0, _progress.BestStars(0));
        }

        [Test]
        public void Restore_OfAStrangeFile_DoesNotThrowAndLimitsWhatItReads()
        {
            var data = new LevelProgressData { stars = new List<int> { -2, 3 }, unlocked = 0 };
            ((ISavable)_progress).RestoreData(data);
            Assert.AreEqual(1, _progress.Unlocked);
            Assert.AreEqual(0, _progress.BestStars(0));
            Assert.AreEqual(3, _progress.BestStars(1));

            ((ISavable)_progress).RestoreData(new LevelProgressData { stars = null, unlocked = 99 });
            Assert.AreEqual(3, _progress.Unlocked, "More open levels than the list has read as all of them.");
            Assert.AreEqual(99, ((LevelProgressData)((ISavable)_progress).CaptureData()).unlocked,
                "The saved value stays: a longer list later opens them.");
            Assert.AreEqual(0, _progress.BestStars(0));
        }

        [Test]
        public void UnlockAll_OpensEveryLevel_AndResetStartsOver()
        {
            _progress.RecordWin(0, 2);

            _progress.UnlockAll();
            Assert.AreEqual(3, _progress.Unlocked);
            Assert.AreEqual(2, _progress.BestStars(0));

            _progress.ResetProgress();
            Assert.AreEqual(1, _progress.Unlocked);
            Assert.AreEqual(0, _progress.BestStars(0));
        }
    }
}
