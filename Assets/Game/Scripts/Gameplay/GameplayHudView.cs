using System;
using AF.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// The gameplay HUD (GameplayHud prefab). Its full-screen root has no Graphic, so touches outside its buttons reach the
    /// game.
    /// </summary>
    public sealed class GameplayHudView : MonoBehaviour, IGameplayHudView
    {
        [SerializeField, Required] private Button _menu;

        public event Action MenuClicked;

        private void Awake() => _menu.onClick.AddListener(() => MenuClicked?.Invoke());
    }
}
