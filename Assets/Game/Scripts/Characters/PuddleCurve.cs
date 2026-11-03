using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>A puddle's size and opacity over its life: it spreads out quickly, stays, and fades at the end.</summary>
    public static class PuddleCurve
    {
        /// <summary>
        /// The puddle's scale (1 is full size) and opacity (1 is the look's own) <paramref name="time"/> seconds after it
        /// appeared; false once it is gone.
        /// </summary>
        public static bool TryAt(float time, PuddleLook look, out float scale, out float alpha)
        {
            time = Mathf.Max(time, 0f);
            if (time >= look.Seconds)
            {
                scale = 0f;
                alpha = 0f;
                return false;
            }

            var grown = Mathf.Clamp01(time / look.GrowSeconds);
            scale = Mathf.Lerp(look.GrowFrom, 1f, 1f - (1f - grown) * (1f - grown));
            var fadeFrom = look.Seconds - look.FadeSeconds;
            alpha = time > fadeFrom ? 1f - (time - fadeFrom) / look.FadeSeconds : 1f;
            return true;
        }
    }
}
