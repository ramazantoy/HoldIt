using System;
using HoldIt.Gameplay;
using HoldIt.Levels;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class GameplayFlowTests
    {
        private FakeHud _hud;
        private FakePauseView _view;
        private FakePause _pause;
        private FakeLevelRun _run;
        private FakeSceneLoader _scenes;
        private BackBroker _back;
        private GameplayFlow _flow;

        [SetUp]
        public void SetUpFlow()
        {
            _hud = new FakeHud();
            _view = new FakePauseView();
            _pause = new FakePause();
            _run = new FakeLevelRun();
            _scenes = new FakeSceneLoader();
            _back = new BackBroker();
            _flow = new GameplayFlow(_hud, _view, _pause, _run, _scenes, _back.Subscriber);
            _flow.Initialize();
            _flow.Start();
        }

        [TearDown]
        public void TearDownFlow()
        {
            _flow.Dispose();
            _back.Dispose();
        }

        [Test]
        public void Start_HidesThePauseScreen()
        {
            Assert.IsFalse(_view.Visible);
            Assert.AreEqual(1, _view.Hides);
            Assert.IsFalse(_pause.IsPaused);
        }

        [Test]
        public void PauseButton_PausesAndShowsTheScreen()
        {
            _hud.ClickPause();

            Assert.IsTrue(_pause.IsPaused);
            Assert.IsTrue(_view.Visible);
            Assert.IsTrue(_flow.IsPaused);
        }

        [Test]
        public void PauseButton_Twice_PausesOnce()
        {
            _hud.ClickPause();
            _hud.ClickPause();

            Assert.AreEqual(1, _pause.ActiveReasons.Count);
        }

        [Test]
        public void Back_OpensThePauseScreenAndBackAgainClosesIt()
        {
            _back.Press();
            Assert.IsTrue(_pause.IsPaused);
            Assert.IsTrue(_view.Visible);

            _back.Press();
            Assert.IsFalse(_pause.IsPaused);
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void Resume_UnpausesAndHidesTheScreen()
        {
            _hud.ClickPause();

            _view.ClickResume();

            Assert.IsFalse(_pause.IsPaused);
            Assert.IsFalse(_view.Visible);
            Assert.IsFalse(_flow.IsPaused);
        }

        [Test]
        public void Menu_LoadsTheMenuOnceAndStaysPaused()
        {
            _hud.ClickPause();

            _view.ClickMenu();
            _view.ClickMenu();
            _back.Press();

            CollectionAssert.AreEqual(new[] { "MenuState Menu" }, _scenes.Loads);
            Assert.IsTrue(_pause.IsPaused);
        }

        [Test]
        public void Menu_WithoutThePauseScreen_DoesNothing()
        {
            _view.ClickMenu();

            Assert.AreEqual(0, _scenes.Loads.Count);
        }

        [Test]
        public void AfterTheLevelEnded_NeitherButtonNorBackPauses()
        {
            _run.Outcome = LevelOutcome.Lost;

            _hud.ClickPause();
            Assert.IsFalse(_pause.IsPaused, "The pause button");
            Assert.IsFalse(_view.Visible, "The pause button");

            _back.Press();
            Assert.IsFalse(_pause.IsPaused, "Back");
            Assert.IsFalse(_view.Visible, "Back");
        }

        [Test]
        public void Dispose_ReleasesThePause()
        {
            _hud.ClickPause();

            _flow.Dispose();

            Assert.IsFalse(_pause.IsPaused);
        }

        private sealed class FakeHud : IGameplayHudView
        {
            public event Action PauseClicked;

            public void ShowLevel(int number)
            {
            }

            public void ShowServed(int served, int crowd)
            {
            }

            public void ShowLives(int lives, int maxLives)
            {
            }

            public void ClickPause() => PauseClicked?.Invoke();
        }

        private sealed class FakePauseView : IPauseView
        {
            public event Action ResumeClicked;
            public event Action MenuClicked;

            public bool Visible { get; private set; }
            public int Hides { get; private set; }

            public void Show() => Visible = true;

            public void Hide()
            {
                Visible = false;
                Hides++;
            }

            public void ClickResume() => ResumeClicked?.Invoke();
            public void ClickMenu() => MenuClicked?.Invoke();
        }
    }
}
