using System;

namespace HoldIt.Characters
{
    /// <summary>How long a one-shot particle system is worth keeping: the last particle is born at the end of its duration.</summary>
    public static class BurstTime
    {
        private const float Least = 0.1f;

        /// <summary>The duration plus the longest a particle lives, never less than a tenth of a second.</summary>
        public static float Of(float duration, float longestLifetime)
        {
            var total = Math.Max(duration, 0f) + Math.Max(longestLifetime, 0f);
            return total > Least ? total : Least;
        }
    }
}
