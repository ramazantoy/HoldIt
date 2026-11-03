using System;
using AF.Core;
using HoldIt.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// The level's result screen (LevelResultScreen prefab): a title for a won level, the last one won, or a lost one; a
    /// star per life left; who made it out of the crowd; and the buttons. Next shows only when the next level is open.
    /// </summary>
    public sealed class LevelResultView : ScreenView, ILevelResultView
    {
        [SerializeField, Required] private TextMeshProUGUI _title;

        [SerializeField, Tooltip("A won level's title; {0} is its number.")]
        private LocalizedString _wonTitle;

        [SerializeField, Tooltip("The last level's title once it is won.")]
        private LocalizedString _gameDoneTitle;

        [SerializeField, Tooltip("A lost level's title.")]
        private LocalizedString _lostTitle;

        [SerializeField, Tooltip("One per star, left to right; stars past the last one are not shown.")]
        private Image[] _stars = Array.Empty<Image>();

        [SerializeField, Required] private GameplayUiStyle _style;

        [SerializeField, Required] private TextMeshProUGUI _served;

        [SerializeField, Tooltip("Who made it; {0} is how many, {1} the crowd.")]
        private LocalizedString _servedLabel;

        [SerializeField, Required] private Button _next;
        [SerializeField, Required] private Button _retry;
        [SerializeField, Required] private Button _menu;

        public event Action NextClicked;
        public event Action RetryClicked;
        public event Action MenuClicked;

        public void Show(LevelResult result)
        {
            _title.text = TitleOf(result).GetLocalizedString(result.Level);
            _served.text = _servedLabel.GetLocalizedString(result.Served, result.Crowd);
            for (var i = 0; i < _stars.Length; i++)
            {
                _stars[i].gameObject.SetActive(i < result.MaxStars);
                _stars[i].color = i < result.Stars ? _style.StarEarned : _style.StarMissed;
            }
            _next.gameObject.SetActive(result.NextOpen);
            ShowScreen();
        }

        public void Hide() => HideScreen();

        private void Awake()
        {
            _next.onClick.AddListener(() => NextClicked?.Invoke());
            _retry.onClick.AddListener(() => RetryClicked?.Invoke());
            _menu.onClick.AddListener(() => MenuClicked?.Invoke());
        }

        private LocalizedString TitleOf(LevelResult result)
        {
            if (!result.IsWon)
                return _lostTitle;
            return result.HasNext ? _wonTitle : _gameDoneTitle;
        }
    }
}
