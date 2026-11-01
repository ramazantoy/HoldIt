using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using VContainer;

namespace AF.Core.Tests
{
    public class SceneLoaderTests
    {
        private readonly List<string> _log = new List<string>();
        private IObjectResolver _container;
        private IGameManager _game;
        private FakeOperations _operations;
        private FakeLoadingScreen _screen;
        private SceneLoader _loader;
        private IDisposable _subscriptions;

        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<GameStateChanged>(messagePipe);
            builder.RegisterMessageBroker<SceneLoadingChanged>(messagePipe);
            builder.RegisterMessageBroker<AppSuspending>(messagePipe);
            builder.Register<BootState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<MenuState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<PlayingState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<IGameManager, GameManager>(Lifetime.Singleton);
            _container = builder.Build();

            _game = _container.Resolve<IGameManager>();
            _operations = new FakeOperations(_log);
            _screen = new FakeLoadingScreen(_log);
            _loader = Create(_screen);

            var bag = DisposableBag.CreateBuilder();
            _container.Resolve<ISubscriber<GameStateChanged>>().Subscribe(change => _log.Add($"state {change.Previous}>{change.Current}")).AddTo(bag);
            _container.Resolve<ISubscriber<SceneLoadingChanged>>().Subscribe(change => _log.Add($"loading {change.IsLoading}")).AddTo(bag);
            _subscriptions = bag.Build();
        }

        [TearDown]
        public void TearDown()
        {
            _subscriptions.Dispose();
            _loader.Dispose();
            _container.Dispose();
        }

        [Test]
        public void LoadWithState_EntersTheStateUnderTheScreen_BeforeTheOldSceneUnloads()
        {
            _loader.Load<MenuState>("Menu").GetAwaiter().GetResult();

            CollectionAssert.AreEqual(
                new[] { "loading True", "show", "state BootState>MenuState", "unload all", "load Menu", "hide", "loading False" }, _log);
            Assert.IsInstanceOf<MenuState>(_game.Current);
            Assert.AreEqual("Menu", _loader.Current);
            Assert.IsFalse(_loader.IsLoading);
            CollectionAssert.AreEqual(new[] { 0f, 0.5f, 1f }, _screen.Progress);
        }

        [Test]
        public void Load_KeepsTheState()
        {
            _loader.Load("Level").GetAwaiter().GetResult();

            CollectionAssert.AreEqual(new[] { "loading True", "show", "unload all", "load Level", "hide", "loading False" }, _log);
            Assert.IsInstanceOf<BootState>(_game.Current);
            Assert.AreEqual("Level", _loader.Current);
        }

        [Test]
        public void Current_IsTheSceneThatPlayStartedIn()
        {
            Assert.AreEqual(FakeOperations.FirstScene, _loader.Current);
        }

        [Test]
        public void AnotherCall_WhileLoading_Throws()
        {
            _operations.Gate = new UniTaskCompletionSource();
            var first = _loader.Load("Level");
            Assert.IsTrue(_loader.IsLoading);

            Assert.Throws<InvalidOperationException>(() => _loader.Load<MenuState>("Menu"));
            Assert.Throws<InvalidOperationException>(() => _loader.LoadAdditive("Hud"));

            _operations.Gate.TrySetResult();
            first.GetAwaiter().GetResult();
            Assert.IsFalse(_loader.IsLoading);
            Assert.IsInstanceOf<BootState>(_game.Current, "The rejected call changed nothing.");
        }

        [Test]
        public void SceneNotInTheBuild_ThrowsBeforeAnythingStarts()
        {
            Assert.Throws<ArgumentException>(() => _loader.Load<MenuState>(FakeOperations.MissingScene));

            CollectionAssert.IsEmpty(_log);
            Assert.IsFalse(_loader.IsLoading);
            Assert.DoesNotThrow(() => _loader.Load("Level").GetAwaiter().GetResult(), "The loader is free again.");
        }

