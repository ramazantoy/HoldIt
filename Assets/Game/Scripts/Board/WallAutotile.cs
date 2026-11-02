namespace HoldIt.Board
{
    /// <summary>
    /// Picks a wall block's shape and facing from the sides it shares with other wall blocks, so that neighbouring blocks
    /// read as one mass.
    /// </summary>
    public static class WallAutotile
    {
        private const int North = 1 << (int)Direction.North;
        private const int East = 1 << (int)Direction.East;
        private const int South = 1 << (int)Direction.South;
        private const int AllSides = 0b1111;

        /// <summary>The bit of a side in a side mask: North 1, East 2, South 4, West 8.</summary>
        public static int Bit(Direction side) => 1 << (int)side;

        /// <param name="wallSides">The sides with a wall block next to them, as a mask of <see cref="Bit"/>s.</param>
        /// <returns>
        /// The shape whose joined sides, turned clockwise by <c>Facing</c> quarter turns, are exactly
        /// <paramref name="wallSides"/>. A shape that fits several facings takes the first one clockwise from North.
        /// </returns>
        public static (WallShape Shape, Direction Facing) Pick(int wallSides)
        {
            var sides = wallSides & AllSides;
            for (var turns = 0; turns < 4; turns++)
            {
                var shape = ShapeFacingNorth(sides);
                if (shape.HasValue)
                    return (shape.Value, (Direction)turns);
                sides = TurnCounterClockwise(sides);
            }
            // Unreachable: every 4-bit mask is one of the six shapes in some facing.
            return (WallShape.Single, Direction.North);
        }

        // The shape that joins exactly these sides while facing North, if any.
        private static WallShape? ShapeFacingNorth(int sides) => sides switch
        {
            0 => WallShape.Single,
            North => WallShape.End,
            North | South => WallShape.Straight,
            North | East => WallShape.Corner,
            North | East | South => WallShape.T,
            AllSides => WallShape.Cross,
            _ => null,
        };

        // Moves every side one step counter-clockwise (East to North): undoes one clockwise quarter turn of the piece.
        private static int TurnCounterClockwise(int sides) => ((sides >> 1) | (sides << 3)) & AllSides;
    }
}
