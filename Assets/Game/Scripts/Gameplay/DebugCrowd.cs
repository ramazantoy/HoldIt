#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using AF.Core;
using HoldIt.Board;
using HoldIt.Characters;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// Editor and development builds only: puts one character of every gender and size on the lobby, spread apart, until
    /// spawning (M6) brings them in from the entrance.
    /// </summary>
    internal sealed class DebugCrowd : IStartable
    {
        private readonly ICharacters _characters;
        private readonly ITileGrid _grid;

        public DebugCrowd(ICharacters characters, ITileGrid grid)
        {
            _characters = characters;
            _grid = grid;
        }

        public void Start()
        {
            var lobby = LobbyCells();
            if (lobby.Count == 0)
            {
                Log.Warning("The map has no lobby; no debug characters.");
                return;
            }

            var sizes = (BodySize[])Enum.GetValues(typeof(BodySize));
            var genders = (Gender[])Enum.GetValues(typeof(Gender));
            var count = sizes.Length * genders.Length;
            var gap = Math.Max(1, lobby.Count / count);
            for (var i = 0; i < count; i++)
                _characters.Spawn(genders[i % genders.Length], sizes[i / genders.Length], lobby[i * gap % lobby.Count]);
        }

        private List<Cell> LobbyCells()
        {
            var cells = new List<Cell>();
            for (var row = 0; row < _grid.Rows; row++)
            {
                for (var col = 0; col < _grid.Cols; col++)
                {
                    var cell = new Cell(col, row);
                    if (_grid.At(cell).Type == TileType.Lobby)
                        cells.Add(cell);
                }
            }
            return cells;
        }
    }
}
#endif
