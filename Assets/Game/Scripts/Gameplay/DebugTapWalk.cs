#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using AF.Core;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Input;
using HoldIt.Paths;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// Editor and development builds only, until path drawing (M5): press on an Idle character's tile and release on
    /// another tile, and the character walks the shortest drawable route there (a door only as the last tile). The route
    /// may lead to the wrong door or take a Fat character through a narrow passage, which the step rules (M3) refuse.
    /// </summary>
    internal sealed class DebugTapWalk : IInitializable, IDisposable
    {
        private readonly ICharacters _characters;
        private readonly ITileGrid _grid;
        private readonly IBoardPicker _picker;
        private readonly ISubscriber<PointerDown> _down;
        private readonly ISubscriber<PointerUp> _up;
        private IDisposable _subscriptions;
        private CharacterId _picked;
        private bool _hasPick;

        public DebugTapWalk(ICharacters characters, ITileGrid grid, IBoardPicker picker, ISubscriber<PointerDown> down,
            ISubscriber<PointerUp> up)
        {
            _characters = characters;
            _grid = grid;
            _picker = picker;
            _down = down;
            _up = up;
        }

        public void Initialize()
        {
            var bag = DisposableBag.CreateBuilder();
            _down.Subscribe(message => Pick(message.Position)).AddTo(bag);
            _up.Subscribe(message => Release(message.Position)).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Dispose() => _subscriptions?.Dispose();

        private void Pick(Vector2 position)
        {
            _hasPick = false;
            if (!_picker.TryPick(position, out var cell))
                return;

            foreach (var character in _characters.All)
            {
                if (character.State == CharacterState.Idle && character.Cell == cell)
                {
                    _picked = character.Id;
                    _hasPick = true;
                    return;
                }
            }
        }

        private void Release(Vector2 position)
        {
            if (!_hasPick)
                return;
            _hasPick = false;
            if (!_picker.TryPick(position, out var target) || !_characters.TryGet(_picked, out var character) ||
                target == character.Cell)
                return;

            var path = PathFinder.Shortest(_grid, character.Cell, target, cell => CanEnter(cell, target));
            if (path == null)
                Log.Info($"No drawable route for {_picked} from {character.Cell} to {target}.");
            else if (!_characters.TryWalk(_picked, path))
                Log.Info($"{_picked} did not take the route {path}.");
        }

        private bool CanEnter(Cell cell, Cell target) =>
            PathRules.IsDrawable(_grid, cell) && (cell == target || _grid.At(cell).Type != TileType.Door);
    }
}
#endif
