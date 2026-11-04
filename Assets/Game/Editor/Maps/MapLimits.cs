using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldIt.Editor.Maps
{
    /// <summary>
    /// How big a map can be before the board camera (<see cref="HoldIt.Board.CameraFit"/>) shrinks its tiles on a phone. The
    /// fit is limited by the width: more columns make every tile smaller than a finger. More rows only shrink the tiles once
    /// a 9:16 screen is limited by the height. Smaller maps are fine: their tiles grow.
    /// </summary>
    [Serializable]
    public struct MapLimits
    {
        [SerializeField, Min(1), Tooltip("The most columns: wider maps get tiles too small to draw on with a finger.")]
        private int _maxCols;

        [SerializeField, Min(1), Tooltip("The most rows before a 9:16 phone fits the board by its height and the tiles shrink.")]
        private int _maxRows;

        public MapLimits(int maxCols, int maxRows)
        {
            _maxCols = maxCols;
            _maxRows = maxRows;
        }

        /// <summary>10 columns (Lobby's) and 20 rows: a 10-wide map is fitted by its width on a 9:16 phone up to 20 rows.</summary>
        public static MapLimits Default => new MapLimits(10, 20);

        public int MaxCols => _maxCols;
        public int MaxRows => _maxRows;

        /// <summary>What a map of that size exceeds; empty when it fits.</summary>
        public IReadOnlyList<string> Problems(int cols, int rows)
        {
            var problems = new List<string>();
            if (cols > _maxCols)
                problems.Add($"It is {cols} tiles wide; on a phone its tiles are smaller than on a {_maxCols}-wide map.");
            if (rows > _maxRows)
                problems.Add($"It is {rows} tiles tall; above {_maxRows} a phone fits it by its height and the tiles shrink.");
            return problems;
        }
    }
}
