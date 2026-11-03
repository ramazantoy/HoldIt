using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>A character nearly out of patience shakes sideways, harder the closer it gets to an accident.</summary>
    public static class PatienceShake
    {
        /// <summary>0 up to the look's threshold, rising to 1 when patience is out.</summary>
        public static float Urgency(float patience, PatienceLook look) => look.ShakeFrom >= 1f
            ? 0f
            : Mathf.Clamp01((patience - look.ShakeFrom) / (1f - look.ShakeFrom));

        /// <summary>The body's sideways offset, in tiles, <paramref name="time"/> seconds into the game.</summary>
        public static float Offset(float time, float patience, PatienceLook look) =>
            Mathf.Sin(time * look.ShakeFrequency) * look.ShakeAmplitude * Urgency(patience, look);
    }
}
