using System;
using System.Collections.Generic;
using AF.Core;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Feedback;
using HoldIt.Pooling;
using UnityEngine;
using VContainer.Unity;

namespace HoldIt.Drawing
{
    /// <summary>
    /// Marks every cell of the path being drawn, its end in red while it cannot end there, and the cells still ahead of
    /// every walking character, so a path shortens as it is walked. Marks come from the scene's pools and are matched to
    /// the wanted list every frame by character and cell: a new mark pops in, a removed one shrinks away before it goes
    /// back to the pool, and one turning red shakes.
    /// </summary>
    internal sealed class PathMarkers : ILateTickable, IDisposable
    {
        private readonly List<(MarkerKey Key, MarkerLook Look)> _wanted = new List<(MarkerKey, MarkerLook)>();
        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly Dictionary<MarkerKey, PathMarker> _shown = new Dictionary<MarkerKey, PathMarker>();
        private readonly List<Vanishing> _vanishing = new List<Vanishing>();
        private readonly MarkerDiff _diff = new MarkerDiff();
        private readonly IPathDrawing _drawing;
        private readonly ICharacters _characters;
        private readonly ITileGrid _grid;
        private readonly DrawingConfig _config;
        private readonly IGameClock _clock;
        private readonly IFeedback _feedback;
        private readonly IPool<PathMarker> _pool;
        private CharacterId _drawer;
        private bool _wasDrawing;

        public PathMarkers(IPathDrawing drawing, ICharacters characters, ITileGrid grid, DrawingConfig config, IGameClock clock,
            IFeedback feedback, ILevelPools pools)
        {
            _drawing = drawing;
            _characters = characters;
            _grid = grid;
            _config = config;
            _clock = clock;
            _feedback = feedback;
            _pool = pools.For(config.Marker);
        }

        public void LateTick()
        {
            _wanted.Clear();
            _positions.Clear();
            AddWalkedPaths();
            AddDrawnPath();

            _diff.Update(_wanted);
            Remove();
            Add();
            for (var i = 0; i < _diff.Changed.Count; i++)
                Change(_diff.Changed[i].Key, _diff.Changed[i].Look);
            Place();
            ReleaseVanished();
            SendOff();
        }

        public void Dispose()
        {
            _shown.Clear();
            _vanishing.Clear();
            _diff.Clear();
        }

        private void Remove()
        {
            for (var i = 0; i < _diff.Removed.Count; i++)
            {
                var key = _diff.Removed[i];
                if (!_shown.TryGetValue(key, out var marker))
                    continue;
                _shown.Remove(key);
                marker.Vanish(_config.VanishSeconds);
                _vanishing.Add(new Vanishing(marker.Life, _clock.Time + _config.VanishSeconds));
            }
        }

        private void Add()
        {
            for (var i = 0; i < _diff.Added.Count; i++)
            {
                var (key, look) = _diff.Added[i];
                var marker = _pool.Spawn();
                _shown.Add(key, marker);
                marker.PopIn(_config.PopSeconds);
                if (look == MarkerLook.Blocked)
                    Shake(marker);
                else if (IsBeingDrawn(key))
                    _feedback.Play(FeedbackKind.PathCell);
            }
        }

        private void Change(MarkerKey key, MarkerLook look)
        {
            if (look == MarkerLook.Blocked && _shown.TryGetValue(key, out var marker))
                Shake(marker);
        }

        private void Shake(PathMarker marker)
        {
            marker.Shake(_config.ShakeSeconds, _config.ShakeStrength, _config.ShakeFrequency);
            _feedback.Play(FeedbackKind.PathBlocked);
        }

        /// <summary>A walking character's marks are not the player's doing; only the path being drawn ticks.</summary>
        private bool IsBeingDrawn(MarkerKey key) => _drawing.IsDrawing && key.Owner == _drawing.Drawer;

        /// <summary>The drawing just ended with the drawer on its way: the path was sent.</summary>
        private void SendOff()
        {
            if (_drawing.IsDrawing)
            {
                _wasDrawing = true;
                _drawer = _drawing.Drawer;
                return;
            }
            if (_wasDrawing && _characters.TryGet(_drawer, out var character) && character.Route != null)
                _feedback.Play(FeedbackKind.PathSent);
            _wasDrawing = false;
        }

        private void Place()
        {
            for (var i = 0; i < _wanted.Count; i++)
            {
                if (_shown.TryGetValue(_wanted[i].Key, out var marker))
                    marker.Place(_positions[i], _wanted[i].Look);
            }
        }

        /// <summary>A shrunk mark goes back by the game clock; the end of its tween is not the way back.</summary>
        private void ReleaseVanished()
        {
            for (var i = _vanishing.Count - 1; i >= 0; i--)
            {
                if (_clock.Time < _vanishing[i].Until && _vanishing[i].Life.IsCurrent)
                    continue;
                _vanishing[i].Life.Despawn();
                _vanishing.RemoveAt(i);
            }
        }

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
                    Want(character.Id, route[j], look, _config.WalkedMarkHeight);
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
                Want(_drawing.Drawer, cells[i], blocked ? MarkerLook.Blocked : look, _config.DrawnMarkHeight);
            }
        }

        private void Want(CharacterId owner, Cell cell, MarkerLook look, float height)
        {
            _wanted.Add((new MarkerKey(owner, cell), look));
            _positions.Add(_grid.Space.CellCenter(cell) + Vector3.up * height);
        }

        private static MarkerLook LookOf(Gender gender) => gender == Gender.Male ? MarkerLook.Male : MarkerLook.Female;

        private readonly struct Vanishing
        {
            public readonly PoolLife Life;
            public readonly float Until;

            public Vanishing(PoolLife life, float until)
            {
                Life = life;
                Until = until;
            }
        }
    }
}
