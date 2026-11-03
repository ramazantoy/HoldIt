using HoldIt.Board;

namespace HoldIt.Paths
{
    /// <summary>Where the player may draw a path.</summary>
    public static class PathRules
    {
        /// <summary>
        /// True for a walkable tile inside the map that is not an entrance: walls and buildings stop a path, and entrances
        /// are where characters come in. Narrow passages and either door are drawable; walking them is checked step by step.
        /// </summary>
        public static bool IsDrawable(ITileGrid grid, Cell cell) =>
            grid.IsWalkable(cell) && grid.At(cell).Type != TileType.Entrance;
    }
}
