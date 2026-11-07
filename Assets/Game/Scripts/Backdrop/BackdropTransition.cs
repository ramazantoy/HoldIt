using UnityEngine;

namespace HoldIt.Backdrop
{
    /// <summary>How long the backdrop's change takes.</summary>
    public static class BackdropTransition
    {
        /// <summary>
        /// Seconds the change takes: <paramref name="minSeconds"/> for no change at all up to <paramref name="maxSeconds"/>
        /// at <paramref name="fullDistance"/> apart (see <see cref="BackdropColors.Distance"/>) or more.
        /// </summary>
        public static float Duration(BackdropColors from, BackdropColors to, float minSeconds, float maxSeconds, float fullDistance)
        {
            if (fullDistance <= 0f)
                return maxSeconds;
            var share = Mathf.Clamp01(BackdropColors.Distance(from, to) / fullDistance);
            return Mathf.Lerp(minSeconds, maxSeconds, share);
        }
    }
}
