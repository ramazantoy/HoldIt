namespace HoldIt.Board
{
    /// <summary>
    /// Which way a narrow tile may be walked. Its posts stand across the passage, so a step that runs across it (into the
    /// tile from the side, or out of it to the side, as between two narrow tiles standing side by side) goes through a post
    /// and is not allowed. Drawing, walking, arrivals and the map check all ask it.
    /// </summary>
    public static class Passage
    {
        /// <summary>
        /// <see cref="Direction.North"/> when the passage runs north-south (its posts stand east and west), or
        /// <see cref="Direction.East"/> when both its north and south sides are closed, which makes it east-west.
        /// </summary>
        public static Direction Axis(ITileGrid grid, Cell cell) =>
            !grid.IsWalkable(cell.Step(Direction.North)) && !grid.IsWalkable(cell.Step(Direction.South))
                ? Direction.East
                : Direction.North;

        /// <summary>True when a step between two side neighbours crosses the posts of neither narrow tile it touches.</summary>
        public static bool CanStep(ITileGrid grid, Cell from, Cell to) => Runs(grid, from, to) && Runs(grid, to, from);

        // A step along the passage of `tile` (toward `other`) is open; one across it meets its posts.
        private static bool Runs(ITileGrid grid, Cell tile, Cell other)
        {
            if (!grid.InBounds(tile) || grid.At(tile).Type != TileType.Narrow)
                return true;
            var northSouth = tile.Col == other.Col;
            return Axis(grid, tile) == Direction.North ? northSouth : !northSouth;
        }
    }
}