        [Test]
        public void FailedLoad_GivesInputBack_KeepsTheScreen_AndRethrows()
        {
            _operations.LoadFailure = new InvalidOperationException("load failed");

            Assert.Throws<InvalidOperationException>(() => _loader.Load("Level").GetAwaiter().GetResult());

            CollectionAssert.AreEqual(new[] { "loading True", "show", "unload all", "load Level", "loading False" }, _log);
            Assert.IsFalse(_loader.IsLoading);
            Assert.AreEqual(FakeOperations.TransitionScene, _loader.Current, "Current reports where the player really is.");

            _operations.LoadFailure = null;
            _loader.Load<MenuState>("Menu").GetAwaiter().GetResult();
            Assert.AreEqual("Menu", _loader.Current, "Another load recovers.");
        }

        [Test]
        public void FailureBeforeTheOldScenesUnload_UncoversThem()
        {
            Assert.Throws<InvalidOperationException>(() => _loader.Load<UnregisteredState>("Menu").GetAwaiter().GetResult());

            CollectionAssert.AreEqual(new[] { "loading True", "show", "hide", "loading False" }, _log,
                "The old scene is still there: the screen uncovers it and nothing unloads.");
            Assert.IsInstanceOf<BootState>(_game.Current);
            Assert.AreEqual(FakeOperations.FirstScene, _loader.Current);
            Assert.IsFalse(_loader.IsLoading);
        }

        [Test]
        public void FailedAdditiveLoad_FreesTheLoader()
        {
            _operations.AddFailure = new InvalidOperationException("add failed");
            Assert.Throws<InvalidOperationException>(() => _loader.LoadAdditive("Hud").GetAwaiter().GetResult());

            _operations.AddFailure = null;
            Assert.DoesNotThrow(() => _loader.LoadAdditive("Hud").GetAwaiter().GetResult());
        }

        [Test]
        public void WithoutALoadingScreen_ScenesChangeWithoutOne()
        {
            _loader.Dispose();
            _loader = Create();

            _loader.Load("Level").GetAwaiter().GetResult();

            CollectionAssert.AreEqual(new[] { "loading True", "unload all", "load Level", "loading False" }, _log);
        }

