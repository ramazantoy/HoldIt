using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using VContainer;

namespace AF.Core
{
    public sealed class SceneLoader : ISceneLoader, IDisposable
    {
        private readonly ISceneOperations _operations;
        private readonly IGameManager _game;
        private readonly ILoadingScreen _loadingScreen;
        private readonly IPublisher<SceneLoadingChanged> _loadingChanged;
        private readonly Action<float> _reportProgress;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly IDisposable _subscription;
        private string _current;
        private string _running;
        private bool _disposed;

        [Inject]
        public SceneLoader(IGameManager game, IReadOnlyList<ILoadingScreen> loadingScreens, IPublisher<SceneLoadingChanged> loadingChanged,
            ISubscriber<AppSuspending> suspending)
            : this(new UnitySceneOperations(), game, loadingScreens, loadingChanged, suspending)
        {
        }

        internal SceneLoader(ISceneOperations operations, IGameManager game, IReadOnlyList<ILoadingScreen> loadingScreens,
            IPublisher<SceneLoadingChanged> loadingChanged, ISubscriber<AppSuspending> suspending)
        {
            if (loadingScreens.Count > 1)
            {
                var names = string.Join(", ", loadingScreens.Select(screen => screen.GetType().Name));
                throw new InvalidOperationException($"{loadingScreens.Count} loading screens are registered ({names}). Register one ILoadingScreen in the root scope.");
            }

            _operations = operations;
            _game = game;
            _loadingScreen = loadingScreens.Count == 1 ? loadingScreens[0] : null;
            _loadingChanged = loadingChanged;
            _reportProgress = progress =>
            {
                if (_loadingScreen.IsAlive())
                    _loadingScreen.SetProgress(progress);
            };

            _subscription = suspending.Subscribe(message =>
            {
                if (message.Reason == SuspendReason.Quit)
                    StopForQuit();
            });
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
        }

        public string Current => _current ??= _operations.ActiveScene;
        public bool IsLoading { get; private set; }

        public UniTask Load(string scene)
        {
            ThrowIfBusy(scene);
            CheckInBuild(scene);
            _running = $"loading '{scene}'";
            return Replace(scene, null);
        }

        public UniTask Load<TState>(string scene) where TState : IGameState
        {
            ThrowIfBusy(scene);
            CheckInBuild(scene);
            if (_game.IsTransitioning)
                throw new InvalidOperationException($"Cannot load '{scene}' while the transition from {_game.Current} is still running.");
            _running = $"loading '{scene}'";
            return Replace(scene, () => _game.GoTo<TState>());
        }

        public UniTask LoadAdditive(string scene)
        {
            ThrowIfBusy(scene);
            CheckInBuild(scene);
            if (_operations.IsLoaded(scene))
                throw new InvalidOperationException($"The scene '{scene}' is already loaded.");
            _running = $"adding '{scene}'";
            var main = Current;
            return Run(cancellation => _operations.LoadAdditive(scene, main, cancellation), $"Scene added: {scene}");
        }

        public UniTask UnloadAdditive(string scene)
        {
            ThrowIfBusy(scene);
            if (scene == Current)
                throw new InvalidOperationException($"'{scene}' is the main scene. Replace it with Load instead.");
            if (string.IsNullOrEmpty(scene) || !_operations.IsLoaded(scene))
                throw new InvalidOperationException($"The scene '{scene}' is not loaded.");
            _running = $"removing '{scene}'";
            return Run(cancellation => _operations.Unload(scene, cancellation), $"Scene removed: {scene}");
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _subscription.Dispose();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#endif
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        /// <summary>
        /// Unity refuses scene loads while the app quits, before the root scope is disposed: in the Editor from the moment Play
        /// is asked to stop, a frame before Application.quitting. A change still running then ends as a cancellation.
        /// </summary>
        private void StopForQuit()
        {
            if (!_disposed)
                _lifetime.Cancel();
        }

#if UNITY_EDITOR
        private void OnPlayModeChanged(UnityEditor.PlayModeStateChange change)
        {
            if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                StopForQuit();
        }
#endif

        private async UniTask Replace(string scene, Func<UniTask> enterState)
        {
            var cancellation = _lifetime.Token;
            var unloading = false;
            IsLoading = true;
            try
            {
                _loadingChanged.Publish(new SceneLoadingChanged(true, scene));
                if (_loadingScreen.IsAlive())
                {
                    _loadingScreen.SetProgress(0f);
                    await _loadingScreen.Show(cancellation);
                }

                // The old state exits while its scene still exists; the new scene's entry points start in the new state.
                if (enterState != null)
                    await enterState();

                // Every old scope is disposed (its savables captured) before the new scene's objects wake up, so reloading a
                // scene never registers the same savable key twice, whatever order Unity uses.
                unloading = true;
                await _operations.UnloadAll(cancellation);
                _current = null;
                await _operations.LoadSingle(scene, _reportProgress, cancellation);
                _current = scene;
                Log.Info($"Scene loaded: {scene}");

                if (_loadingScreen.IsAlive())
                    await _loadingScreen.Hide(cancellation);
            }
            catch (Exception) when (!unloading && !cancellation.IsCancellationRequested)
            {
                // Failed before anything unloaded (Show, or the state change): the old scenes are still there, so uncover them.
                // The caller gets the original exception.
                if (_loadingScreen.IsAlive())
                    await _loadingScreen.Hide(cancellation);
                throw;
            }
            finally
            {
                // Once the old scenes are gone, a failed change keeps the loading screen up (the error reaches the caller) but
                // gives input back, so another Load can recover.
                IsLoading = false;
                _running = null;
                // Quitting or disposed: the subscribers are going away too.
                if (!cancellation.IsCancellationRequested)
                    _loadingChanged.Publish(new SceneLoadingChanged(false, scene));
            }
        }

        private async UniTask Run(Func<CancellationToken, UniTask> operation, string done)
        {
            try
            {
                await operation(_lifetime.Token);
                Log.Info(done);
            }
            finally
            {
                _running = null;
            }
        }

        private void ThrowIfBusy(string scene)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SceneLoader));
            _lifetime.Token.ThrowIfCancellationRequested();
            if (_running != null)
                throw new InvalidOperationException($"Cannot change scenes ('{scene}') while {_running} is still running.");
        }

        private void CheckInBuild(string scene)
        {
            if (string.IsNullOrEmpty(scene))
                throw new ArgumentException("A scene name is required.", nameof(scene));
            if (!_operations.IsInBuild(scene))
                throw new ArgumentException($"The scene '{scene}' is not in the build. Add it to the scene list in File > Build Profiles.", nameof(scene));
        }
    }
}
