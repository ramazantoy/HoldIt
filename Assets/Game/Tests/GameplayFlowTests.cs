using System;
using HoldIt.Gameplay;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class GameplayFlowTests
    {
        private FakeHud _hud;
        private FakeSceneLoader _scenes;
        private BackBroker _back;
        private GameplayFlow _flow;

        [SetUp]
        public void SetUpFlow()
        {
            _hud = new FakeHud();
            _scenes = new FakeSceneLoader();
            _back = new BackBroker();
            _flow = new GameplayFlow(_hud, _scenes, _back.Subscriber);
            _flow.Initialize();
        }

        [TearDown]
        public void TearDownFlow()
        {
            _flow.Dispose();
            _back.Dispose();
        }

        [Test]
        public void MenuButton_LoadsTheMenuOnce()
        {
            _hud.ClickMenu();
            _hud.ClickMenu();
            _back.Press();

            CollectionAssert.AreEqual(new[] { "MenuState Menu" }, _scenes.Loads);
        }

        [Test]
        public void Back_LoadsTheMenu()
        {
            _back.Press();

            CollectionAssert.AreEqual(new[] { "MenuState Menu" }, _scenes.Loads);
        }

        private sealed class FakeHud : IGameplayHudView
        {
            public event Action MenuClicked;

            public void ClickMenu() => MenuClicked?.Invoke();
        }
    }
}
