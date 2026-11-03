#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using AF.Core;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Paths;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// Editor and development builds only, until spawning (M6): every few seconds a random character comes in at a free
    /// entrance and walks the shortest way around everyone to a free lobby cell, which its path reserves, so nobody can be
    /// drawn onto it. Someone may still step in its way later; then it stops where it is, like everyone else.
    /// </summary>
    internal sealed class DebugArrivals : ITickable
    {
        private const float IntervalSeconds = 6f;
        private const int MaxCharacters = 9;

        private readonly List<Cell> _entrances = new List<Cell>();
        private readonly List<Cell> _lobby = new List<Cell>();
        private readonly Random _random = new Random();
        private readonly ICharacters _characters;
        private readonly ITileGrid _grid;
        private readonly IGameClock _clock;
        private float _next = IntervalSeconds;

        public DebugArrivals(ICharacters characters, ITileGrid grid, IGameClock clock)
        {
            _characters = characters;
            _grid = grid;
            _clock = clock;
        }

        public void Tick()
        {
            if (_clock.Time < _next)
                return;
            _next = _clock.Time + IntervalSeconds;
            if (_characters.All.Count < MaxCharacters)
                TryArrive();
        }

        private void TryArrive()
        {
            FreeCells();
            if (_entrances.Count == 0 || _lobby.Count == 0)
                return;

            var gender = _random.Next(2) == 0 ? Gender.Male : Gender.Female;
            var size = (BodySize)_random.Next(Enum.GetValues(typeof(BodySize)).Length);
            var entrance = _entrances[_random.Next(_entrances.Count)];
            var target = _lobby[_random.Next(_lobby.Count)];
            var path = PathFinder.Shortest(_grid, entrance, target,
                cell => CanPass(cell, size) && (cell == target || _characters.IsFree(cell)));
            if (path == null)
                return;

            var id = _characters.Spawn(gender, size, entrance);
            if (!_characters.TryWalk(id, path))
                Log.Info($"Arriving {id} did not take the path {path}.");
        }

        private void FreeCells()
        {
            _entrances.Clear();
            foreach (var entrance in _grid.Entrances)
            {
                if (_characters.IsFree(entrance))
                    _entrances.Add(entrance);
            }

            _lobby.Clear();
            for (var row = 0; row < _grid.Rows; row++)
            {
                for (var col = 0; col < _grid.Cols; col++)
                {
                    var cell = new Cell(col, row);
                    if (_grid.At(cell).Type == TileType.Lobby && _characters.IsFree(cell))
                        _lobby.Add(cell);
                }
            }
        }

        private bool CanPass(Cell cell, BodySize size)
        {
            var type = _grid.At(cell).Type;
            return _grid.IsWalkable(cell) && type != TileType.Door && !(type == TileType.Narrow && size == BodySize.Fat);
        }
    }
}
#endif
