using System;
using AF.Core;
using Cysharp.Threading.Tasks;
using HoldIt.Input;
using HoldIt.Levels;
using HoldIt.Settings;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// Pausing: the HUD's pause button or back pauses the game and opens the pause screen; back or Resume closes it,
    /// Settings opens the settings screen in its place (the game stays paused, and closing the settings brings the pause
    /// screen back), and Menu leaves for the menu. Back closes the topmost screen. Once the level has ended, its result
    /// screen takes over (<see cref="LevelEndFlow"/>).
    /// </summary>
    internal sealed class GameplayFlow : IInitializable, IStartable, IDisposable
    {
        private const string PAUSE_REASON = "pause screen";

        private readonly IGameplayHudView _hud;
        private readonly IPauseView _pauseView;
        private readonly ISettingsScreen _settings;
        private readonly IPauseService _pause;
        private readonly ILevelRun _run;
        private readonly ISceneLoader _scenes;
        private readonly ISubscriber<BackPressed> _back;

        private IDisposable _backSubscription;
        private IDisposable _paused;
        private bool _leaving;

        public GameplayFlow(IGameplayHudView hud, IPauseView pauseView, ISettingsScreen settings, IPauseService pause,
            ILevelRun run, ISceneLoader scenes, ISubscriber<BackPressed> back)
        {
            _hud = hud;
            _pauseView = pauseView;
            _settings = settings;
            _pause = pause;
            _run = run;
            _scenes = scenes;
            _back = back;
        }

        public bool IsPaused => _paused != null;

        public void Initialize()
        {
            _hud.PauseClicked += Open;
            _pauseView.ResumeClicked += Close;
            _pauseView.SettingsClicked += OpenSettings;
            _pauseView.MenuClicked += ToMenu;
            _settings.Closed += ShowPauseScreen;
            _backSubscription = _back.Subscribe(_ => Back());
        }

        // In Start, not Initialize: the views set themselves up in Awake, after the scope is built.
        public void Start() => _pauseView.Hide();

        public void Dispose()
        {
            _hud.PauseClicked -= Open;
            _pauseView.ResumeClicked -= Close;
            _pauseView.SettingsClicked -= OpenSettings;
            _pauseView.MenuClicked -= ToMenu;
            _settings.Closed -= ShowPauseScreen;
            _backSubscription?.Dispose();
            _paused?.Dispose();
            _paused = null;
        }

        private void Back()
        {
            if (_settings.IsOpen)
                _settings.Close();
            else if (IsPaused)
                Close();
            else
                Open();
        }

        private void Open()
        {
            if (_leaving || IsPaused || _run.Outcome != LevelOutcome.Playing)
                return;
            _paused = _pause.Request(PAUSE_REASON);
            _hud.SetPaused(true);
            _pauseView.Show();
        }

        private void Close()
        {
            if (_leaving || !IsPaused || _settings.IsOpen)
                return;
            _paused.Dispose();
            _paused = null;
            _hud.SetPaused(false);
            _pauseView.Hide();
        }

        private void OpenSettings()
        {
            if (_leaving || !IsPaused || _settings.IsOpen)
                return;
            _pauseView.Hide();
            _settings.Open();
        }

        private void ShowPauseScreen()
        {
            if (!_leaving && IsPaused)
                _pauseView.Show();
        }

        // The game stays paused until the scene is gone.
        private void ToMenu()
        {
            if (_leaving || !IsPaused || _settings.IsOpen)
                return;
            _leaving = true;
            _scenes.Load<MenuState>(Scenes.Menu).Forget();
        }
    }
}
