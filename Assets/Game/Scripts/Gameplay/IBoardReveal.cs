using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HoldIt.Gameplay
{
    /// <summary>Brings the standing board and restrooms in and takes them out, piece by piece in a wave.</summary>
    internal interface IBoardReveal
    {
        /// <summary>Grows the hidden pieces in. Completes when the last one is in.</summary>
        UniTask Show(CancellationToken cancellation);

        /// <summary>
        /// Shrinks the pieces out, and with them the <paramref name="content"/> that stands on them (characters, marks,
        /// puddles; a screen-space one such as a bubble goes at the start). Completes when the last one is gone.
        /// </summary>
        UniTask Hide(IReadOnlyList<Transform> content, CancellationToken cancellation);
    }
}
