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
    /// Makes a button give under the finger: it shrinks while pressed and comes back on release (or when the finger
    /// slides off it). Unscaled time, so it works in the paused game. A button that cannot be pressed does not move.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
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
            ScaleTo(_config.PressScale);
            _feedback?.Play(FeedbackKind.UiClick);
        }

        public void OnPointerUp(PointerEventData eventData) => ScaleTo(1f);

        public void OnPointerExit(PointerEventData eventData) => ScaleTo(1f);

        private void ScaleTo(float share)
        {
            if (_tween.isAlive)
                _tween.Stop();
            _tween = Tween.Scale(transform, _restScale * share, _config.PressSeconds, Ease.OutQuad, useUnscaledTime: true)
                .SetCancellationToken(destroyCancellationToken);
        }
    }
}
