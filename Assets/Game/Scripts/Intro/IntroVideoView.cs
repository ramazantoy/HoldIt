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
    /// steps run. The video is prepared first (opening the decoder can stall the first frames for seconds), then plays for
    /// its own length on unscaled time; no player event is listened to. A video that does not prepare in time is given up
    /// on, so the game never stays black in the intro.
    /// </summary>
    public sealed class IntroVideoView : MonoBehaviour, IIntroVideo
    {
        private const float PrepareTimeoutSeconds = 6f;

        [SerializeField, Required] private VideoPlayer _player;

        public async UniTask Play(CancellationToken cancellation)
        {
            if (!_player.isPrepared)
            {
                _player.Prepare();
                if (!await WaitUntilPrepared(cancellation))
                {
                    Log.Warning($"Intro video was not prepared within {PrepareTimeoutSeconds:0.#} s; moving on.", this);
                    return;
                }
            }

            _player.Play();
            if (_player.clip != null)
                await UniTask.Delay(TimeSpan.FromSeconds(_player.clip.length), DelayType.UnscaledDeltaTime, cancellationToken: cancellation);
        }

        /// <summary>True when the player got ready before the limit ran out.</summary>
        private async UniTask<bool> WaitUntilPrepared(CancellationToken cancellation)
        {
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var winner = await UniTask.WhenAny(
                UniTask.WaitUntil(() => _player.isPrepared, cancellationToken: stop.Token),
                UniTask.Delay(TimeSpan.FromSeconds(PrepareTimeoutSeconds), DelayType.UnscaledDeltaTime, cancellationToken: stop.Token));
            stop.Cancel();
            return winner == 0;
        }
    }
}
