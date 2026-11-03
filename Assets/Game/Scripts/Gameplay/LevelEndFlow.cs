using System;
using AF.Core;
using AF.Input;
using Cysharp.Threading.Tasks;
using HoldIt.Input;
using HoldIt.Levels;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// A level's end. As it ends, drawing stops (an input mode without the gameplay map) and the board plays on a moment,
    /// so the last accident is seen; then the game pauses under the result screen. Next moves on to the next level when the
    /// progress has it open (a win opens it; a level played again may have it open already, lost or won), and Retry plays
    /// this one again, both by loading the gameplay scene anew; Menu, or back, leaves for the menu. A win is in the
    /// progress by the time the screen shows (<see cref="ProgressRecorder"/> keeps it as the level ends).
    /// </summary>
    internal sealed class LevelEndFlow : IInitializable, IStartable, ITickable, IDisposable
    {
        private const string PAUSE_REASON = "level result";

        private readonly ILevelResultView _view;
        private readonly ILevelRun _run;
        private readonly ILevelCursor _cursor;
        private readonly RunConfig _config;
        private readonly IGameClock _clock;
        private readonly IPauseService _pause;
        private readonly IInputModeService _input;
        private readonly ISceneLoader _scenes;
        private readonly ISubscriber<LevelEnded> _ended;
        private readonly ISubscriber<BackPressed> _back;

        private IDisposable _subscriptions;
        private IDisposable _inputMode;
        private IDisposable _paused;
        private bool _hasEnded;
        private float _wait;
        private bool _leaving;

        public LevelEndFlow(ILevelResultView view, ILevelRun run, ILevelCursor cursor, RunConfig config, IGameClock clock,
            IPauseService pause, IInputModeService input, ISceneLoader scenes, ISubscriber<LevelEnded> ended,
            ISubscriber<BackPressed> back)
        {
            _view = view;
            _run = run;
            _cursor = cursor;
            _config = config;
            _clock = clock;
            _pause = pause;
            _input = input;
            _scenes = scenes;
            _ended = ended;
            _back = back;
        }

        /// <summary>True once the result screen is up.</summary>
        public bool IsShowing => _paused != null;

        public void Initialize()
        {
            _view.NextClicked += Next;
            _view.RetryClicked += Retry;
            _view.MenuClicked += ToMenu;
            var bag = DisposableBag.CreateBuilder();
            _ended.Subscribe(_ => End()).AddTo(bag);
            _back.Subscribe(_ => ToMenu()).AddTo(bag);
            _subscriptions = bag.Build();
        }

        // In Start, not Initialize: the views set themselves up in Awake, after the scope is built.
        public void Start() => _view.Hide();

        public void Tick()
        {
            if (!_hasEnded || IsShowing)
                return;
            _wait -= _clock.DeltaTime;
            if (_wait > 0f)
                return;

            _paused = _pause.Request(PAUSE_REASON);
            _view.Show(new LevelResult(_run.Outcome, _cursor.Number, _run.Stars, _run.MaxLives, _run.Served, _run.Crowd,
                _cursor.HasNext, _cursor.IsNextOpen));
        }

        public void Dispose()
        {
            _view.NextClicked -= Next;
            _view.RetryClicked -= Retry;
            _view.MenuClicked -= ToMenu;
            _subscriptions?.Dispose();
            _inputMode?.Dispose();
            _paused?.Dispose();
        }

        private void End()
        {
            if (_hasEnded)
                return;
            _hasEnded = true;
            _wait = _config.EndDelay;
            _inputMode = _input.Request(InputModes.Menu);
        }

        private void Next()
        {
            if (!CanLeave() || !_cursor.IsNextOpen)
                return;
            _leaving = true;
            _cursor.Next();
            _scenes.Load(Scenes.Gameplay).Forget();
        }

        private void Retry()
        {
            if (!CanLeave())
                return;
            _leaving = true;
            _scenes.Load(Scenes.Gameplay).Forget();
        }

        private void ToMenu()
        {
            if (!CanLeave())
                return;
            _leaving = true;
            _scenes.Load<MenuState>(Scenes.Menu).Forget();
        }

        private bool CanLeave() => IsShowing && !_leaving;
    }
}
