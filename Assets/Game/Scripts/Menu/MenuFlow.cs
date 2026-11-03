using System;
using AF.Core;
using Cysharp.Threading.Tasks;
using HoldIt.Input;
using HoldIt.Levels;
using HoldIt.Settings;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Menu
{
    /// <summary>
    /// The menu scene's screens. Play opens the last open level, Levels the level select (a level chosen there opens),
    /// Settings its screen. Back closes the settings or the level select; on the main menu it leaves the game where there is
    /// no Quit button (mobile) and does nothing on desktop, where Escape quitting at once would surprise.
    /// </summary>
    internal sealed class MenuFlow : IInitializable, IStartable, IDisposable
    {
        private readonly IMainMenuView _main;
        private readonly ILevelSelect _levels;
        private readonly ISettingsScreen _settings;
        private readonly IAppExit _exit;
        private readonly ISceneLoader _scenes;
        private readonly ILevelCursor _cursor;
        private readonly ILevelProgress _progress;
        private readonly ISubscriber<BackPressed> _back;

        private IDisposable _backSubscription;
        private bool _leaving;

        public MenuFlow(IMainMenuView main, ILevelSelect levels, ISettingsScreen settings, IAppExit exit,
            ISceneLoader scenes, ILevelCursor cursor, ILevelProgress progress, ISubscriber<BackPressed> back)
        {
            _main = main;
            _levels = levels;
            _settings = settings;
            _exit = exit;
            _scenes = scenes;
            _cursor = cursor;
            _progress = progress;
            _back = back;
        }

        public void Initialize()
        {
            _main.PlayClicked += PlayLatest;
            _main.LevelsClicked += OpenLevels;
            _main.SettingsClicked += OpenSettings;
            _main.QuitClicked += Quit;
            _levels.LevelChosen += Play;
            _levels.Closed += ShowMain;
            _settings.Closed += ShowMain;
            _backSubscription = _back.Subscribe(_ => Back());
        }

        // In Start, not Initialize: the views set themselves up in Awake, after the scope is built.
        public void Start() => ShowMain();

        public void Dispose()
        {
            _main.PlayClicked -= PlayLatest;
            _main.LevelsClicked -= OpenLevels;
            _main.SettingsClicked -= OpenSettings;
            _main.QuitClicked -= Quit;
            _levels.LevelChosen -= Play;
            _levels.Closed -= ShowMain;
            _settings.Closed -= ShowMain;
            _backSubscription?.Dispose();
        }

        // Read when pressed, not at Start: the menu takes no clicks before the boot steps (the saved progress) are done.
        private void PlayLatest() => Play(_progress.Unlocked - 1);

        private void Play(int index)
        {
            if (_leaving)
                return;
            _leaving = true;
            _cursor.Select(index);
            _scenes.Load<PlayingState>(Scenes.Gameplay).Forget();
        }

        private void OpenLevels()
        {
            if (_leaving)
                return;
            _main.Hide();
            _levels.Open();
        }

        private void OpenSettings()
        {
            if (_leaving)
                return;
            _main.Hide();
            _settings.Open();
        }

        private void Back()
        {
            if (_leaving)
                return;
            if (_settings.IsOpen)
                _settings.Close();
            else if (_levels.IsOpen)
                _levels.Close();
            else if (!_exit.HasQuitButton)
                Quit();
        }

        private void Quit()
        {
            _leaving = true;
            _exit.Quit();
        }

        private void ShowMain() => _main.Show(_exit.HasQuitButton);
    }
}
