using System;
using AF.Core;
using Cysharp.Threading.Tasks;
using HoldIt.Input;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Menu
{
    /// <summary>
    /// The menu scene's screens. Play opens the game, Settings opens its screen. Back closes the settings screen; on the main
    /// menu it leaves the game where there is no Quit button (mobile) and does nothing on desktop, where Escape quitting at
    /// once would surprise.
    /// </summary>
    internal sealed class MenuFlow : IInitializable, IStartable, IDisposable
    {
        private readonly IMainMenuView _main;
        private readonly ISettingsView _settings;
        private readonly IAppExit _exit;
        private readonly ISceneLoader _scenes;
        private readonly ISubscriber<BackPressed> _back;

        private IDisposable _backSubscription;
        private bool _settingsOpen;
        private bool _leaving;

        public MenuFlow(IMainMenuView main, ISettingsView settings, IAppExit exit, ISceneLoader scenes, ISubscriber<BackPressed> back)
        {
            _main = main;
            _settings = settings;
            _exit = exit;
            _scenes = scenes;
            _back = back;
        }

        public void Initialize()
        {
            _main.PlayClicked += Play;
            _main.SettingsClicked += OpenSettings;
            _main.QuitClicked += Quit;
            _settings.BackClicked += CloseSettings;
            _backSubscription = _back.Subscribe(_ => Back());
        }

        // In Start, not Initialize: the views find their elements in OnEnable, after the scope is built.
        public void Start() => ShowMain();

        public void Dispose()
        {
            _main.PlayClicked -= Play;
            _main.SettingsClicked -= OpenSettings;
            _main.QuitClicked -= Quit;
            _settings.BackClicked -= CloseSettings;
            _backSubscription?.Dispose();
        }

        private void Play()
        {
            if (_leaving)
                return;
            _leaving = true;
            _scenes.Load<PlayingState>(Scenes.Gameplay).Forget();
        }

        private void OpenSettings()
        {
            _settingsOpen = true;
            _main.Hide();
            _settings.Show();
        }

        private void CloseSettings() => ShowMain();

        private void Back()
        {
            if (_leaving)
                return;
            if (_settingsOpen)
                CloseSettings();
            else if (!_exit.HasQuitButton)
                Quit();
        }

        private void Quit()
        {
            _leaving = true;
            _exit.Quit();
        }

        private void ShowMain()
        {
            _settingsOpen = false;
            _settings.Hide();
            _main.Show(_exit.HasQuitButton);
        }
    }
}
