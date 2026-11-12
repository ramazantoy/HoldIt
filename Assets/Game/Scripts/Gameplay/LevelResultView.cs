using System;
using System.Collections.Generic;
using AF.Core;
using HoldIt.Feedback;
using HoldIt.UI;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using VContainer;

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

        [SerializeField, Tooltip("The pieces of confetti thrown when a level is won; they are placed from the pattern (see the style).")]
        private Image[] _confetti = Array.Empty<Image>();

        [SerializeField, Required] private GameplayUiStyle _style;

        [SerializeField, Required] private TextMeshProUGUI _served;

        [SerializeField, Tooltip("Who made it; {0} is how many, {1} the crowd.")]
        private LocalizedString _servedLabel;

        [SerializeField, Required] private Button _next;
        [SerializeField, Required] private Button _retry;
        [SerializeField, Required] private Button _menu;

        private readonly List<Tween> _pops = new List<Tween>();
        private IFeedback _feedback;

        public event Action NextClicked;
        public event Action RetryClicked;
        public event Action MenuClicked;

        [Inject]
        public void Construct(IFeedback feedback) => _feedback = feedback;

        public void Show(LevelResult result)
        {
            StopPops();
            _title.text = TitleOf(result).GetLocalizedString(result.Level);
            _served.text = _servedLabel.GetLocalizedString(result.Served, result.Crowd);
            for (var i = 0; i < _stars.Length; i++)
            {
                var star = _stars[i];
                var earned = i < result.Stars;
                star.gameObject.SetActive(i < result.MaxStars);
                star.color = earned ? _style.StarEarned : _style.StarMissed;
                star.rectTransform.localScale = Vector3.one;
                if (earned && star.gameObject.activeInHierarchy)
                    PopStar(star.rectTransform, i);
            }
            _next.gameObject.SetActive(result.NextOpen);
            if (result.IsWon)
                ThrowConfetti();
            else
                Place(Array.Empty<ConfettiPiece>(), 0f);
            ShowScreen();
        }

        public void Hide()
        {
            StopPops();
            Place(Array.Empty<ConfettiPiece>(), 0f);
            HideScreen();
        }

        /// <summary>
        /// The screen outlasts a level, so a star still popping (or its sound still waiting) must not carry into the next
        /// one. These tweens are not awaited, so stopping them is safe.
        /// </summary>
        private void StopPops()
        {
            foreach (var pop in _pops)
                pop.Stop();
            _pops.Clear();
        }

        private void Awake()
        {
            _next.onClick.AddListener(() => NextClicked?.Invoke());
            _retry.onClick.AddListener(() => RetryClicked?.Invoke());
            _menu.onClick.AddListener(() => MenuClicked?.Invoke());
        }

        /// <summary>An earned star pops in one after another once the screen has faded in. Unscaled: the game is paused here.</summary>
        private void PopStar(RectTransform star, int index)
        {
            var delay = _style.StarPopDelay + index * _style.StarPopGap;
            star.localScale = Vector3.zero;
            _pops.Add(Tween.Scale(star, Vector3.zero, Vector3.one, _style.StarPopSeconds, Ease.OutBack, startDelay: delay,
                    useUnscaledTime: true)
                .SetCancellationToken(destroyCancellationToken));
            _pops.Add(Tween.Delay(delay, () => _feedback.Play(FeedbackKind.StarPop), useUnscaledTime: true)
                .SetCancellationToken(destroyCancellationToken));
        }

        /// <summary>The pieces fly from where they sit in the prefab, one tween that places all of them. Unscaled: the game is paused.</summary>
        private void ThrowConfetti()
        {
            var seconds = _style.ConfettiSeconds;
            var speed = _style.ConfettiSpeed;
            var pieces = ConfettiPattern.Make(_confetti.Length, UnityEngine.Random.Range(0, int.MaxValue), _style.ConfettiSpread,
                speed.x, speed.y, _style.ConfettiSpin, _style.ConfettiColors.Length, _style.ConfettiDelay);
            Place(pieces, -1f);
            _pops.Add(Tween.Custom(0f, seconds, seconds, time => Place(pieces, time), Ease.Linear,
                    startDelay: _style.ConfettiStart, useUnscaledTime: true)
                .SetCancellationToken(destroyCancellationToken));
        }

        // A time before every piece's delay hides them all; no pieces at all hides every image.
        private void Place(IReadOnlyList<ConfettiPiece> pieces, float time)
        {
            var colors = _style.ConfettiColors;
            for (var i = 0; i < _confetti.Length; i++)
            {
                var image = _confetti[i];
                if (i >= pieces.Count)
                {
                    image.enabled = false;
                    continue;
                }

                var piece = pieces[i];
                var color = colors.Length > 0 ? colors[piece.Color % colors.Length] : Color.white;
                color.a = ConfettiPattern.AlphaAt(piece, time, _style.ConfettiSeconds, _style.ConfettiFadeSeconds);
                image.enabled = true;
                image.color = color;
                image.rectTransform.anchoredPosition = ConfettiPattern.PositionAt(piece, time, _style.ConfettiGravity);
                image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, ConfettiPattern.AngleAt(piece, time));
            }
        }

        private LocalizedString TitleOf(LevelResult result)
        {
            if (!result.IsWon)
                return _lostTitle;
            return result.HasNext ? _wonTitle : _gameDoneTitle;
        }
    }
}
