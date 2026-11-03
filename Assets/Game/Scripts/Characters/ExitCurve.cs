using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>A leaving character's size over the time since it started to go: 1 is its own size, 0 gone.</summary>
    public static class ExitCurve
    {
        /// <summary>Swells to the look's peak, then shrinks to nothing; false once it is gone.</summary>
        public static bool TryPuff(float time, ExitLook look, out float scale)
        {
            var t = Mathf.Max(time, 0f) / look.PuffSeconds;
            if (t >= 1f)
            {
                scale = 0f;
                return false;
            }

            scale = t < look.PuffPeakAt
                ? Mathf.Lerp(1f, look.PuffPeak, t / look.PuffPeakAt)
                : Mathf.Lerp(look.PuffPeak, 0f, (t - look.PuffPeakAt) / (1f - look.PuffPeakAt));
            return true;
        }

        /// <summary>Shrinks evenly to nothing; false once it is gone.</summary>
        public static bool TryShrink(float time, float seconds, out float scale)
        {
            scale = seconds > 0f ? 1f - Mathf.Max(time, 0f) / seconds : 0f;
            if (scale > 0f)
                return true;
            scale = 0f;
            return false;
        }
    }
}
