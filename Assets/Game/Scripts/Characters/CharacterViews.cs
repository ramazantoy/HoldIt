using System;
using AF.Core;
using HoldIt.Board;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Characters
{
    /// <summary>Gives every new character a view from the scene's pools, standing on its cell and facing the camera.</summary>
    internal sealed class CharacterViews : IInitializable, IDisposable
    {
        private readonly IPools _pools;
        private readonly CharacterSet _set;
        private readonly ITileGrid _grid;
        private readonly ISubscriber<CharacterSpawned> _spawned;
        private IDisposable _subscription;

        public CharacterViews(IPools pools, CharacterSet set, ITileGrid grid, ISubscriber<CharacterSpawned> spawned)
        {
            _pools = pools;
            _set = set;
            _grid = grid;
            _spawned = spawned;
        }

        public void Initialize() => _subscription = _spawned.Subscribe(Show);

        public void Dispose() => _subscription?.Dispose();

        private void Show(CharacterSpawned message)
        {
            var pool = _pools.For(_set.Prefab(message.Gender, message.Size));
            var view = pool.Spawn(_grid.Space.CellCenter(message.Cell), Direction.South.Rotation());
            view.Bind(message.Id);
        }
    }
}
