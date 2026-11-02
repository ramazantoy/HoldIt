using System;
using AF.Core;
using HoldIt.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// The gameplay HUD (GameplayHud.uxml). Its full-screen container ignores the pointer, so touches outside its buttons reach
    /// the game.
    /// </summary>
    public sealed class GameplayHudView : MonoBehaviour, IGameplayHudView
    {
        private const string MENU_BUTTON = "menu-button";

        [SerializeField, Required] private UIDocument _document;

        private Button _menu;

        public event Action MenuClicked;

        // The UIDocument builds its tree in its own OnEnable, which runs before this one.
        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            SafeArea.Track(root);
            _menu = root.Required<Button>(MENU_BUTTON);
            _menu.clicked += OnMenu;
        }

        private void OnDisable()
        {
            // Null when OnEnable threw on a renamed element: do not add a second exception to the first.
            if (_menu != null)
                _menu.clicked -= OnMenu;
        }

        private void OnMenu() => MenuClicked?.Invoke();
    }
}
