using System;
using AF.Core;
using HoldIt.Board;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// The gameplay HUD (GameplayHud prefab). Its full-screen root has no Graphic, so touches outside its buttons reach the
    /// game. The board is fitted below its top bar, which holds the pause button, the level, a heart per life (a lost one
    /// greys and shrinks) and who made it out of the level's crowd.
    /// </summary>
    public sealed class GameplayHudView : MonoBehaviour, IGameplayHudView, IBoardInsets
    {
        [SerializeField, Required, FormerlySerializedAs("_menu")] private Button _pause;

        [SerializeField, Required, Tooltip("The strip at the top of the safe area that holds the HUD; the board stays below it.")]
        private RectTransform _topBar;

        [SerializeField, Required] private TextMeshProUGUI _level;

        [SerializeField, Tooltip("The level's label; {0} is its number.")]
        private LocalizedString _levelLabel;

        [SerializeField, Required] private TextMeshProUGUI _served;

        [SerializeField, Tooltip("One per life, left to right; lives past the last heart are not shown.")]
        private Image[] _hearts = Array.Empty<Image>();

        [SerializeField, Required] private GameplayUiStyle _style;

        private readonly Vector3[] _corners = new Vector3[4];
        private int _levelNumber;

        public event Action PauseClicked;

        // The HUD's canvas is Screen Space Overlay, where world space is screen pixels.
        public float Top
        {
            get
            {
                _topBar.GetWorldCorners(_corners);
                return Screen.height - _corners[0].y;
            }
        }

        public void ShowLevel(int number)
        {
            _levelNumber = number;
            _levelLabel.Arguments = new object[] { number };
            _levelLabel.RefreshString();
        }

        public void ShowServed(int served, int crowd) => _served.text = $"{served}/{crowd}";

        public void ShowLives(int lives, int maxLives)
        {
            for (var i = 0; i < _hearts.Length; i++)
            {
                var heart = _hearts[i];
                heart.gameObject.SetActive(i < maxLives);
                var full = i < lives;
                heart.color = full ? _style.HeartFull : _style.HeartLost;
                heart.rectTransform.localScale = Vector3.one * (full ? 1f : _style.LostHeartScale);
            }
        }

        private void Awake() => _pause.onClick.AddListener(() => PauseClicked?.Invoke());

        private void OnEnable() => _levelLabel.StringChanged += SetLevelText;

        private void OnDisable() => _levelLabel.StringChanged -= SetLevelText;

        private void SetLevelText(string text) => _level.text = _levelNumber > 0 ? text : string.Empty;
    }
}
