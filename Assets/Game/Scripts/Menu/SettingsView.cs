using System;
using AF.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HoldIt.Menu
{
    /// <summary>The settings screen (SettingsScreen prefab). Only a back button until M7.</summary>
    public sealed class SettingsView : MonoBehaviour, ISettingsView
    {
        [SerializeField, Required] private Button _back;

        public event Action BackClicked;

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        // Awake, not OnEnable: the listener stays through Show and Hide, which toggle this GameObject.
        private void Awake() => _back.onClick.AddListener(() => BackClicked?.Invoke());
    }
}
