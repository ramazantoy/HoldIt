using System.Collections.Generic;

namespace HoldIt.Board
{
    /// <summary>
    /// Reads a map written as rows of characters, top row first, one character a tile:
    /// <c>.</c> floor, <c>L</c> lobby, <c>E</c> entrance, <c>#</c> wall, <c>n</c> narrow passage, <c>M</c>/<c>W</c> men's and
    /// women's building, <c>m</c>/<c>w</c> their doors. It checks only the format; whether the map is playable is
    /// <see cref="MapCheck"/>'s job.
    /// </summary>
    public static class MapParser
    {
        private const char FLOOR = '.';
        private const char LOBBY = 'L';
        private const char ENTRANCE = 'E';
        private const char WALL = '#';
        private const char NARROW = 'n';
        private const char MALE_BUILDING = 'M';
        private const char FEMALE_BUILDING = 'W';
        private const char MALE_DOOR = 'm';
        private const char FEMALE_DOOR = 'w';

        /// <exception cref="MapFormatException">No rows, an empty or uneven row, or an unknown character.</exception>
        public static TileGrid Parse(IReadOnlyList<string> rows)
        {
            if (rows == null || rows.Count == 0)
                throw new MapFormatException("The map has no rows.");

            var cols = rows[0]?.Length ?? 0;
            if (cols == 0)
                throw new MapFormatException("Row 0 is empty.");

            var tiles = new Tile[cols * rows.Count];
            for (var row = 0; row < rows.Count; row++)
            {
                var line = rows[row] ?? string.Empty;
                if (line.Length != cols)
                    throw new MapFormatException($"Row {row} has {line.Length} tiles; row 0 has {cols}.");

                for (var col = 0; col < cols; col++)
                {
                    if (!TryRead(line[col], out var tile))
                        throw new MapFormatException($"Unknown tile '{line[col]}' at column {col}, row {row}.");
                    tiles[row * cols + col] = tile;
                }
            }

            return new TileGrid(cols, rows.Count, tiles);
        }

        private static bool TryRead(char symbol, out Tile tile)
        {
            switch (symbol)
            {
                case FLOOR: tile = new Tile(TileType.Floor); return true;
                case LOBBY: tile = new Tile(TileType.Lobby); return true;
                case ENTRANCE: tile = new Tile(TileType.Entrance); return true;
                case WALL: tile = new Tile(TileType.Wall); return true;
                case NARROW: tile = new Tile(TileType.Narrow); return true;
                case MALE_BUILDING: tile = new Tile(TileType.Building, Gender.Male); return true;
                case FEMALE_BUILDING: tile = new Tile(TileType.Building, Gender.Female); return true;
                case MALE_DOOR: tile = new Tile(TileType.Door, Gender.Male); return true;
                case FEMALE_DOOR: tile = new Tile(TileType.Door, Gender.Female); return true;
                default: tile = default; return false;
            }
        }
    }
}
