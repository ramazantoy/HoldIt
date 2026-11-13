using System;
using AF.Core;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using VContainer;

namespace AF.Save.Tests
{
    public class SaveLifecycleTests
    {
        private IObjectResolver _container;
        private IGameManager _game;
        private SaveService _save;
        private SaveLifecycle _lifecycle;

        [SetUp]
        public void SetUp()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<GameStateChanged>(messagePipe);
            builder.RegisterMessageBroker<PauseChanged>(messagePipe);
            builder.RegisterMessageBroker<AppSuspending>(messagePipe);
            builder.Register<BootState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<MenuState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<PlayingState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<IGameManager, GameManager>(Lifetime.Singleton);
            builder.Register<IPauseService, PauseService>(Lifetime.Singleton);
            _container = builder.Build();

            var storage = new MemoryStorage();
            _game = _container.Resolve<IGameManager>();
            _save = new SaveService(new SaveRegistry(), storage, new SaveOptions { Storage = storage });
            _lifecycle = new SaveLifecycle(_save, _game, _container.Resolve<IPauseService>(), new IdleSceneLoader(),
                _container.Resolve<ISubscriber<GameStateChanged>>(), _container.Resolve<ISubscriber<AppSuspending>>());
        }

        [TearDown]
        public void TearDown()
        {
            _lifecycle.Dispose();
            _container.Dispose();
        }

        [Test]
        public void ReturningToTheMenu_ClosesTheSlot()
        {
            _save.NewSlot("1");
            GoTo<PlayingState>();
            Assert.AreEqual("1", _save.ActiveSlot);

            GoTo<MenuState>();

            Assert.IsFalse(_save.HasSession, "A slot left open would be written by the next suspend autosave.");
            Assert.IsNull(_save.ActiveSlot);
        }

        [Test]
        public void GameplayWithoutASlot_StartsADevSession_ThatTheMenuCloses()
        {
            GoTo<PlayingState>();
            Assert.IsTrue(_save.IsDevSession);

            GoTo<MenuState>();

            Assert.IsFalse(_save.HasSession);
            Assert.IsFalse(_save.IsDevSession);
        }

        [Test]
        public void GameplayWithoutSlots_StartsNoSession()
        {
            _lifecycle.Dispose();
            var storage = new MemoryStorage();
            _save = new SaveService(new SaveRegistry(), storage, new SaveOptions { Storage = storage, UsesSlots = false });
            _lifecycle = new SaveLifecycle(_save, _game, _container.Resolve<IPauseService>(), new IdleSceneLoader(),
                _container.Resolve<ISubscriber<GameStateChanged>>(), _container.Resolve<ISubscriber<AppSuspending>>());

            GoTo<PlayingState>();

            Assert.IsFalse(_save.HasSession);
            Assert.IsFalse(_save.IsDevSession);

            GoTo<MenuState>();

            Assert.IsFalse(_save.HasSession);
        }

        [Test]
        public void MenuAtBoot_HasNoSessionToClose()
        {
            GoTo<MenuState>();

            Assert.IsFalse(_save.HasSession);
        }

        private void GoTo<TState>() where TState : IGameState => _game.GoTo<TState>().GetAwaiter().GetResult();

        /// <summary>A scene loader that is never loading.</summary>
        private sealed class IdleSceneLoader : ISceneLoader
        {
            public string Current => "Test";
            public bool IsLoading => false;

            public UniTask Load(string scene) => throw new NotSupportedException();
            public UniTask Load<TState>(string scene) where TState : IGameState => throw new NotSupportedException();
            public UniTask LoadAdditive(string scene) => throw new NotSupportedException();
            public UniTask UnloadAdditive(string scene) => throw new NotSupportedException();
        }
    }
}
