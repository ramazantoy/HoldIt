using System;
using AF.Core;
using HoldIt.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace HoldIt.Menu
{
    /// <summary>The settings screen (Settings.uxml). Only a back button until M7.</summary>
    public sealed class SettingsView : MonoBehaviour, ISettingsView
    {
        private const string BACK_BUTTON = "back-button";

        [SerializeField, Required] private UIDocument _document;

        private Button _back;

        public event Action BackClicked;

        public void Show() => _document.rootVisualElement.SetVisible(true);

        public void Hide() => _document.rootVisualElement.SetVisible(false);

        // The UIDocument builds its tree in its own OnEnable, which runs before this one.
        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            SafeArea.Track(root);
            _back = root.Required<Button>(BACK_BUTTON);
            _back.clicked += OnBack;
        }

        private void OnDisable()
        {
            // Null when OnEnable threw on a renamed element: do not add a second exception to the first.
            if (_back != null)
                _back.clicked -= OnBack;
        }

        private void OnBack() => BackClicked?.Invoke();
    }
}
