using System;
using System.Collections.Generic;
using AF.Core;
using MessagePipe;
using NUnit.Framework;
using VContainer;

namespace AF.Platform.Tests
{
    public class OverlayPauseTests
    {
        private IObjectResolver _container;
        private IPauseService _pause;
        private MockPlatformOverlay _overlay;
        private OverlayPause _overlayPause;
        private List<bool> _overlayMessages;
        private IDisposable _subscription;

        [SetUp]
        public void SetUp()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<PauseChanged>(messagePipe);
            builder.RegisterMessageBroker<PlatformOverlayChanged>(messagePipe);
            builder.Register<IPauseService, PauseService>(Lifetime.Singleton);
            builder.Register<MockPlatformOverlay>(Lifetime.Singleton).As<IPlatformOverlay>().AsSelf();
            _container = builder.Build();

            _pause = _container.Resolve<IPauseService>();
            _overlay = _container.Resolve<MockPlatformOverlay>();
            _overlayMessages = new List<bool>();
            _subscription = _container.Resolve<ISubscriber<PlatformOverlayChanged>>().Subscribe(change => _overlayMessages.Add(change.IsOpen));
            _overlayPause = new OverlayPause(_pause, _overlay, _container.Resolve<ISubscriber<PlatformOverlayChanged>>());
            _overlayPause.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _overlayPause.Dispose();
            _subscription.Dispose();
            _container.Dispose();
        }

        [Test]
        public void OpenOverlay_PausesUntilClosed()
        {
            _overlay.SetOpen(true);
            Assert.IsTrue(_pause.IsPaused);
            CollectionAssert.Contains(_pause.ActiveReasons, "platform-overlay");

            _overlay.SetOpen(false);
            Assert.IsFalse(_pause.IsPaused);
        }

        [Test]
        public void ClosingTheOverlay_KeepsAnotherPause()
        {
            using var menu = _pause.Request("pause-menu");
            _overlay.SetOpen(true);
            _overlay.SetOpen(false);

            Assert.IsTrue(_pause.IsPaused);
            CollectionAssert.AreEqual(new[] { "pause-menu" }, _pause.ActiveReasons);
        }

        [Test]
        public void SameState_PublishesOnce()
        {
            _overlay.SetOpen(true);
            _overlay.SetOpen(true);
            _overlay.SetOpen(false);
            _overlay.SetOpen(false);

            CollectionAssert.AreEqual(new[] { true, false }, _overlayMessages);
            Assert.IsFalse(_pause.IsPaused);
        }

        [Test]
        public void Dispose_ReleasesTheRequest()
        {
            _overlay.SetOpen(true);
            _overlayPause.Dispose();

            Assert.IsFalse(_pause.IsPaused);
        }

        [Test]
        public void OverlayOpenAtStart_Pauses()
        {
            _overlayPause.Dispose();
            _overlay.SetOpen(true);
            var late = new OverlayPause(_pause, _overlay, _container.Resolve<ISubscriber<PlatformOverlayChanged>>());

            late.Initialize();

            Assert.IsTrue(_pause.IsPaused);
            late.Dispose();
        }
    }
}
