using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;

namespace AF.Core
{
    /// <summary>
    /// The way to await a PrimeTween animation. Validation reports any other await of a tween or sequence in project code.
    /// </summary>
    [TweenAwaitExempt("The one sanctioned way to await a tween.")]
    public static class TweenExtensions
    {
        /// <summary>
        /// Awaits the tween. Cancelling <paramref name="cancellation"/> stops the tween where it is (it never jumps to its end
        /// value) and the await throws <see cref="System.OperationCanceledException"/>, also when the token is cancelled already. A
        /// dead tween completes at once.
        /// </summary>
        /// <remarks>
        /// PrimeTween's own await returns normally in two cases that look like completion: after <c>Stop()</c>, and when the
        /// token was already cancelled or the tween was recycled by the time the await resumes. This method closes the second;
        /// stop an awaited tween through its token, never with <c>Stop()</c>. Do not call <c>SetCancellationToken</c> on the
        /// tween yourself: PrimeTween accepts one token per animation.
        /// </remarks>
        public static async UniTask ToUniTask(this Tween tween, CancellationToken cancellation)
        {
            if (!tween.isAlive)
            {
                cancellation.ThrowIfCancellationRequested();
                return;
            }
            // A token cancelled already stops the tween here; the await below then returns at once and the check after it throws.
            await tween.SetCancellationToken(cancellation, TweenSettings.CancellationMode.Stop);
            cancellation.ThrowIfCancellationRequested();
        }

        /// <inheritdoc cref="ToUniTask(Tween, CancellationToken)"/>
        public static async UniTask ToUniTask(this Sequence sequence, CancellationToken cancellation)
        {
            if (!sequence.isAlive)
            {
                cancellation.ThrowIfCancellationRequested();
                return;
            }
            await sequence.SetCancellationToken(cancellation, TweenSettings.CancellationMode.Stop);
            cancellation.ThrowIfCancellationRequested();
        }
    }
}
