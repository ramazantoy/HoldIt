using System;
using System.Collections.Generic;
using System.Linq;
using AF.Core;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace AF.Input
{
    public sealed class InputModeService : IInputModeService, IStartable, IDisposable
    {
        private readonly InputActionAsset _actions;
        private readonly IReadOnlyDictionary<Type, InputMode> _stateModes;
        private readonly InputMode _pausedMode;
        private readonly IGameManager _gameManager;
        private readonly IPauseService _pause;
        private readonly ISceneLoader _scenes;
        private readonly IControlSchemeTracker _schemes;
        private readonly IPublisher<InputModeChanged> _modeChanged;
        private readonly Action<CursorLockMode, bool> _applyCursor;
        private readonly HeldButtonFilter _held = new HeldButtonFilter();
        private readonly List<Handle> _handles = new List<Handle>();
        private readonly List<InputMode> _requests = new List<InputMode>();
        private readonly HashSet<InputMode> _checkedModes = new HashSet<InputMode>();
        private readonly IDisposable _subscriptions;
        private InputMode _stateMode = InputMode.None;
        private Handle _pauseHandle;
        private bool _loading;
        private bool _confineCursor;
        private bool _started;
        private bool _applied;
        private bool _applying;
        private bool _applyAgain;
        private bool _disposed;

        [Inject]
        public InputModeService(InputActionAsset actions, InputOptions options, IReadOnlyList<IGameState> states,
            IGameManager gameManager, IPauseService pause, ISceneLoader scenes, IControlSchemeTracker schemes,
            IPublisher<InputModeChanged> modeChanged, ISubscriber<GameStateChanged> stateChanged, ISubscriber<PauseChanged> pauseChanged,
            ISubscriber<SceneLoadingChanged> loadingChanged, ISubscriber<ControlSchemeChanged> schemeChanged)
            : this(actions, options, states, gameManager, pause, scenes, schemes, modeChanged, stateChanged, pauseChanged, loadingChanged,
                schemeChanged,
                (lockMode, visible) =>
                {
                    Cursor.lockState = lockMode;
                    Cursor.visible = visible;
                })
        {
        }

        // Tests pass their own sink so they never touch the Editor's cursor.
        internal InputModeService(InputActionAsset actions, InputOptions options, IReadOnlyList<IGameState> states,
            IGameManager gameManager, IPauseService pause, ISceneLoader scenes, IControlSchemeTracker schemes,
            IPublisher<InputModeChanged> modeChanged, ISubscriber<GameStateChanged> stateChanged, ISubscriber<PauseChanged> pauseChanged,
            ISubscriber<SceneLoadingChanged> loadingChanged, ISubscriber<ControlSchemeChanged> schemeChanged,
            Action<CursorLockMode, bool> applyCursor)
        {
            _actions = actions;
            _gameManager = gameManager;
            _pause = pause;
            _scenes = scenes;
            _schemes = schemes;
            _modeChanged = modeChanged;
            _applyCursor = applyCursor;
            _stateModes = options.StateModes;
            _pausedMode = options.Paused ?? InputMode.None;

            var unmapped = states
                .Select(state => state.GetType())
                .Where(type => type != typeof(BootState) && !_stateModes.ContainsKey(type))
                .Select(type => type.Name)
                .ToList();
            if (unmapped.Count > 0)
            {
                throw new InvalidOperationException(
                    $"No input mode for the game state(s) {string.Join(", ", unmapped)}. Map each one in InputOptions with " +
                    $".ForState<{unmapped[0]}>(mode).");
            }
            CheckMaps(_pausedMode);
            foreach (var mode in _stateModes.Values)
                CheckMaps(mode);

            var bag = DisposableBag.CreateBuilder();
            stateChanged.Subscribe(change =>
            {
                _stateMode = ModeOf(change.Current);
                Apply();
            }).AddTo(bag);
            pauseChanged.Subscribe(change => SetPaused(change.IsPaused)).AddTo(bag);
            loadingChanged.Subscribe(change => SetLoading(change.IsLoading)).AddTo(bag);
            schemeChanged.Subscribe(_ =>
            {
                if (_applied)
                    ApplyCursor(Current);
            }).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public InputMode Current { get; private set; } = InputMode.None;
        public IReadOnlyList<InputMode> Requests => _requests;

        public bool ConfineCursor
        {
            get => _confineCursor;
            set
            {
                if (_confineCursor == value)
                    return;
                _confineCursor = value;
                if (_applied && !_disposed)
                    ApplyCursor(Current);
            }
        }

        public IDisposable Request(InputMode mode)
        {
            if (mode == null)
                throw new ArgumentNullException(nameof(mode));
            if (_disposed)
                throw new ObjectDisposedException(nameof(InputModeService));
            CheckMaps(mode);

            var handle = new Handle(this, mode);
            _handles.Add(handle);
            _requests.Add(mode);
            Apply();
            return handle;
        }

        // After every Awake and OnEnable of the first frame: the UI input module enables its own actions when it is enabled.
        public void Start()
        {
            _stateMode = ModeOf(_gameManager.Current);
            _loading = _scenes.IsLoading;
            SetPaused(_pause.IsPaused);
            _started = true;
            Apply();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _subscriptions.Dispose();
            _held.Dispose();
            _handles.Clear();
            _requests.Clear();
            _actions.Disable();
            _applyCursor(CursorLockMode.None, true);
        }

        private void SetPaused(bool paused)
        {
            if (paused && _pauseHandle == null)
                _pauseHandle = (Handle)Request(_pausedMode);
            else if (!paused && _pauseHandle != null)
            {
                var handle = _pauseHandle;
                _pauseHandle = null;
                handle.Dispose();
            }
        }

        // Nothing takes input while ISceneLoader replaces the scenes, whatever is requested meanwhile (the new scene's Start, a
        // pause); those requests keep their order and apply when the load ends. It ends after the new state is entered, so the
        // previous state's mode does not come back in between.
        private void SetLoading(bool loading)
        {
            if (_loading == loading)
                return;
            _loading = loading;
            Apply();
        }

        private void Release(Handle handle)
        {
            var index = _handles.IndexOf(handle);
            if (index < 0)
                return;
            _handles.RemoveAt(index);
            _requests.RemoveAt(index);
            Apply();
        }

        /// <summary>Enables the maps of the active mode and disables every other map.</summary>
        private void Apply()
        {
            if (!_started || _disposed)
                return;

            // Disabling a map runs canceled callbacks, and publishing runs InputModeChanged handlers; either may request or
            // release a mode. Such a call only marks the work, and this loop applies again once the current pass is done, so
            // the maps, Current and the published changes always follow the latest request in order.
            if (_applying)
            {
                _applyAgain = true;
                return;
            }
            _applying = true;
            try
            {
                do
                {
                    _applyAgain = false;
                    ApplyOnce();
                }
                while (_applyAgain && !_disposed);
            }
            finally
            {
                _applying = false;
            }
        }

        private void ApplyOnce()
        {
            var next = _loading ? InputMode.None : _handles.Count > 0 ? _handles[_handles.Count - 1].Mode : _stateMode;
            if (_applied)
                InputModeAudit.Check(_actions, Current, _held.Contains);

            foreach (var map in _actions.actionMaps)
            {
                if (!next.Enables(map.name))
                {
                    _held.Forget(map);
                    map.Disable();
                    continue;
                }
                foreach (var action in map.actions)
                {
                    if (action.enabled || _held.Contains(action))
                        continue;
                    if (HeldButtonFilter.IsHeld(action))
                        _held.Hold(action);
                    else
                        action.Enable();
                }
            }

            var previous = Current;
            Current = next;
            _applied = true;
            ApplyCursor(next);
            if (previous != next)
            {
                Log.Info($"Input mode: {previous} -> {next}");
                _modeChanged.Publish(new InputModeChanged(previous, next));
            }
        }

        private void ApplyCursor(InputMode mode)
        {
            var unlocked = _confineCursor ? CursorLockMode.Confined : CursorLockMode.None;
            switch (mode.Cursor)
            {
                case CursorState.Locked:
                    _applyCursor(CursorLockMode.Locked, false);
                    break;
                case CursorState.Hidden:
                    _applyCursor(unlocked, false);
                    break;
                default:
                    _applyCursor(unlocked, _schemes.UsesMouse);
                    break;
            }
        }

        private InputMode ModeOf(IGameState state) =>
            state != null && _stateModes.TryGetValue(state.GetType(), out var mode) ? mode : InputMode.None;

        private void CheckMaps(InputMode mode)
        {
            if (!_checkedModes.Add(mode))
                return;
            foreach (var map in mode.Maps)
            {
                // Exact names: FindActionMap ignores case, but a mode enables maps by their exact name.
                if (_actions.actionMaps.Any(existing => existing.name == map))
                    continue;
                _checkedModes.Remove(mode);
                var known = string.Join(", ", _actions.actionMaps.Select(existing => existing.name));
                throw new ArgumentException($"Input mode '{mode}' enables the map '{map}', which {_actions.name} does not have. Its maps: {known}.");
            }
        }

        private sealed class Handle : IDisposable
        {
            private InputModeService _owner;

            public Handle(InputModeService owner, InputMode mode)
            {
                _owner = owner;
                Mode = mode;
            }

            public InputMode Mode { get; }

            public void Dispose()
            {
                var releasing = _owner;
                _owner = null;
                releasing?.Release(this);
            }
        }
    }
}
