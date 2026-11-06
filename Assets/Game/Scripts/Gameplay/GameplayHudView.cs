using System;
using AF.Core;
using HoldIt.Board;
using PrimeTween;
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

        [SerializeField, Required, Tooltip("Covers the screen and flashes when a life is lost; no raycast target.")]
        private Image _flash;

        [SerializeField, Required, Tooltip("The pill holding the hearts.")]
        private RectTransform _livesPill;

        [SerializeField, Required, Tooltip("The pill holding the served counter.")]
        private RectTransform _servedPill;

        [SerializeField, Required, Tooltip("The HUD's group: it goes transparent and stops taking presses while the game is paused.")]
        private CanvasGroup _group;

        private readonly Vector3[] _corners = new Vector3[4];
        private int _levelNumber;
        private Tween _livesBump;
        private Tween _servedBump;

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

        public void SetPaused(bool paused)
        {
            _group.alpha = paused ? 0f : 1f;
            _group.interactable = !paused;
        }

        public void ShowServed(int served, int crowd, bool animate)
        {
            _served.text = $"{served}/{crowd}";
            if (animate)
                Bump(ref _servedBump, _servedPill);
        }

        public void ShowLives(int lives, int maxLives, bool animate)
        {
            for (var i = 0; i < _hearts.Length; i++)
            {
                var heart = _hearts[i];
                heart.gameObject.SetActive(i < maxLives);
                var full = i < lives;
                var color = full ? _style.HeartFull : _style.HeartLost;
                var scale = Vector3.one * (full ? 1f : _style.LostHeartScale);
                if (animate && heart.gameObject.activeInHierarchy)
                {
                    Tween.Color(heart, color, _style.HeartSeconds, useUnscaledTime: true)
                        .SetCancellationToken(destroyCancellationToken);
                    Tween.Scale(heart.rectTransform, scale, _style.HeartSeconds, Ease.OutBack, useUnscaledTime: true)
                        .SetCancellationToken(destroyCancellationToken);
                }
                else
                {
                    heart.color = color;
                    heart.rectTransform.localScale = scale;
                }
            }

            if (!animate)
                return;
            Bump(ref _livesBump, _livesPill);
            _flash.color = _style.FlashColor;
            Tween.Alpha(_flash, _style.FlashAlpha, 0f, _style.FlashSeconds, Ease.OutQuad, useUnscaledTime: true)
                .SetCancellationToken(destroyCancellationToken);
        }

        /// <summary>A short swell of the pill; a swell still running is finished first so the pill never keeps a drift.</summary>
        private void Bump(ref Tween running, RectTransform pill)
        {
            if (running.isAlive)
                running.Complete();
            running = Tween.PunchScale(pill, Vector3.one * _style.BumpScale, _style.BumpSeconds, frequency: 1,
                useUnscaledTime: true).SetCancellationToken(destroyCancellationToken);
        }

        private void Awake() => _pause.onClick.AddListener(() => PauseClicked?.Invoke());

        private void OnEnable() => _levelLabel.StringChanged += SetLevelText;

        private void OnDisable() => _levelLabel.StringChanged -= SetLevelText;

        private void SetLevelText(string text) => _level.text = _levelNumber > 0 ? text : string.Empty;
    }
}
