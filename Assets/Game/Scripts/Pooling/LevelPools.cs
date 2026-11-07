using System;
using System.Collections.Generic;
using AF.Core;
using UnityEngine;

namespace HoldIt.Pooling
{
    /// <summary>
    /// Lends the root's pools to one level. Whatever the level spawned (a view, a mark, a puddle) goes back to its pool when
    /// the level is disposed: the views let go of the level's services as they are despawned, and their tweens end.
    /// Disposing does not touch a pool no level used. A level's scope is built after the old one is gone, never beside it:
    /// <see cref="IPool{T}.DespawnAll"/> would take another level's instances too.
    /// </summary>
    internal sealed class LevelPools : ILevelPools, IDisposable
    {
        private readonly IPersistentPools _pools;
        private readonly List<object> _used = new List<object>();
        private readonly List<Action> _despawnAll = new List<Action>();
        private bool _disposed;

        public LevelPools(IPersistentPools pools) => _pools = pools;

        public IPool<T> For<T>(T prefab) where T : Component
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LevelPools), "The level that owned these pools is gone.");

            var pool = _pools.For(prefab);
            if (!_used.Contains(pool))
            {
                _used.Add(pool);
                _despawnAll.Add(pool.DespawnAll);
            }
            return pool;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            foreach (var despawnAll in _despawnAll)
            {
                try
                {
                    despawnAll();
                }
                catch (Exception exception)
                {
                    // One pool failing must not leave the others' instances spawned.
                    Log.Exception(exception);
                }
            }
            _used.Clear();
            _despawnAll.Clear();
        }
    }
}
