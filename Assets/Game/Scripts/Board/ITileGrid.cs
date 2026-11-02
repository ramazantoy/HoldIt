using System.Collections.Generic;
using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>
    /// The current map's tiles. Read-only: what stands on a tile (characters, a queue) is kept by the systems that own it.
    /// The door and building queries assume a map that passes <see cref="MapCheck"/>.
    /// </summary>
    public interface ITileGrid
    {
        int Cols { get; }
        int Rows { get; }

        /// <summary>The cell's world placement (<see cref="BoardSpace"/>).</summary>
        BoardSpace Space { get; }

        /// <summary>The entrance tiles, row by row from the top.</summary>
        IReadOnlyList<Cell> Entrances { get; }

        bool InBounds(Cell cell);

        /// <summary>The tile at the cell. Throws outside the map.</summary>
        Tile At(Cell cell);

        /// <summary>False outside the map.</summary>
        bool IsWalkable(Cell cell);

        /// <summary>The doors of that gender, row by row from the top. A valid map has exactly one.</summary>
        IReadOnlyList<Cell> DoorsOf(Gender gender);

        /// <summary>
        /// The smallest rectangle (x = column, y = row) holding that gender's building tiles and doors; zero size if there
        /// are none. In a valid map it is the restroom.
        /// </summary>
        RectInt BuildingOf(Gender gender);
    }
}
