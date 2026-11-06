using System;
using HoldIt.Gameplay;
using HoldIt.Levels;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class HudPresenterTests
    {
        private FakeHud _hud;
        private FakeLevelRun _run;
        private FakeLevelCursor _cursor;
        private Broker<ServedChanged> _served;
        private Broker<LivesChanged> _lives;
        private HudPresenter _presenter;

        [SetUp]
        public void SetUpPresenter()
        {
            _hud = new FakeHud();
            _run = new FakeLevelRun { Crowd = 12, Served = 0, Lives = 3, MaxLives = 3 };
            _cursor = new FakeLevelCursor { Index = 4, Count = 10 };
            _served = new Broker<ServedChanged>();
            _lives = new Broker<LivesChanged>();
            _presenter = new HudPresenter(_hud, _run, _cursor, _served.Subscriber, _lives.Subscriber);
            _presenter.Initialize();
        }

        [TearDown]
        public void TearDownPresenter()
        {
            _presenter.Dispose();
            _served.Dispose();
            _lives.Dispose();
        }

        [Test]
        public void Start_ShowsTheLevelTheScoreAndTheLives()
        {
            _presenter.Start();

            Assert.AreEqual(5, _hud.Level);
            Assert.AreEqual((0, 12), _hud.Served);
            Assert.AreEqual((3, 3), _hud.Lives);
        }

        [Test]
        public void Changes_AreShownAsTheyCome()
        {
            _presenter.Start();

            _served.Publish(new ServedChanged(4));
            _lives.Publish(new LivesChanged(2));

            Assert.AreEqual((4, 12), _hud.Served);
            Assert.AreEqual((2, 3), _hud.Lives);
        }

        [Test]
        public void Start_ShowsTheFirstReadingWithoutAnimation()
        {
            _presenter.Start();

            Assert.IsFalse(_hud.ServedAnimated);
            Assert.IsFalse(_hud.LivesAnimated);
        }

        [Test]
        public void Changes_AreAnimated()
        {
            _presenter.Start();

            _served.Publish(new ServedChanged(1));
            _lives.Publish(new LivesChanged(2));

            Assert.IsTrue(_hud.ServedAnimated);
            Assert.IsTrue(_hud.LivesAnimated);
        }

        [Test]
        public void Dispose_StopsFollowing()
        {
            _presenter.Start();
            _presenter.Dispose();

            _served.Publish(new ServedChanged(4));

            Assert.AreEqual((0, 12), _hud.Served);
        }

        private sealed class FakeHud : IGameplayHudView
        {
            public event Action PauseClicked
            {
                add { }
                remove { }
            }

            public int Level { get; private set; }
            public (int, int) Served { get; private set; }
            public (int, int) Lives { get; private set; }
            public void SetPaused(bool paused)
            {
            }

            public bool ServedAnimated { get; private set; }
            public bool LivesAnimated { get; private set; }

            public void ShowLevel(int number) => Level = number;

            public void ShowServed(int served, int crowd, bool animate)
            {
                Served = (served, crowd);
                ServedAnimated = animate;
            }

            public void ShowLives(int lives, int maxLives, bool animate)
            {
                Lives = (lives, maxLives);
                LivesAnimated = animate;
            }
        }
    }
}
