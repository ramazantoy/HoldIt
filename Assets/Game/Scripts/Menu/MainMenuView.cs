using System;
using AF.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HoldIt.Menu
{
    /// <summary>The main menu screen (MainMenuScreen prefab). Forwards the clicks; MenuFlow decides what they do.</summary>
    public sealed class MainMenuView : MonoBehaviour, IMainMenuView
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
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        // Awake, not OnEnable: the listeners stay through Show and Hide, which toggle this GameObject.
        private void Awake()
        {
            _play.onClick.AddListener(() => PlayClicked?.Invoke());
            _settings.onClick.AddListener(() => SettingsClicked?.Invoke());
            _quit.onClick.AddListener(() => QuitClicked?.Invoke());
        }
    }
}
