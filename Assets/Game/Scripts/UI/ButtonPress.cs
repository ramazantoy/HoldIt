using AF.Core;
using HoldIt.Feedback;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

namespace HoldIt.UI
{
    /// <summary>
    /// Makes a button give under the finger: it shrinks when pressed and springs back (<see cref="Press"/>). Unscaled time, so it works in the paused game. A button that cannot be pressed does not move.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonPress : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField, Required] private ScreenTransitionConfig _config;

        private Button _button;
        private IFeedback _feedback;
        private Vector3 _restScale;
        private Tween _tween;

        /// <summary>The scene scope injects the screens' canvas (autoInjectGameObjects), so the click can be heard.</summary>
        [Inject]
        public void Construct(IFeedback feedback) => _feedback = feedback;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _restScale = transform.localScale;
        }

        private void OnDisable()
        {
            if (_tween.isAlive)
                _tween.Stop();
            transform.localScale = _restScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.IsInteractable())
                return;
            Press();
            _feedback?.Play(FeedbackKind.UiClick);
        }

        /// <summary>
        /// One press is one round trip: rest size to the pressed size and back (yoyo), started from the rest size every
        /// time. It needs no release event and no callback, and a press that lands on a running one restarts it, so the
        /// tween never starts at the value it ends on.
        /// </summary>
        private void Press()
        {
            if (_tween.isAlive)
                _tween.Stop();
            transform.localScale = _restScale;
            _tween = Tween.Scale(transform, _restScale, _restScale * _config.PressScale, _config.PressSeconds, Ease.OutQuad,
                    cycles: 2, cycleMode: CycleMode.Yoyo, useUnscaledTime: true)
                .SetCancellationToken(destroyCancellationToken);
        }
    }
}
