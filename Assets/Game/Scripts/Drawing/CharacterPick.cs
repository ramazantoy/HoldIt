using System.Collections.Generic;
using HoldIt.Characters;
using UnityEngine;

namespace HoldIt.Drawing
{
    /// <summary>
    /// Which character a press means. Each candidate is the line from its feet to its head on the screen: the camera looks
    /// down at a slant, so a character covers the tile behind its own, and the press is measured to that line, not to the
    /// tile under the finger.
    /// </summary>
    public static class CharacterPick
    {
        /// <summary>
        /// The candidate whose line is nearest to <paramref name="pointer"/>, if that is within the candidate's radius.
        /// </summary>
        public static bool TryPick(IReadOnlyList<PickCandidate> candidates, Vector2 pointer, out CharacterId id)
        {
            id = default;
            var best = float.MaxValue;
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                var distance = DistanceToSegment(pointer, candidate.Feet, candidate.Head);
                if (distance > candidate.Radius || distance >= best)
                    continue;
                best = distance;
                id = candidate.Id;
            }
            return best < float.MaxValue;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var lengthSquared = ab.sqrMagnitude;
            var t = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSquared) : 0f;
            return Vector2.Distance(point, a + t * ab);
        }
    }
}
