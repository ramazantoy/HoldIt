using System;
using UnityEngine;

namespace HoldIt.Spawning
{
    /// <summary>
    /// The gaps between a level's arrivals: the first is <c>start</c> seconds, the last <c>end</c>, and the ones between
    /// shrink (or grow) evenly from one to the other.
    /// </summary>
    public static class SpawnSchedule
    {
        /// <summary>
        /// Seconds from arrival <paramref name="arrived"/> to the next, counting from 1: gap 1 is between the first two
        /// arrivals. A crowd of one or two has a single gap, <paramref name="start"/>.
        /// </summary>
        public static float GapAfter(int arrived, int crowd, float start, float end)
        {
            if (arrived < 1)
                throw new ArgumentOutOfRangeException(nameof(arrived), $"Gaps follow an arrival, not {arrived}.");

            var gaps = crowd - 1;
            var t = gaps > 1 ? Mathf.Clamp01((arrived - 1f) / (gaps - 1f)) : 0f;
            return Mathf.Lerp(start, end, t);
        }
    }
}
