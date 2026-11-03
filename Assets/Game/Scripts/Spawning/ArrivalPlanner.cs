using System;
using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Paths;

namespace HoldIt.Spawning
{
    /// <summary>
    /// Where a newcomer comes in and where it goes: a free entrance, a free lobby cell, and the shortest way from one to
    /// the other around everyone. The way skips doors, and narrow passages for a Fat one, so arrivals never take a wrong turn.
    /// </summary>
    public static class ArrivalPlanner
    {
        /// <summary>
        /// Picks a free entrance and a free lobby cell at random and finds the way between them. False when no entrance or
        /// lobby cell is free, or no way leads from the one to the other.
        /// </summary>
        /// <param name="isFree">Whether nobody stands on, steps into or reserved a cell.</param>
        public static bool TryPlan(ITileGrid grid, Func<Cell, bool> isFree, BodySize size, Random random,
            out CellPath path)
        {
            path = null;
            var entrances = FreeEntrances(grid, isFree);
            var lobby = FreeLobby(grid, isFree);
            if (entrances.Count == 0 || lobby.Count == 0)
                return false;

            var entrance = entrances[random.Next(entrances.Count)];
            var target = lobby[random.Next(lobby.Count)];
            path = PathFinder.Shortest(grid, entrance, target,
                cell => CanPass(grid, cell, size) && (cell == target || isFree(cell)));
            return path != null;
        }

        private static List<Cell> FreeEntrances(ITileGrid grid, Func<Cell, bool> isFree)
        {
            var free = new List<Cell>();
            foreach (var entrance in grid.Entrances)
            {
                if (isFree(entrance))
                    free.Add(entrance);
            }
            return free;
        }

        private static List<Cell> FreeLobby(ITileGrid grid, Func<Cell, bool> isFree)
        {
            var free = new List<Cell>();
            for (var row = 0; row < grid.Rows; row++)
            {
                for (var col = 0; col < grid.Cols; col++)
                {
                    var cell = new Cell(col, row);
                    if (grid.At(cell).Type == TileType.Lobby && isFree(cell))
                        free.Add(cell);
                }
            }
            return free;
        }

        private static bool CanPass(ITileGrid grid, Cell cell, BodySize size)
        {
            var type = grid.At(cell).Type;
            return grid.IsWalkable(cell) && type != TileType.Door && !(type == TileType.Narrow && size == BodySize.Fat);
        }
    }
}
