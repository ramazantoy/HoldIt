using System;
using HoldIt.Menu;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class MenuFlowTests
    {
        private FakeMainMenu _main;
        private FakeLevelSelect _levels;
        private FakeSettingsScreen _settings;
        private FakeExit _exit;
        private FakeSceneLoader _scenes;
        private FakeLevelCursor _cursor;
        private FakeLevelProgress _progress;
        private BackBroker _back;
        private MenuFlow _flow;

        private void Start(bool desktop)
        {
            _main = new FakeMainMenu();
            _levels = new FakeLevelSelect();
            _settings = new FakeSettingsScreen();
            _exit = new FakeExit { HasQuitButton = desktop };
            _scenes = new FakeSceneLoader();
            _cursor = new FakeLevelCursor { Count = 5 };
            _progress = new FakeLevelProgress { Unlocked = 3 };
            _cursor.Open = _progress.Unlocked;
            _back = new BackBroker();
            _flow = new MenuFlow(_main, _levels, _settings, _exit, _scenes, _cursor, _progress, _back.Subscriber);
            _flow.Initialize();
            _flow.Start();
        }

        [TearDown]
        public void TearDownFlow()
        {
            _flow.Dispose();
            _back.Dispose();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Start_ShowsTheMainMenu_WithQuitOnlyOnDesktop(bool desktop)
        {
            Start(desktop);

            Assert.IsTrue(_main.Visible);
            Assert.AreEqual(desktop, _main.QuitVisible);
            Assert.IsFalse(_levels.IsOpen);
            Assert.IsFalse(_settings.IsOpen);
        }

        [Test]
        public void Play_OpensTheLastOpenLevelOnce()
        {
            Start(desktop: false);

            _main.ClickPlay();
            _main.ClickPlay();
            _back.Press();

            Assert.AreEqual(2, _cursor.Index);
            CollectionAssert.AreEqual(new[] { "PlayingState Gameplay" }, _scenes.Loads);
            Assert.AreEqual(0, _exit.Quits, "Back while the game loads does nothing.");
        }

        [Test]
        public void Levels_OpensTheLevelSelect()
        {
            Start(desktop: false);

            _main.ClickLevels();

            Assert.IsTrue(_levels.IsOpen);
            Assert.IsFalse(_main.Visible);
        }

        [Test]
        public void ALevelChosen_OpensThatLevelOnce()
        {
            Start(desktop: false);
            _main.ClickLevels();

            _levels.Choose(1);
            _levels.Choose(2);

            Assert.AreEqual(1, _cursor.Index);
            CollectionAssert.AreEqual(new[] { "PlayingState Gameplay" }, _scenes.Loads);
        }

        [Test]
        public void Back_ClosesTheLevelSelect()
        {
            Start(desktop: false);
            _main.ClickLevels();

            _back.Press();

            Assert.IsFalse(_levels.IsOpen);
            Assert.IsTrue(_main.Visible);
            Assert.AreEqual(0, _exit.Quits, "Back from the level select only closes it.");
        }

        [Test]
        public void Settings_OpensItsScreen_AndBackClosesIt()
        {
            Start(desktop: false);
            _main.ClickSettings();
            Assert.IsTrue(_settings.IsOpen);
            Assert.IsFalse(_main.Visible);

            _back.Press();

            Assert.IsFalse(_settings.IsOpen);
            Assert.IsTrue(_main.Visible);
            Assert.AreEqual(0, _exit.Quits, "Back from the settings screen only closes it.");
        }

        [Test]
        public void SettingsClosedItself_ReturnsToTheMainMenu()
        {
            Start(desktop: true);
            _main.ClickSettings();

            _settings.Close();

            Assert.IsTrue(_main.Visible);
        }

        [TestCase(false, 1)]
        [TestCase(true, 0)]
        public void BackOnTheMainMenu_QuitsOnlyWithoutAQuitButton(bool desktop, int quits)
        {
            Start(desktop);

            _back.Press();

            Assert.AreEqual(quits, _exit.Quits);
        }

        [Test]
        public void QuitButton_Quits()
        {
            Start(desktop: true);

            _main.ClickQuit();

            Assert.AreEqual(1, _exit.Quits);
        }

        private sealed class FakeMainMenu : IMainMenuView
        {
            public bool Visible;
            public bool QuitVisible;

            public event Action PlayClicked;
            public event Action LevelsClicked;
            public event Action SettingsClicked;
            public event Action QuitClicked;

            public void ClickPlay() => PlayClicked?.Invoke();
            public void ClickLevels() => LevelsClicked?.Invoke();
            public void ClickSettings() => SettingsClicked?.Invoke();
            public void ClickQuit() => QuitClicked?.Invoke();

            public void Show(bool quitVisible)
            {
                Visible = true;
                QuitVisible = quitVisible;
            }

            public void Hide() => Visible = false;
        }

        private sealed class FakeLevelSelect : ILevelSelect
        {
            public bool IsOpen { get; private set; }

            public event Action Closed;
            public event Action<int> LevelChosen;

            public void Open() => IsOpen = true;

            public void Close()
            {
                IsOpen = false;
                Closed?.Invoke();
            }

            public void Choose(int index) => LevelChosen?.Invoke(index);
        }

        private sealed class FakeExit : IAppExit
        {
            public int Quits;

            public bool HasQuitButton { get; set; }

            public void Quit() => Quits++;
        }
    }
}
