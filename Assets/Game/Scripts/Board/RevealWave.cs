using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>
    /// When each piece of a board comes in: a wave that crosses it from its top left corner to the bottom right one, so
    /// the pieces of the tiles and of the restrooms (which sit between tiles) share one timing.
    /// </summary>
    public static class RevealWave
    {
        /// <summary>
        /// Where <paramref name="x"/>, <paramref name="z"/> lies along the wave, from 0 at the top left corner of
        /// <paramref name="bounds"/> (smallest x, largest z: the map's top row is +Z) to 1 at the bottom right one. Points
        /// outside the bounds count as on its edge.
        /// </summary>
        public static float Progress(float x, float z, Bounds bounds)
        {
            var width = bounds.size.x;
            var depth = bounds.size.z;
            if (width + depth <= 0f)
                return 0f;

            var along = (x - bounds.min.x) + (bounds.max.z - z);
            return Mathf.Clamp01(along / (width + depth));
        }

        /// <summary>How long after the wave starts a piece at <paramref name="x"/>, <paramref name="z"/> starts to come in.</summary>
        public static float DelayOf(float x, float z, Bounds bounds, float totalSeconds) =>
            Progress(x, z, bounds) * totalSeconds;
    }
}
