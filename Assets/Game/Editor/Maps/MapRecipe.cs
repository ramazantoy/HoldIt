using System;
using UnityEngine;

namespace HoldIt.Editor.Maps
{
    /// <summary>Where a map's two restrooms stand.</summary>
    public enum RestroomPlacement
    {
        /// <summary>Both against the top edge, side by side, their doors facing down.</summary>
        TopSideBySide,

        /// <summary>One against the left edge and one against the right, their doors facing in.</summary>
        Sides,

        /// <summary>One in a top corner, the other against the opposite side lower down: far apart.</summary>
        TopAndSide,

        /// <summary>Any of the others, picked by the seed.</summary>
        Any,
    }

    /// <summary>The map edges entrances may be on; side entrances stay low, by the lobby.</summary>
    [Flags]
    public enum EntranceEdges
    {
        None = 0,
        Bottom = 1,
        Left = 2,
        Right = 4,
    }

    /// <summary>
    /// What a generated map looks like (<see cref="MapGenerator"/>): its size, restrooms, entrances, lobby and the walls in
    /// between, and how long its walks should come out (<see cref="MapMetrics"/>). Ranges are inclusive, min then max.
    /// </summary>
    [Serializable]
    public struct MapRecipe
    {
        [SerializeField, Tooltip("Columns; at most the map limits' (finger-sized tiles on a phone).")]
        private Vector2Int _cols;

        [SerializeField, Tooltip("Rows; at most the map limits'.")]
        private Vector2Int _rows;

        [SerializeField] private RestroomPlacement _placement;

        [SerializeField, Tooltip("Stalls per restroom (1–3).")]
        private Vector2Int _stalls;

        [SerializeField, Tooltip("Tiles a restroom may be wider than its stalls need.")]
        private Vector2Int _extraWidth;

        [SerializeField, Tooltip("Separate entrances, each one or two tiles.")]
        private Vector2Int _entrances;

        [SerializeField] private EntranceEdges _entranceEdges;

        [SerializeField, Tooltip("Lobby tiles, grown from the entrances.")]
        private Vector2Int _lobby;

        [SerializeField, Tooltip("Walls across the map between the lobby and the restrooms, each with gaps.")]
        private Vector2Int _barriers;

        [SerializeField, Tooltip("Narrow gaps in each barrier: short ways that Fat characters cannot take.")]
        private Vector2Int _narrowGaps;

        [SerializeField, Tooltip("Open gaps alternate from one end of the barriers to the other, so the way winds.")]
        private bool _winding;

        [SerializeField, Tooltip("Small wall blocks scattered on the floor.")]
        private Vector2Int _blocks;

        [SerializeField, Tooltip("Average steps from a lobby tile to its door (MapMetrics.MeanPath).")]
        private Vector2 _meanPath;

        [SerializeField, Tooltip("Extra steps Fat characters walk on average (MapMetrics.FatDetour): enough for the narrow gaps to matter, not so many they cannot make it.")]
        private Vector2 _fatDetour;

        public MapRecipe(Vector2Int cols, Vector2Int rows, RestroomPlacement placement, Vector2Int stalls,
            Vector2Int extraWidth, Vector2Int entrances, EntranceEdges entranceEdges, Vector2Int lobby, Vector2Int barriers,
            Vector2Int narrowGaps, bool winding, Vector2Int blocks, Vector2 meanPath, Vector2 fatDetour)
        {
            _cols = cols;
            _rows = rows;
            _placement = placement;
            _stalls = stalls;
            _extraWidth = extraWidth;
            _entrances = entrances;
            _entranceEdges = entranceEdges;
            _lobby = lobby;
            _barriers = barriers;
            _narrowGaps = narrowGaps;
            _winding = winding;
            _blocks = blocks;
            _meanPath = meanPath;
            _fatDetour = fatDetour;
        }

        public Vector2Int Cols => _cols;
        public Vector2Int Rows => _rows;
        public RestroomPlacement Placement => _placement;
        public Vector2Int Stalls => _stalls;
        public Vector2Int ExtraWidth => _extraWidth;
        public Vector2Int Entrances => _entrances;
        public EntranceEdges EntranceEdges => _entranceEdges;
        public Vector2Int Lobby => _lobby;
        public Vector2Int Barriers => _barriers;
        public Vector2Int NarrowGaps => _narrowGaps;
        public bool Winding => _winding;
        public Vector2Int Blocks => _blocks;
        public Vector2 MeanPath => _meanPath;
        public Vector2 FatDetour => _fatDetour;
    }
}
