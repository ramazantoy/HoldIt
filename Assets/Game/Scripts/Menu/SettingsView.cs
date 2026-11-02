using System;
using AF.Core;
using HoldIt.UI;
using UnityEngine;
using UnityEngine.UI;

namespace HoldIt.Menu
{
    /// <summary>The settings screen (SettingsScreen prefab). Only a back button until M7.</summary>
    public sealed class SettingsView : ScreenView, ISettingsView
    {
        [SerializeField, Required] private Button _back;

        public event Action BackClicked;

        public void Show() => ShowScreen();

        public void Hide() => HideScreen();

        private void Awake() => _back.onClick.AddListener(() => BackClicked?.Invoke());
    }
}
