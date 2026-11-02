using System;
using AF.Core;
using HoldIt.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace HoldIt.Menu
{
    /// <summary>The main menu screen (MainMenu.uxml). Forwards the clicks; MenuFlow decides what they do.</summary>
    public sealed class MainMenuView : MonoBehaviour, IMainMenuView
    {
        private const string PLAY_BUTTON = "play-button";
        private const string SETTINGS_BUTTON = "settings-button";
        private const string QUIT_BUTTON = "quit-button";

        [SerializeField, Required] private UIDocument _document;

        private Button _play;
        private Button _settings;
        private Button _quit;

        public event Action PlayClicked;
        public event Action SettingsClicked;
        public event Action QuitClicked;

        public void Show(bool quitVisible)
        {
            _quit.SetVisible(quitVisible);
            _document.rootVisualElement.SetVisible(true);
        }

        public void Hide() => _document.rootVisualElement.SetVisible(false);

        // The UIDocument builds its tree in its own OnEnable, which runs before this one.
        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            SafeArea.Track(root);
            _play = root.Required<Button>(PLAY_BUTTON);
            _settings = root.Required<Button>(SETTINGS_BUTTON);
            _quit = root.Required<Button>(QUIT_BUTTON);
            _play.clicked += OnPlay;
            _settings.clicked += OnSettings;
            _quit.clicked += OnQuit;
        }

        private void OnDisable()
        {
            // Null when OnEnable threw on a renamed element: do not add a second exception to the first.
            if (_play != null)
                _play.clicked -= OnPlay;
            if (_settings != null)
                _settings.clicked -= OnSettings;
            if (_quit != null)
                _quit.clicked -= OnQuit;
        }

        private void OnPlay() => PlayClicked?.Invoke();
        private void OnSettings() => SettingsClicked?.Invoke();
        private void OnQuit() => QuitClicked?.Invoke();
    }
}
