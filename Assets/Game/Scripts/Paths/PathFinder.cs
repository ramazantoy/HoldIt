using System;
using System.Collections.Generic;
using HoldIt.Board;

namespace HoldIt.Paths
{
    /// <summary>Shortest routes over the grid, for walks the player does not draw.</summary>
    public static class PathFinder
    {
        /// <summary>
        /// The shortest side-step route from <paramref name="from"/> to <paramref name="to"/> that enters only cells
        /// <paramref name="canEnter"/> allows (the start is not asked). Neighbours are tried clockwise from North, so the
        /// same request always gives the same route. Null when <paramref name="to"/> cannot be reached or is the start.
        /// </summary>
        public static CellPath Shortest(ITileGrid grid, Cell from, Cell to, Func<Cell, bool> canEnter)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));
            if (canEnter == null)
                throw new ArgumentNullException(nameof(canEnter));
            if (from == to || !grid.InBounds(from) || !grid.InBounds(to))
                return null;

            var previous = new Dictionary<Cell, Cell> { [from] = from };
            var queue = new Queue<Cell>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (cell == to)
                    return Trace(previous, from, to);

                foreach (var direction in Directions.All)
                {
                    var next = cell.Step(direction);
                    if (!grid.InBounds(next) || previous.ContainsKey(next) || !Passage.CanStep(grid, cell, next) ||
                        !canEnter(next))
                        continue;
                    previous[next] = cell;
                    queue.Enqueue(next);
                }
            }
            return null;
        }

        private static CellPath Trace(Dictionary<Cell, Cell> previous, Cell from, Cell to)
        {
            var cells = new List<Cell> { to };
            for (var cell = to; cell != from; cell = previous[cell])
                cells.Add(previous[cell]);
            cells.Reverse();
            return new CellPath(cells);
        }
    }
}
