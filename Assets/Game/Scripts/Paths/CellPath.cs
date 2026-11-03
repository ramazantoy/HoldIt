using System;
using System.Collections.Generic;
using HoldIt.Board;

namespace HoldIt.Paths
{
    /// <summary>
    /// A route a character can walk: at least two cells, each one a side neighbour of the one before. Whoever builds it
    /// (<see cref="PathDraft"/>, <see cref="PathFinder"/>) keeps it on cells that may be walked.
    /// </summary>
    public sealed class CellPath
    {
        private readonly Cell[] _cells;

        public CellPath(IReadOnlyList<Cell> cells)
        {
            if (cells == null)
                throw new ArgumentNullException(nameof(cells));
            if (cells.Count < 2)
                throw new ArgumentException($"A path needs at least two cells, not {cells.Count}.", nameof(cells));

            _cells = new Cell[cells.Count];
            for (var i = 0; i < cells.Count; i++)
            {
                if (i > 0 && !cells[i - 1].IsNeighbour(cells[i]))
                    throw new ArgumentException($"{cells[i - 1]} and {cells[i]} are not side neighbours.", nameof(cells));
                _cells[i] = cells[i];
            }
        }

        public int Count => _cells.Length;
        public Cell this[int index] => _cells[index];
        public Cell Start => _cells[0];
        public Cell End => _cells[_cells.Length - 1];

        public override string ToString() => string.Join(" ", _cells);
    }
}
