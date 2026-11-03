using System;
using System.Collections.Generic;
using AF.Core;
using HoldIt.Board;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Characters
{
    /// <summary>
    /// Gives every new character a view from the scene's pools, standing on its cell and facing the camera, and has the
    /// view say why its character gave up a step.
    /// </summary>
    internal sealed class CharacterViews : IInitializable, IDisposable
    {
        private readonly Dictionary<CharacterId, CharacterView> _views = new Dictionary<CharacterId, CharacterView>();
        private readonly IPools _pools;
        private readonly CharacterSet _set;
        private readonly ITileGrid _grid;
        private readonly ISubscriber<CharacterSpawned> _spawned;
        private readonly ISubscriber<StepRefused> _refused;
        private IDisposable _subscriptions;

        public CharacterViews(IPools pools, CharacterSet set, ITileGrid grid, ISubscriber<CharacterSpawned> spawned,
            ISubscriber<StepRefused> refused)
        {
            _pools = pools;
            _set = set;
            _grid = grid;
            _spawned = spawned;
            _refused = refused;
        }

        public void Initialize()
        {
            var bag = DisposableBag.CreateBuilder();
            _spawned.Subscribe(Show).AddTo(bag);
            _refused.Subscribe(Say).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Dispose() => _subscriptions?.Dispose();

        private void Show(CharacterSpawned message)
        {
            var pool = _pools.For(_set.Prefab(message.Gender, message.Size));
            var view = pool.Spawn(_grid.Space.CellCenter(message.Cell), Direction.South.Rotation());
            view.Bind(message.Id);
            _views[message.Id] = view;
        }

        private void Say(StepRefused message)
        {
            if (_views.TryGetValue(message.Id, out var view) && view.IsAlive())
                view.Say(message.Reason);
        }
    }
}
