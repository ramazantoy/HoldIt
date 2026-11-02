using System;
using HoldIt.Menu;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class MenuFlowTests
    {
        private FakeMainMenu _main;
        private FakeSettings _settings;
        private FakeExit _exit;
        private FakeSceneLoader _scenes;
        private BackBroker _back;
        private MenuFlow _flow;

        private void Start(bool desktop)
        {
            _main = new FakeMainMenu();
            _settings = new FakeSettings();
            _exit = new FakeExit { HasQuitButton = desktop };
            _scenes = new FakeSceneLoader();
            _back = new BackBroker();
            _flow = new MenuFlow(_main, _settings, _exit, _scenes, _back.Subscriber);
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
            Assert.IsFalse(_settings.Visible);
        }

        [Test]
        public void Back_ClosesTheSettingsScreen()
        {
            Start(desktop: false);
            _main.ClickSettings();
            Assert.IsTrue(_settings.Visible);
            Assert.IsFalse(_main.Visible);

            _back.Press();

            Assert.IsFalse(_settings.Visible);
            Assert.IsTrue(_main.Visible);
            Assert.AreEqual(0, _exit.Quits, "Back from the settings screen only closes it.");
        }

        [Test]
        public void SettingsBackButton_ReturnsToTheMainMenu()
        {
            Start(desktop: true);
            _main.ClickSettings();

            _settings.ClickBack();

            Assert.IsFalse(_settings.Visible);
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
        public void Play_LoadsTheGameOnce()
        {
            Start(desktop: false);

            _main.ClickPlay();
            _main.ClickPlay();
            _back.Press();

            CollectionAssert.AreEqual(new[] { "PlayingState Gameplay" }, _scenes.Loads);
            Assert.AreEqual(0, _exit.Quits, "Back while the game loads does nothing.");
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
            public event Action SettingsClicked;
            public event Action QuitClicked;

            public void ClickPlay() => PlayClicked?.Invoke();
            public void ClickSettings() => SettingsClicked?.Invoke();
            public void ClickQuit() => QuitClicked?.Invoke();

            public void Show(bool quitVisible)
            {
                Visible = true;
                QuitVisible = quitVisible;
            }

            public void Hide() => Visible = false;
        }

        private sealed class FakeSettings : ISettingsView
        {
            public bool Visible;

            public event Action BackClicked;

            public void ClickBack() => BackClicked?.Invoke();

            public void Show() => Visible = true;
            public void Hide() => Visible = false;
        }

        private sealed class FakeExit : IAppExit
        {
            public int Quits;

            public bool HasQuitButton { get; set; }

            public void Quit() => Quits++;
        }
    }
}
