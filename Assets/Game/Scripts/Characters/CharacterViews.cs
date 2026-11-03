using System;
using System.Collections.Generic;
using AF.Core;
using HoldIt.Board;
using HoldIt.Restrooms;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace HoldIt.Characters
{
    /// <summary>
    /// Gives every new character a view from the scene's pools, standing on its cell and facing the camera, and has the
    /// view say why its character gave up a step. When a character leaves the board the view plays its way out: a visitor
    /// sits in its stall until the stall frees (<see cref="StallStateChanged"/>), then goes; after an accident it spins away
    /// and a puddle is left where it stood.
    /// </summary>
    internal sealed class CharacterViews : IInitializable, IDisposable
    {
        private readonly Dictionary<CharacterId, CharacterView> _views = new Dictionary<CharacterId, CharacterView>();
        private readonly Dictionary<(Gender, int), CharacterView> _seated = new Dictionary<(Gender, int), CharacterView>();
        private readonly IPools _pools;
        private readonly CharacterSet _set;
        private readonly ITileGrid _grid;
        private readonly ISubscriber<CharacterSpawned> _spawned;
        private readonly ISubscriber<StepRefused> _refused;
        private readonly ISubscriber<CharacterRemoved> _removed;
        private readonly ISubscriber<StallStateChanged> _stallChanged;
        private IDisposable _subscriptions;

        public CharacterViews(IPools pools, CharacterSet set, ITileGrid grid, ISubscriber<CharacterSpawned> spawned,
            ISubscriber<StepRefused> refused, ISubscriber<CharacterRemoved> removed,
            ISubscriber<StallStateChanged> stallChanged)
        {
            _pools = pools;
            _set = set;
            _grid = grid;
            _spawned = spawned;
            _refused = refused;
            _removed = removed;
            _stallChanged = stallChanged;
        }

        public void Initialize()
        {
            var bag = DisposableBag.CreateBuilder();
            _spawned.Subscribe(Show).AddTo(bag);
            _refused.Subscribe(Say).AddTo(bag);
            _removed.Subscribe(Remove).AddTo(bag);
            _stallChanged.Subscribe(StallChanged).AddTo(bag);
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

        /// <summary>The view plays the way out and goes back to its pool by itself.</summary>
        private void Remove(CharacterRemoved message)
        {
            if (!_views.Remove(message.Id, out var view) || !view.IsAlive())
                return;

            if (message.Reason == RemovalReason.Accident)
            {
                LeavePuddle(view.transform.position);
                view.Fail();
                return;
            }

            if (!view.TrySit(out var visit))
                return;
            var stall = (visit.Gender, visit.Stall);
            // A stall holds one visitor at a time; one still sitting there would already have been told it freed.
            if (_seated.Remove(stall, out var earlier) && earlier.IsAlive())
                earlier.Relieve();
            _seated[stall] = view;
        }

        private void StallChanged(StallStateChanged message)
        {
            if (message.State == StallState.Free && _seated.Remove((message.Gender, message.Stall), out var view) &&
                view.IsAlive())
                view.Relieve();
        }

        private void LeavePuddle(Vector3 at)
        {
            var rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            _pools.For(_set.Puddle).Spawn(new Vector3(at.x, 0f, at.z), rotation);
        }
    }
}
