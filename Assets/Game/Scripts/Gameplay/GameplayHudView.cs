using System;
using AF.Core;
using HoldIt.Board;
using UnityEngine;
using UnityEngine.UI;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// The gameplay HUD (GameplayHud prefab). Its full-screen root has no Graphic, so touches outside its buttons reach the
    /// game. The board is fitted below its top bar.
    /// </summary>
    public sealed class GameplayHudView : MonoBehaviour, IGameplayHudView, IBoardInsets
    {
        [SerializeField, Required] private Button _menu;

        [SerializeField, Required, Tooltip("The strip at the top of the safe area that holds the HUD's buttons; the board stays below it.")]
        private RectTransform _topBar;

        private readonly Vector3[] _corners = new Vector3[4];

        public event Action MenuClicked;

        // The HUD's canvas is Screen Space Overlay, where world space is screen pixels.
        public float Top
        {
            get
            {
                _topBar.GetWorldCorners(_corners);
                return Screen.height - _corners[0].y;
            }
        }

        private void Awake() => _menu.onClick.AddListener(() => MenuClicked?.Invoke());
    }
}
