using System;
using AF.Core;
using HoldIt.UI;
using UnityEngine;
using UnityEngine.UI;

namespace HoldIt.Gameplay
{
    /// <summary>The pause screen (PauseScreen prefab).</summary>
    public sealed class PauseView : ScreenView, IPauseView
    {
        [SerializeField, Required] private Button _resume;
        [SerializeField, Required] private Button _settings;
        [SerializeField, Required] private Button _menu;

        public event Action ResumeClicked;
        public event Action SettingsClicked;
        public event Action MenuClicked;

        public void Show() => ShowScreen();

        public void Hide() => HideScreen();

        private void Awake()
        {
            _resume.onClick.AddListener(() => ResumeClicked?.Invoke());
            _settings.onClick.AddListener(() => SettingsClicked?.Invoke());
            _menu.onClick.AddListener(() => MenuClicked?.Invoke());
        }
    }
}