        [Test]
        public void TwoLoadingScreens_FailToBuild()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => Create(_screen, new FakeLoadingScreen(_log)));

            StringAssert.Contains(nameof(FakeLoadingScreen), exception.Message);
        }

        [Test]
        public void Additive_LoadsUnderTheMainScene_WithoutTheScreenOrAnInputBlock()
        {
            _loader.LoadAdditive("Hud").GetAwaiter().GetResult();
            Assert.Throws<InvalidOperationException>(() => _loader.LoadAdditive("Hud"), "Already loaded.");
            Assert.Throws<InvalidOperationException>(() => _loader.UnloadAdditive(FakeOperations.FirstScene), "The main scene is replaced with Load.");
            _loader.UnloadAdditive("Hud").GetAwaiter().GetResult();

            CollectionAssert.AreEqual(new[] { $"add Hud under {FakeOperations.FirstScene}", "remove Hud" }, _log);
            Assert.Throws<InvalidOperationException>(() => _loader.UnloadAdditive("Hud"), "No longer loaded.");
        }

        [Test]
        public void DisposedWhileLoading_EndsWithCancellation_AndPublishesNothing()
        {
            _operations.Gate = new UniTaskCompletionSource();
            var load = _loader.Load("Level");

            _loader.Dispose();

            var thrown = Assert.Catch<OperationCanceledException>(() => load.GetAwaiter().GetResult());
            Assert.IsTrue(EntryPointErrors.IsCancellation(thrown), "Stopping Play mid-load is not logged as an error.");
            CollectionAssert.AreEqual(new[] { "loading True", "show", "unload all", "load Level" }, _log,
                "The subscribers are being disposed with the root scope.");
        }

        [Test]
        public void QuittingWhileLoading_EndsWithCancellation_AndRefusesNewLoads()
        {
            _operations.Gate = new UniTaskCompletionSource();
            var load = _loader.Load("Level");

            _container.Resolve<IPublisher<AppSuspending>>().Publish(new AppSuspending(SuspendReason.Background));
            Assert.IsTrue(_loader.IsLoading, "Going to the background does not stop a load.");
            _container.Resolve<IPublisher<AppSuspending>>().Publish(new AppSuspending(SuspendReason.Quit));

            var thrown = Assert.Catch<OperationCanceledException>(() => load.GetAwaiter().GetResult());
            Assert.IsTrue(EntryPointErrors.IsCancellation(thrown), "Stopping Play mid-load is not logged as an error.");
            Assert.IsFalse(_loader.IsLoading);
            Assert.Catch<OperationCanceledException>(() => _loader.Load("Menu"));
        }

        [Test]
        public void OnlyACancelledTokenMakesACancellationQuiet()
        {
            using var source = new CancellationTokenSource();
            source.Cancel();

            Assert.IsTrue(EntryPointErrors.IsCancellation(new OperationCanceledException(source.Token)));
            Assert.IsFalse(EntryPointErrors.IsCancellation(new OperationCanceledException()), "Thrown without a token: a bug.");
            Assert.IsFalse(EntryPointErrors.IsCancellation(new InvalidOperationException()));
        }

        private SceneLoader Create(params ILoadingScreen[] screens) =>
            new SceneLoader(_operations, _game, screens, _container.Resolve<IPublisher<SceneLoadingChanged>>(),
                _container.Resolve<ISubscriber<AppSuspending>>());

        private sealed class FakeOperations : ISceneOperations
        {
            public const string FirstScene = "Start";
            public const string TransitionScene = "Transition";
            public const string MissingScene = "Missing";

            private readonly List<string> _log;
            private readonly HashSet<string> _loaded = new HashSet<string> { FirstScene };

            public FakeOperations(List<string> log) => _log = log;

            public UniTaskCompletionSource Gate { get; set; }
            public Exception LoadFailure { get; set; }
            public Exception AddFailure { get; set; }
            public string ActiveScene { get; private set; } = FirstScene;

            public bool IsInBuild(string scene) => scene != MissingScene;

            public bool IsLoaded(string scene) => _loaded.Contains(scene);

            public UniTask UnloadAll(CancellationToken cancellation)
            {
                _log.Add("unload all");
                _loaded.Clear();
                ActiveScene = TransitionScene;
                return UniTask.CompletedTask;
            }

            public async UniTask LoadSingle(string scene, Action<float> progress, CancellationToken cancellation)
            {
                _log.Add($"load {scene}");
                progress(0.5f);
                if (Gate != null)
                    await Gate.Task.AttachExternalCancellation(cancellation);
                if (LoadFailure != null)
                    throw LoadFailure;
                progress(1f);
                _loaded.Add(scene);
                ActiveScene = scene;
            }

            public UniTask LoadAdditive(string scene, string mainScene, CancellationToken cancellation)
            {
                if (AddFailure != null)
                    return UniTask.FromException(AddFailure);
                _log.Add($"add {scene} under {mainScene}");
                _loaded.Add(scene);
                return UniTask.CompletedTask;
            }

            public UniTask Unload(string scene, CancellationToken cancellation)
            {
                _log.Add($"remove {scene}");
                _loaded.Remove(scene);
                return UniTask.CompletedTask;
            }
        }

        private sealed class UnregisteredState : GameState
        {
        }

        private sealed class FakeLoadingScreen : ILoadingScreen
        {
            private readonly List<string> _log;

            public FakeLoadingScreen(List<string> log) => _log = log;

            public List<float> Progress { get; } = new List<float>();

            public UniTask Show(CancellationToken cancellation)
            {
                _log.Add("show");
                return UniTask.CompletedTask;
            }

            public void SetProgress(float progress) => Progress.Add(progress);

            public UniTask Hide(CancellationToken cancellation)
            {
                _log.Add("hide");
                return UniTask.CompletedTask;
            }
        }
    }
}
