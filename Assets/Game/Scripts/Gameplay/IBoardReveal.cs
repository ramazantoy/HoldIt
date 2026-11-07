using System.Threading;
using Cysharp.Threading.Tasks;

namespace HoldIt.Gameplay
{
    /// <summary>Brings the standing board and restrooms in and takes them out, piece by piece in a wave.</summary>
    internal interface IBoardReveal
    {
        /// <summary>Grows the hidden pieces in. Completes when the last one is in.</summary>
        UniTask Show(CancellationToken cancellation);

        /// <summary>Shrinks the pieces out. Completes when the last one is gone.</summary>
        UniTask Hide(CancellationToken cancellation);
    }
}
