using System;
using System.Collections.Generic;
using HoldIt.Levels;
using HoldIt.Menu;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class LevelSelectTests
    {
        private FakeLevelSelectView _view;
        private FakeLevelProgress _progress;
        private FakeLevelCursor _cursor;
        private RunConfig _config;
        private LevelSelect _select;
        private List<int> _chosen;
        private int _closes;

        [SetUp]
        public void SetUpSelect()
        {
            _view = new FakeLevelSelectView();
            _progress = new FakeLevelProgress { Unlocked = 2 };
            _progress.Stars[0] = 2;
            _cursor = new FakeLevelCursor { Count = 4 };
            _config = ScriptableObject.CreateInstance<RunConfig>();
            _select = new LevelSelect(_view, _progress, _cursor, _config);
            _chosen = new List<int>();
            _closes = 0;
            _select.LevelChosen += _chosen.Add;
            _select.Closed += () => _closes++;
            _select.Initialize();
            _select.Start();
        }

        [TearDown]
        public void TearDownSelect()
        {
            _select.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void Start_HidesTheScreen()
        {
            Assert.IsFalse(_view.Visible);
            Assert.IsFalse(_select.IsOpen);
        }

        [Test]
        public void Open_ShowsEveryLevelAsTheProgressHasIt()
        {
            _select.Open();

            Assert.IsTrue(_view.Visible);
            Assert.AreEqual(4, _view.Slots.Count);
            Assert.AreEqual(2, _view.Slots[0].Stars);
            Assert.IsTrue(_view.Slots[1].IsNewest);
            Assert.IsFalse(_view.Slots[2].IsOpen);
            Assert.AreEqual(_config.Lives, _view.MaxStars);
        }

        [Test]
        public void Open_AgainAfterAWin_ShowsTheNewProgress()
        {
            _select.Open();
            _select.Close();
            _progress.Unlocked = 3;

            _select.Open();

            Assert.IsTrue(_view.Slots[2].IsOpen);
            Assert.IsTrue(_view.Slots[2].IsNewest);
        }

        [Test]
        public void TapOnAnOpenLevel_ChoosesIt_ALockedOneDoesNothing()
        {
            _select.Open();

            _view.Tap(2);
            _view.Tap(1);

            CollectionAssert.AreEqual(new[] { 1 }, _chosen);
        }

        [Test]
        public void BackButton_ClosesOnce()
        {
            _select.Open();

            _view.ClickBack();
            _select.Close();

            Assert.IsFalse(_view.Visible);
            Assert.AreEqual(1, _closes);
        }

        [Test]
        public void WhileClosed_TapsDoNothing()
        {
            _view.Tap(0);

            Assert.AreEqual(0, _chosen.Count);
        }

        private sealed class FakeLevelSelectView : ILevelSelectView
        {
            public IReadOnlyList<LevelSlot> Slots;
            public int MaxStars;
            public bool Visible;

            public event Action<int> LevelClicked;
            public event Action BackClicked;

            public void Show(IReadOnlyList<LevelSlot> slots, int maxStars)
            {
                Slots = slots;
                MaxStars = maxStars;
                Visible = true;
            }

            public void Hide() => Visible = false;

            public void Tap(int index) => LevelClicked?.Invoke(index);
            public void ClickBack() => BackClicked?.Invoke();
        }
    }
}
