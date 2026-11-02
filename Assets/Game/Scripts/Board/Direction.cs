using System.Collections.Generic;
using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>
    /// A side of a tile. North is the map's top row side (away from the camera, world +Z), East is world +X.
    /// </summary>
    public enum Direction
    {
        North,
        East,
        South,
        West,
    }

    public static class Directions
    {
        /// <summary>The four directions, clockwise from North.</summary>
        public static readonly IReadOnlyList<Direction> All =
            new[] { Direction.North, Direction.East, Direction.South, Direction.West };

        /// <summary>
        /// Rotation about Y that turns a piece modelled on its tile's north edge to this edge (0, 90, 180 or 270 degrees).
        /// </summary>
        public static Quaternion Rotation(this Direction direction) => Quaternion.Euler(0f, 90f * (int)direction, 0f);

        public static Direction Opposite(this Direction direction) => (Direction)(((int)direction + 2) % 4);

        /// <summary>The next direction clockwise: North to East, West to North.</summary>
        public static Direction Clockwise(this Direction direction) => (Direction)(((int)direction + 1) % 4);
    }
}
