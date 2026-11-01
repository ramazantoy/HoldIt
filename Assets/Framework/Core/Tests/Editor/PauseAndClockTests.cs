using System;
using System.Collections.Generic;
using MessagePipe;
using NUnit.Framework;
using VContainer;

namespace AF.Core.Tests
{
    public class PauseAndClockTests
    {
        private IObjectResolver _container;
        private IPauseService _pause;
        private IDisposable _subscription;
        private List<bool> _pauseEvents;
        private GameClock _clock;
        private float _appliedTimeScale;

        [SetUp]
        public void SetUp()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<PauseChanged>(messagePipe);
            builder.Register<IPauseService, PauseService>(Lifetime.Singleton);
            _container = builder.Build();

            _pause = _container.Resolve<IPauseService>();
            _pauseEvents = new List<bool>();
            var pauseChanged = _container.Resolve<ISubscriber<PauseChanged>>();
            _subscription = pauseChanged.Subscribe(message => _pauseEvents.Add(message.IsPaused));
            _clock = new GameClock(_pause, pauseChanged, value => _appliedTimeScale = value);
        }

        [TearDown]
        public void TearDown()
        {
            _clock.Dispose();
            _subscription.Dispose();
            _container.Dispose();
        }

        [Test]
        public void Pause_LastsUntilEveryHandleIsDisposed()
        {
            var menu = _pause.Request("menu");
            var focus = _pause.Request("focus");
            CollectionAssert.AreEqual(new[] { "menu", "focus" }, _pause.ActiveReasons);

            menu.Dispose();
            Assert.IsTrue(_pause.IsPaused);
            CollectionAssert.AreEqual(new[] { "focus" }, _pause.ActiveReasons);

            focus.Dispose();
            Assert.IsFalse(_pause.IsPaused);
            CollectionAssert.AreEqual(new[] { true, false }, _pauseEvents, "PauseChanged fires on the edges only.");
        }

        [Test]
        public void Pause_DisposingAHandleTwice_KeepsOtherRequests()
        {
            var first = _pause.Request("first");
            var second = _pause.Request("second");

            first.Dispose();
            first.Dispose();
            Assert.IsTrue(_pause.IsPaused);

            second.Dispose();
            Assert.IsFalse(_pause.IsPaused);
        }

        [Test]
        public void Pause_WithoutReason_Throws()
        {
            Assert.Throws<ArgumentException>(() => _pause.Request(""));
        }

        [Test]
        public void Clock_AccumulatesScaledTime()
        {
            _clock.Advance(0.25f);
            _clock.Advance(0.5f);

            Assert.AreEqual(0.75f, _clock.Time, 1e-5f);
            Assert.AreEqual(0.5f, _clock.DeltaTime, 1e-5f);
        }

        [Test]
        public void Clock_StopsWhilePaused()
        {
            using (_pause.Request("test"))
            {
                Assert.AreEqual(0f, _appliedTimeScale);
                _clock.Advance(0.5f);
                Assert.AreEqual(0f, _clock.DeltaTime);
                Assert.AreEqual(0f, _clock.Time);
            }

            Assert.AreEqual(1f, _appliedTimeScale);
            _clock.Advance(0.5f);
            Assert.AreEqual(0.5f, _clock.Time, 1e-5f);
        }

        [Test]
        public void Clock_ScaleSurvivesPause()
        {
            _clock.Scale = 0.5f;
            Assert.AreEqual(0.5f, _appliedTimeScale);

            var handle = _pause.Request("test");
            _clock.Scale = 0.25f;
            Assert.AreEqual(0f, _appliedTimeScale, "Changing the scale while paused must not resume.");

            handle.Dispose();
            Assert.AreEqual(0.25f, _appliedTimeScale);
        }

        [Test]
        public void Clock_NegativeScale_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _clock.Scale = -1f);
        }
    }
}
