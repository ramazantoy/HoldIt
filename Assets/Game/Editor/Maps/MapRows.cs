using System;
using System.Collections.Generic;
using HoldIt.Board;

namespace HoldIt.Editor.Maps
{
    /// <summary>Edits of a map's rows for the map Inspector; each returns new rows and leaves the old ones as they were.</summary>
    public static class MapRows
    {
        /// <summary>The rows with <paramref name="symbol"/> on <paramref name="cell"/>. Throws for a cell outside them.</summary>
        public static string[] Set(IReadOnlyList<string> rows, Cell cell, char symbol)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));
            if (cell.Row < 0 || cell.Row >= rows.Count || cell.Col < 0 || cell.Col >= (rows[cell.Row]?.Length ?? 0))
                throw new ArgumentOutOfRangeException(nameof(cell), cell, "The cell is outside the rows.");

            var copy = Copy(rows);
            var line = copy[cell.Row].ToCharArray();
            line[cell.Col] = symbol;
            copy[cell.Row] = new string(line);
            return copy;
        }

        /// <summary>
        /// The rows cut or grown to <paramref name="cols"/> × <paramref name="rowCount"/> from the top left: what stays keeps
        /// its place, new tiles are floor.
        /// </summary>
        public static string[] Resize(IReadOnlyList<string> rows, int cols, int rowCount)
        {
            if (cols < 1 || rowCount < 1)
                throw new ArgumentOutOfRangeException(cols < 1 ? nameof(cols) : nameof(rowCount), "A map has a tile at least.");

            var resized = new string[rowCount];
            for (var row = 0; row < rowCount; row++)
            {
                var line = row < (rows?.Count ?? 0) ? rows[row] ?? string.Empty : string.Empty;
                resized[row] = line.Length >= cols ? line.Substring(0, cols) : line + new string(MapSymbols.Floor, cols - line.Length);
            }
            return resized;
        }

        /// <summary>Rows pasted as text, one a line; blank lines and spaces around a line are dropped.</summary>
        public static string[] Parse(string text)
        {
            var rows = new List<string>();
            foreach (var line in (text ?? string.Empty).Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length > 0)
                    rows.Add(trimmed);
            }
            return rows.ToArray();
        }

        private static string[] Copy(IReadOnlyList<string> rows)
        {
            var copy = new string[rows.Count];
            for (var i = 0; i < rows.Count; i++)
                copy[i] = rows[i] ?? string.Empty;
            return copy;
        }
    }
}
