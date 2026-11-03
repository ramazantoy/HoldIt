using System;
using System.Collections.Generic;
using AF.Core;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Input;
using HoldIt.Paths;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace HoldIt.Drawing
{
    /// <summary>
    /// The player's finger draws paths. A press on or near an Idle character (<see cref="CharacterPick"/>) starts a path
    /// from its cell; dragging extends and cuts it (<see cref="PathDraft"/>); letting go cuts it back to the last cell the
    /// character can end on, which someone else may stand on or have reserved, and sends the character if two or more
    /// cells are left. The path may cross other characters: the step rules deal with them on the way.
    /// </summary>
    internal sealed class PathDrawing : IPathDrawing, IInitializable, IDisposable
    {
        private static readonly IReadOnlyList<Cell> _noCells = Array.Empty<Cell>();

        private readonly List<PickCandidate> _candidates = new List<PickCandidate>();
        private readonly ICharacters _characters;
        private readonly ITileGrid _grid;
        private readonly IBoardPicker _picker;
        private readonly IBoardProjection _projection;
        private readonly DrawingConfig _config;
        private readonly ISubscriber<PointerDown> _down;
        private readonly ISubscriber<PointerMoved> _moved;
        private readonly ISubscriber<PointerUp> _up;
        private readonly PathDraft _draft;
        private IDisposable _subscriptions;

        public PathDrawing(ICharacters characters, ITileGrid grid, IBoardPicker picker, IBoardProjection projection,
            DrawingConfig config, ISubscriber<PointerDown> down, ISubscriber<PointerMoved> moved, ISubscriber<PointerUp> up)
        {
            _characters = characters;
            _grid = grid;
            _picker = picker;
            _projection = projection;
            _config = config;
            _down = down;
            _moved = moved;
            _up = up;
            _draft = new PathDraft(grid);
        }

        public bool IsDrawing { get; private set; }
        public CharacterId Drawer { get; private set; }
        public IReadOnlyList<Cell> Cells => IsDrawing ? _draft.Cells : _noCells;
        public bool EndIsFree => !IsDrawing || _characters.CanEndOn(Drawer, _draft.Last);

        public void Initialize()
        {
            var bag = DisposableBag.CreateBuilder();
            _down.Subscribe(message => Begin(message.Position)).AddTo(bag);
            _moved.Subscribe(message => Extend(message.Position)).AddTo(bag);
            _up.Subscribe(message => Finish(message.Position)).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Dispose() => _subscriptions?.Dispose();

        private void Begin(Vector2 position)
        {
            IsDrawing = false;
            FillCandidates();
            if (!CharacterPick.TryPick(_candidates, position, out var id) || !_characters.TryGet(id, out var character))
                return;

            Drawer = id;
            _draft.Begin(character.Cell);
            IsDrawing = true;
        }

        private void Extend(Vector2 position)
        {
            if (IsDrawing && _picker.TryPickClamped(position, out var cell))
                _draft.ExtendToward(cell);
        }

        private void Finish(Vector2 position)
        {
            if (!IsDrawing)
                return;

            Extend(position);
            IsDrawing = false;
            _draft.TrimEnd(cell => _characters.CanEndOn(Drawer, cell));
            if (_draft.CanCommit && !_characters.TryWalk(Drawer, _draft.ToPath()))
                Log.Info($"{Drawer} did not take the path {_draft.ToPath()}.");
        }

        /// <summary>Every Idle character, as a line from its feet to its head on the screen.</summary>
        private void FillCandidates()
        {
            _candidates.Clear();
            var all = _characters.All;
            for (var i = 0; i < all.Count; i++)
            {
                var character = all[i];
                if (character.State != CharacterState.Idle)
                    continue;

                var feet = _grid.Space.CellCenter(character.Cell);
                if (_projection.TryWorldToScreen(feet, out var feetOnScreen) &&
                    _projection.TryWorldToScreen(feet + Vector3.up * _config.PickHeight, out var headOnScreen) &&
                    _projection.TryWorldToScreen(feet + Vector3.right, out var nextTileOnScreen))
                {
                    var radius = _config.PickRadius * Vector2.Distance(feetOnScreen, nextTileOnScreen);
                    _candidates.Add(new PickCandidate(character.Id, feetOnScreen, headOnScreen, radius));
                }
            }
        }
    }
}
