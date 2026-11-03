using System;
using System.Collections.Generic;
using AF.Core;
using HoldIt.Board;
using HoldIt.Characters;
using UnityEngine;
using VContainer.Unity;

namespace HoldIt.Drawing
{
    /// <summary>
    /// Marks every cell of the path being drawn, its end in red while it cannot end there, and the cells still ahead of
    /// every walking or returning character, so a path shortens as it is walked. Marks come from the scene's pools and are
    /// matched to the wanted list every frame.
    /// </summary>
    internal sealed class PathMarkers : ILateTickable, IDisposable
    {
        private readonly List<(Vector3 Position, MarkerLook Look)> _wanted = new List<(Vector3, MarkerLook)>();
        private readonly List<PathMarker> _shown = new List<PathMarker>();
        private readonly IPathDrawing _drawing;
        private readonly ICharacters _characters;
        private readonly ITileGrid _grid;
        private readonly DrawingConfig _config;
        private readonly IPool<PathMarker> _pool;

        public PathMarkers(IPathDrawing drawing, ICharacters characters, ITileGrid grid, DrawingConfig config, IPools pools)
        {
            _drawing = drawing;
            _characters = characters;
            _grid = grid;
            _config = config;
            _pool = pools.For(config.Marker);
        }

        public void LateTick()
        {
            _wanted.Clear();
            AddWalkedPaths();
            AddDrawnPath();

            for (var i = 0; i < _wanted.Count; i++)
            {
                if (i == _shown.Count)
                    _shown.Add(_pool.Spawn());
                _shown[i].Place(_wanted[i].Position, _wanted[i].Look);
            }
            for (var i = _shown.Count - 1; i >= _wanted.Count; i--)
            {
                _pool.Despawn(_shown[i]);
                _shown.RemoveAt(i);
            }
        }

        public void Dispose() => _shown.Clear();

        private void AddWalkedPaths()
        {
            var all = _characters.All;
            for (var i = 0; i < all.Count; i++)
            {
                var character = all[i];
                var route = character.Route;
                if (route == null)
                    continue;
                var look = LookOf(character.Gender);
                for (var j = character.RouteIndex + 1; j < route.Count; j++)
                    _wanted.Add((At(route[j], _config.WalkedMarkHeight), look));
            }
        }

        private void AddDrawnPath()
        {
            if (!_drawing.IsDrawing || !_characters.TryGet(_drawing.Drawer, out var drawer))
                return;

            var cells = _drawing.Cells;
            var look = LookOf(drawer.Gender);
            var last = cells.Count - 1;
            for (var i = 0; i < cells.Count; i++)
            {
                var blocked = i == last && i > 0 && !_drawing.EndIsFree;
                _wanted.Add((At(cells[i], _config.DrawnMarkHeight), blocked ? MarkerLook.Blocked : look));
            }
        }

        private Vector3 At(Cell cell, float height) => _grid.Space.CellCenter(cell) + Vector3.up * height;

        private static MarkerLook LookOf(Gender gender) => gender == Gender.Male ? MarkerLook.Male : MarkerLook.Female;
    }
}
