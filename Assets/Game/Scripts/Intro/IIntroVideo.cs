using System.Threading;
using Cysharp.Threading.Tasks;

namespace HoldIt.Intro
{
    /// <summary>The splash video the game opens with.</summary>
    public interface IIntroVideo
    {
        /// <summary>Plays it to the end. A video that fails to play is logged and counts as finished.</summary>
        UniTask Play(CancellationToken cancellation);
    }
}
