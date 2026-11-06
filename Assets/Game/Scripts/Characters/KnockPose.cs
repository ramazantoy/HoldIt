using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>
    /// The knocking arm of a character waiting at a taken stall's door, by seconds since the wait began. A row of knocks
    /// repeats with a rest in between: the arm is raised, the hand strikes at the start of every beat and pulls back until
    /// the next, and after the last beat the arm comes down for the rest and goes up again before the next row.
    /// </summary>
    public static class KnockPose
    {
        /// <summary>Seconds from the start of one row to the start of the next.</summary>
        public static float CycleSeconds(KnockStyle style) => style.Beats * style.BeatSeconds + style.PauseSeconds;

        /// <summary>Degrees the arm is raised forward at <paramref name="time"/>; positive swings the hand forward.</summary>
        public static float Arm(float time, KnockStyle style)
        {
            var t = Mathf.Repeat(Mathf.Max(0f, time), CycleSeconds(style));
            var knocking = style.Beats * style.BeatSeconds;
            if (t < knocking)
            {
                var u = Mathf.Repeat(t, style.BeatSeconds) / style.BeatSeconds;
                var pulled = (1f - Mathf.Cos(u * 2f * Mathf.PI)) * 0.5f;
                return style.Raise - style.PullBack * pulled;
            }

            var rest = t - knocking;
            var lower = Mathf.Min(style.LowerSeconds, style.PauseSeconds * 0.5f);
            if (lower <= 0f)
                return 0f;
            if (rest < lower)
                return style.Raise * (1f - Mathf.SmoothStep(0f, 1f, rest / lower));
            if (rest > style.PauseSeconds - lower)
                return style.Raise * Mathf.SmoothStep(0f, 1f, (rest - (style.PauseSeconds - lower)) / lower);
            return 0f;
        }

        /// <summary>How many knocks land in the time after <paramref name="from"/> up to and including <paramref name="to"/>.</summary>
        public static int Hits(float from, float to, KnockStyle style) => HitsUpTo(to, style) - HitsUpTo(from, style);

        private static int HitsUpTo(float time, KnockStyle style)
        {
            if (time < 0f)
                return 0;
            var cycle = CycleSeconds(style);
            var rows = Mathf.FloorToInt(time / cycle);
            var into = time - rows * cycle;
            var inRow = Mathf.Min(style.Beats, Mathf.FloorToInt(into / style.BeatSeconds) + 1);
            return rows * style.Beats + inRow;
        }
    }
}
