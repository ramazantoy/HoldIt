using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>A speech bubble's size and opacity over its life: it pops up with a small overshoot, stays, then fades.</summary>
    public static class BubbleCurve
    {
        private const float Overshoot = 1.70158f;

        /// <summary>
        /// The bubble's scale (1 is full size) and alpha <paramref name="time"/> seconds after it was shown; false once it
        /// has faded out.
        /// </summary>
        public static bool TryAt(float time, BubbleTiming timing, out float scale, out float alpha)
        {
            scale = 1f;
            alpha = 1f;
            if (time >= timing.FadeAt + timing.Fade)
            {
                scale = 0f;
                alpha = 0f;
                return false;
            }

            time = Mathf.Max(time, 0f);
            if (time < timing.Pop)
                scale = EaseOutBack(time / timing.Pop);
            if (time > timing.FadeAt)
                alpha = 1f - (time - timing.FadeAt) / timing.Fade;
            return true;
        }

        private static float EaseOutBack(float t)
        {
            var u = Mathf.Clamp01(t) - 1f;
            return 1f + (Overshoot + 1f) * u * u * u + Overshoot * u * u;
        }
    }
}
