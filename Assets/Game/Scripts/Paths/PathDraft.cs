using System;
using System.Collections.Generic;
using HoldIt.Board;
using UnityEngine;

namespace HoldIt.Paths
{
    /// <summary>
    /// A path being drawn, one side step at a time. Moving toward a cell fills a diagonal jump step by step, stops at walls,
    /// buildings and entrances (<see cref="PathRules"/>), and ends at a door. Reaching a cell the path already holds cuts
    /// the path back to it, so dragging back to the previous cell removes the last one and the path never crosses itself.
    /// </summary>
    public sealed class PathDraft
    {
        private readonly ITileGrid _grid;
        private readonly List<Cell> _cells = new List<Cell>();

        public PathDraft(ITileGrid grid) => _grid = grid ?? throw new ArgumentNullException(nameof(grid));

        public IReadOnlyList<Cell> Cells => _cells;

        /// <summary>The cell the path ends on. Throws before <see cref="Begin"/>.</summary>
        public Cell Last => _cells.Count > 0 ? _cells[_cells.Count - 1] : throw NotBegun();

        /// <summary>True once the path leaves its start cell.</summary>
        public bool CanCommit => _cells.Count >= 2;

        /// <summary>Starts over from the cell a character stands on.</summary>
        public void Begin(Cell start)
        {
            if (!_grid.InBounds(start))
                throw new ArgumentOutOfRangeException(nameof(start), $"{start} is outside the {_grid.Cols} x {_grid.Rows} map.");
            _cells.Clear();
            _cells.Add(start);
        }

        /// <summary>
        /// Extends or cuts the path toward <paramref name="target"/> (clamped into the map). The larger of the column and
        /// row gaps is stepped first, the other one when that side is blocked. True when the path changed.
        /// </summary>
        public bool ExtendToward(Cell target)
        {
            if (_cells.Count == 0)
                throw NotBegun();

            target = new Cell(Mathf.Clamp(target.Col, 0, _grid.Cols - 1), Mathf.Clamp(target.Row, 0, _grid.Rows - 1));
            var changed = false;
            for (var guard = _grid.Cols * _grid.Rows; guard > 0; guard--)
            {
                var last = Last;
                if (last == target)
                    break;
                if (CutBackTo(target))
                {
                    changed = true;
                    break;
                }
                if (_grid.At(last).Type == TileType.Door)
                    break;
                if (!StepToward(last, target))
                    break;
                changed = true;
            }
            return changed;
        }

        /// <summary>
        /// Drops cells from the end until the last one passes <paramref name="canEndOn"/> or only the start is left. True
        /// when the path changed.
        /// </summary>
        public bool TrimEnd(Func<Cell, bool> canEndOn)
        {
            if (canEndOn == null)
                throw new ArgumentNullException(nameof(canEndOn));
            if (_cells.Count == 0)
                throw NotBegun();

            var count = _cells.Count;
            while (_cells.Count > 1 && !canEndOn(Last))
                _cells.RemoveAt(_cells.Count - 1);
            return _cells.Count != count;
        }

        /// <summary>The finished path. Throws unless <see cref="CanCommit"/>.</summary>
        public CellPath ToPath() =>
            CanCommit ? new CellPath(_cells) : throw new InvalidOperationException("A path needs at least two cells.");

        private bool StepToward(Cell last, Cell target)
        {
            var colGap = target.Col - last.Col;
            var rowGap = target.Row - last.Row;
            var colStep = new Cell(last.Col + Math.Sign(colGap), last.Row);
            var rowStep = new Cell(last.Col, last.Row + Math.Sign(rowGap));
            var colFirst = Math.Abs(colGap) >= Math.Abs(rowGap);

            return colFirst
                ? (colGap != 0 && TryStep(colStep)) || (rowGap != 0 && TryStep(rowStep))
                : (rowGap != 0 && TryStep(rowStep)) || (colGap != 0 && TryStep(colStep));
        }

        private bool TryStep(Cell cell)
        {
            if (CutBackTo(cell))
                return true;
            if (!PathRules.IsDrawable(_grid, cell))
                return false;
            _cells.Add(cell);
            return true;
        }

        private bool CutBackTo(Cell cell)
        {
            var index = _cells.IndexOf(cell);
            if (index < 0)
                return false;
            _cells.RemoveRange(index + 1, _cells.Count - index - 1);
            return true;
        }

        private static InvalidOperationException NotBegun() => new InvalidOperationException("Begin the path first.");
    }
}
