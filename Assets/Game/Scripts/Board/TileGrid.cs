using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>A map's tiles, built by <see cref="MapParser"/>.</summary>
    public sealed class TileGrid : ITileGrid
    {
        private readonly Tile[] _tiles;
        private readonly List<Cell> _entrances = new List<Cell>();
        private readonly Dictionary<Gender, List<Cell>> _doors = new Dictionary<Gender, List<Cell>>();
        private readonly Dictionary<Gender, RectInt> _buildings = new Dictionary<Gender, RectInt>();

        /// <param name="tiles">Row by row from the top, <paramref name="cols"/> tiles a row.</param>
        public TileGrid(int cols, int rows, Tile[] tiles)
        {
            if (cols <= 0 || rows <= 0)
                throw new ArgumentOutOfRangeException(nameof(cols), $"A grid needs at least one tile, not {cols} x {rows}.");
            if (tiles == null || tiles.Length != cols * rows)
                throw new ArgumentException($"Expected {cols * rows} tiles for {cols} x {rows}.", nameof(tiles));

            Cols = cols;
            Rows = rows;
            Space = new BoardSpace(cols, rows);
            _tiles = (Tile[])tiles.Clone();

            foreach (Gender gender in Enum.GetValues(typeof(Gender)))
                _doors[gender] = new List<Cell>();
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < cols; col++)
                    Index(new Cell(col, row), _tiles[row * cols + col]);
            }
        }

        public int Cols { get; }
        public int Rows { get; }
        public BoardSpace Space { get; }
        public IReadOnlyList<Cell> Entrances => _entrances;

        public bool InBounds(Cell cell) => cell.Col >= 0 && cell.Col < Cols && cell.Row >= 0 && cell.Row < Rows;

        public Tile At(Cell cell)
        {
            if (!InBounds(cell))
                throw new ArgumentOutOfRangeException(nameof(cell), $"{cell} is outside the {Cols} x {Rows} map.");
            return _tiles[cell.Row * Cols + cell.Col];
        }

        public bool IsWalkable(Cell cell) => InBounds(cell) && At(cell).IsWalkable;

        public IReadOnlyList<Cell> DoorsOf(Gender gender) => _doors[gender];

        public RectInt BuildingOf(Gender gender) => _buildings.TryGetValue(gender, out var rect) ? rect : default;

        private void Index(Cell cell, Tile tile)
        {
            if (tile.Type == TileType.Entrance)
                _entrances.Add(cell);
            if (tile.Type == TileType.Door && tile.Gender.HasValue)
                _doors[tile.Gender.Value].Add(cell);
            if ((tile.Type == TileType.Building || tile.Type == TileType.Door) && tile.Gender.HasValue)
                Grow(tile.Gender.Value, cell);
        }

        private void Grow(Gender gender, Cell cell)
        {
            if (!_buildings.TryGetValue(gender, out var rect))
            {
                _buildings[gender] = new RectInt(cell.Col, cell.Row, 1, 1);
                return;
            }

            var xMin = Mathf.Min(rect.xMin, cell.Col);
            var yMin = Mathf.Min(rect.yMin, cell.Row);
            var xMax = Mathf.Max(rect.xMax, cell.Col + 1);
            var yMax = Mathf.Max(rect.yMax, cell.Row + 1);
            _buildings[gender] = new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
