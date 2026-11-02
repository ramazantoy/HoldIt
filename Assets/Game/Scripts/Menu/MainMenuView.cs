using System;
using AF.Core;
using HoldIt.UI;
using UnityEngine;
using UnityEngine.UI;

namespace HoldIt.Menu
{
    /// <summary>The main menu screen (MainMenuScreen prefab). Forwards the clicks; MenuFlow decides what they do.</summary>
    public sealed class MainMenuView : ScreenView, IMainMenuView
    {
        [SerializeField, Required] private Button _play;
        [SerializeField, Required] private Button _settings;
        [SerializeField, Required] private Button _quit;

        public event Action PlayClicked;
        public event Action SettingsClicked;
        public event Action QuitClicked;

        public void Show(bool quitVisible)
        {
            _quit.gameObject.SetActive(quitVisible);
            ShowScreen();
        }

        public void Hide() => HideScreen();

        private void Awake()
        {
            _play.onClick.AddListener(() => PlayClicked?.Invoke());
            _settings.onClick.AddListener(() => SettingsClicked?.Invoke());
            _quit.onClick.AddListener(() => QuitClicked?.Invoke());
        }
    }
}
