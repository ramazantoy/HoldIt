using System;
using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;

namespace HoldIt.Intro
{
    /// <summary>
    /// Plays the splash on the camera's far plane. The VideoPlayer has Play On Awake off: IntroFlow starts it, while the boot
    /// steps run. A video that never reports its end (a decoder stall, a missing clip) is given up on after its length plus a
    /// margin, so the game never stays black in the intro.
    /// </summary>
    public sealed class IntroVideoView : MonoBehaviour, IIntroVideo
    {
        private const float TimeoutMarginSeconds = 2f;

        [SerializeField, Required] private VideoPlayer _player;

        public async UniTask Play(CancellationToken cancellation)
        {
            var finished = new UniTaskCompletionSource();
            void OnEnd(VideoPlayer _) => finished.TrySetResult();
            void OnError(VideoPlayer _, string message)
            {
                Log.Error($"Intro video failed: {message}", this);
                finished.TrySetResult();
            }

            var timeout = TimeSpan.FromSeconds((_player.clip != null ? _player.clip.length : 0d) + TimeoutMarginSeconds);
            using var stopTimer = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            _player.loopPointReached += OnEnd;
            _player.errorReceived += OnError;
            try
            {
                _player.Play();
                var winner = await UniTask.WhenAny(
                    finished.Task.AttachExternalCancellation(cancellation),
                    UniTask.Delay(timeout, DelayType.UnscaledDeltaTime, cancellationToken: stopTimer.Token));
                if (winner == 1)
                    Log.Warning($"Intro video did not report its end within {timeout.TotalSeconds:0.#} s; moving on.", this);
            }
            finally
            {
                stopTimer.Cancel();
                // Managed events: removing them is safe even if the scene is already being destroyed.
                _player.loopPointReached -= OnEnd;
                _player.errorReceived -= OnError;
            }
        }
    }
}
