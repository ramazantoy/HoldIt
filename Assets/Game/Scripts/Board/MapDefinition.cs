using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>
    /// A map as rows of characters, top row (farthest from the camera) first. The characters are listed on
    /// <see cref="MapParser"/>; "AF/Validate All" reports a map that does not parse or cannot be played (<see cref="MapCheck"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Board/Map", fileName = "Map")]
    public sealed class MapDefinition : ScriptableObject
    {
        [SerializeField, Tooltip(". floor, L lobby, E entrance, # wall, n narrow passage, M/W men's/women's building, m/w their doors. Every row has the same length.")]
        private string[] _rows = Array.Empty<string>();

        [SerializeField, Min(1), Tooltip("How many stalls each restroom has. They share the restroom's width; \"AF/Validate All\" reports a restroom too narrow for them.")]
        private int _stallsPerRestroom = 2;

        public IReadOnlyList<string> Rows => _rows;
        public int StallsPerRestroom => _stallsPerRestroom;
    }
}
