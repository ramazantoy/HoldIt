using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using VContainer;

namespace AF.Core.Tests
{
    public class GameManagerTests
    {
        private IObjectResolver _container;
        private IGameManager _game;
        private IDisposable _subscription;
        private List<GameStateChanged> _published;

        [SetUp]
        public void SetUp()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<GameStateChanged>(messagePipe);
            builder.Register<BootState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<MenuState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<PlayingState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<GatedState>(Lifetime.Singleton).As<IGameState>().AsSelf();
            builder.Register<FailingState>(Lifetime.Singleton).As<IGameState>().AsSelf();
            builder.Register<IGameManager, GameManager>(Lifetime.Singleton);
            _container = builder.Build();

            _game = _container.Resolve<IGameManager>();
            _published = new List<GameStateChanged>();
            _subscription = _container.Resolve<ISubscriber<GameStateChanged>>().Subscribe(_published.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _subscription.Dispose();
            _container.Dispose();
        }

        [Test]
        public void StartsInBoot()
        {
            Assert.IsInstanceOf<BootState>(_game.Current);
            Assert.IsFalse(_game.IsTransitioning);
        }

        [Test]
        public void GoTo_EntersState_ThenPublishesPreviousAndCurrent()
        {
            _game.GoTo<MenuState>().GetAwaiter().GetResult();

            Assert.IsInstanceOf<MenuState>(_game.Current);
            Assert.AreEqual(1, _published.Count);
            Assert.IsInstanceOf<BootState>(_published[0].Previous);
            Assert.IsInstanceOf<MenuState>(_published[0].Current);
        }

        [Test]
        public void GoTo_CurrentState_DoesNothing()
        {
            _game.GoTo<BootState>().GetAwaiter().GetResult();

            Assert.IsInstanceOf<BootState>(_game.Current);
            Assert.IsEmpty(_published);
        }

        [Test]
        public void GoTo_UnregisteredState_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _game.GoTo<UnregisteredState>().GetAwaiter().GetResult());
            Assert.IsInstanceOf<BootState>(_game.Current);
        }

        [Test]
        public void GoTo_WhileTransitioning_Throws()
        {
            var gated = _container.Resolve<GatedState>();
            var first = _game.GoTo<GatedState>();
            Assert.IsTrue(_game.IsTransitioning);

            Assert.Throws<InvalidOperationException>(() => _game.GoTo<MenuState>().GetAwaiter().GetResult());
            Assert.IsEmpty(_published, "Nothing is published before the running transition has entered its state.");

            gated.Open();
            first.GetAwaiter().GetResult();

            Assert.IsFalse(_game.IsTransitioning);
            Assert.AreSame(gated, _game.Current);
            Assert.AreEqual(1, _published.Count);
        }

        [Test]
        public void EnterThrows_TheGameIsInTheNewState_PublishedAndRethrown()
        {
            var failing = _container.Resolve<FailingState>();
            failing.EnterFails = true;

            Assert.Throws<InvalidOperationException>(() => _game.GoTo<FailingState>().GetAwaiter().GetResult());

            Assert.AreSame(failing, _game.Current);
            Assert.IsFalse(_game.IsTransitioning);
            Assert.AreEqual(1, _published.Count, "Subscribers follow Current although Enter failed.");
            Assert.AreSame(failing, _published[0].Current);
        }

        [Test]
        public void ExitThrows_TheGameStaysAndNothingIsPublished()
        {
            var failing = _container.Resolve<FailingState>();
            _game.GoTo<FailingState>().GetAwaiter().GetResult();
            _published.Clear();
            failing.ExitFails = true;

            Assert.Throws<InvalidOperationException>(() => _game.GoTo<MenuState>().GetAwaiter().GetResult());

            Assert.AreSame(failing, _game.Current);
            Assert.IsFalse(_game.IsTransitioning);
            Assert.IsEmpty(_published);
        }

        [Test]
        public void DisposedDuringEnter_EndsWithCancellation_AndPublishesNothing()
        {
            var transition = _game.GoTo<GatedState>();

            ((IDisposable)_game).Dispose();

            var thrown = Assert.Catch<OperationCanceledException>(() => transition.GetAwaiter().GetResult());
            Assert.IsTrue(EntryPointErrors.IsCancellation(thrown), "Stopping Play mid-transition is not logged as an error.");
            Assert.IsEmpty(_published);
        }

        private sealed class GatedState : GameState
        {
            private readonly UniTaskCompletionSource _gate = new UniTaskCompletionSource();

            public override UniTask Enter(CancellationToken cancellation) => _gate.Task.AttachExternalCancellation(cancellation);
            public void Open() => _gate.TrySetResult();
        }

        private sealed class FailingState : GameState
        {
            public bool EnterFails;
            public bool ExitFails;

            public override UniTask Enter(CancellationToken cancellation) =>
                EnterFails ? UniTask.FromException(new InvalidOperationException("Enter failed")) : UniTask.CompletedTask;

            public override UniTask Exit(CancellationToken cancellation) =>
                ExitFails ? UniTask.FromException(new InvalidOperationException("Exit failed")) : UniTask.CompletedTask;
        }

        private sealed class UnregisteredState : GameState
        {
        }
    }
}
