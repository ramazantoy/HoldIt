using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace HoldIt.UI
{
    /// <summary>
    /// Base of a full-screen panel that fades in and out. The screen's root carries its own (nested) Canvas and a CanvasGroup.
    /// Hidden, it takes no pointer (blocksRaycasts off: a hidden screen never counts as UI for the gameplay press filter) and
    /// is not drawn at all (its Canvas is off once the fade ends). The first call after the scene loads applies at once, so a
    /// screen that starts hidden never flashes. Fades use unscaled time, so they run while the game is paused.
    /// </summary>
    public abstract class ScreenView : MonoBehaviour
    {
        [SerializeField, Required] private ScreenTransitionConfig _transition;
        [SerializeField, Required] private Canvas _canvas;
        [SerializeField, Required] private CanvasGroup _group;

        private CancellationTokenSource _fade;
        private bool _initialized;

        public bool IsVisible { get; private set; }

        protected void ShowScreen() => Fade(true).Forget();

        protected void HideScreen() => Fade(false).Forget();

        protected virtual void OnDestroy()
        {
            _fade?.Cancel();
            _fade?.Dispose();
        }

        private async UniTask Fade(bool visible)
        {
            // A newer show or hide wins: cancelling stops the running fade where it is, and its await throws (Forget drops
            // the OperationCanceledException), so the code after an await only runs for the newest call.
            _fade?.Cancel();
            _fade?.Dispose();
            _fade = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            var cancellation = _fade.Token;

            IsVisible = visible;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
            var alpha = visible ? 1f : 0f;

            if (visible)
                _canvas.enabled = true;
            if (!_initialized || _transition.FadeSeconds <= 0f)
                _group.alpha = alpha;
            else if (!Mathf.Approximately(_group.alpha, alpha))
                await Tween.Alpha(_group, alpha, _transition.FadeSeconds, useUnscaledTime: true).ToUniTask(cancellation);
            _initialized = true;

            if (!visible)
                _canvas.enabled = false;
        }
    }
}
