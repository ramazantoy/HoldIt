using System.Collections.Generic;
using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using HoldIt.Board;
using HoldIt.Restrooms;
using PrimeTween;
using UnityEngine;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// Plays <see cref="IBoardReveal"/> on the scene's board and restrooms: every piece grows from nothing (or shrinks to
    /// it) a moment after the pieces before it, in the order the <see cref="RevealWave"/> gives. The tweens run on unscaled
    /// time, as the game is paused while they play. Only the wave's end is awaited, with the token of the change.
    /// </summary>
    internal sealed class BoardReveal : IBoardReveal
    {
        private readonly BoardView _board;
        private readonly RestroomView _restrooms;
        private readonly BoardRevealConfig _config;
        private readonly List<Tween> _tweens = new List<Tween>();

        public BoardReveal(BoardView board, RestroomView restrooms, BoardRevealConfig config)
        {
            _board = board;
            _restrooms = restrooms;
            _config = config;
        }

        public UniTask Show(CancellationToken cancellation) => Play(true, cancellation);

        public UniTask Hide(CancellationToken cancellation) => Play(false, cancellation);

        private async UniTask Play(bool show, CancellationToken cancellation)
        {
            if (!_board.IsBuilt)
                return;

            var bounds = _board.Bounds;
            var speed = show ? 1f : _config.OutSpeed;
            var total = _config.WaveSeconds / speed;
            var pieceSeconds = _config.PieceSeconds / speed;
            var ease = show ? _config.Ease : Ease.InQuad;

            try
            {
                Start(_board.Pieces, bounds, total, pieceSeconds, ease, show);
                Start(_restrooms.Pieces, bounds, total, pieceSeconds, ease, show);

                await Tween.Delay(total + pieceSeconds, useUnscaledTime: true).ToUniTask(cancellation);
                // Whatever the last frame left: every piece is exactly as shown, or gone.
                Settle(_board.Pieces, show);
                Settle(_restrooms.Pieces, show);
            }
            finally
            {
                // A cancelled change takes the board down next, and a tween must not outlive the piece it moves. These
                // are not awaited, so stopping them is safe (the awaited one stops through its token).
                foreach (var tween in _tweens)
                    tween.Stop();
                _tweens.Clear();
            }
        }

        private static void Settle(IReadOnlyList<Revealable> pieces, bool show)
        {
            foreach (var piece in pieces)
            {
                if (piece.Transform != null)
                    piece.Transform.localScale = show ? piece.Scale : Vector3.zero;
            }
        }

        private void Start(IReadOnlyList<Revealable> pieces, Bounds bounds, float total, float pieceSeconds, Ease ease, bool show)
        {
            foreach (var piece in pieces)
            {
                var transform = piece.Transform;
                if (transform == null)
                    continue;

                var position = transform.position;
                var delay = RevealWave.DelayOf(position.x, position.z, bounds, total);
                // The wave retreats as the board goes out: the pieces that came in last go first.
                if (!show)
                    delay = total - delay;

                var from = show ? Vector3.zero : piece.Scale;
                var to = show ? piece.Scale : Vector3.zero;
                transform.localScale = from;
                _tweens.Add(Tween.Scale(transform, from, to, pieceSeconds, ease, startDelay: delay, useUnscaledTime: true));
            }
        }
    }
}
