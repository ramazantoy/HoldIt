using System.Collections.Generic;
using AF.Core;
using UnityEngine;

namespace HoldIt.Pooling
{
    /// <summary>What a level can ask of a pool it used: the instances that are on the board right now.</summary>
    internal interface ISpawnedSource
    {
        /// <summary>Adds the transform of every instance of this pool that is spawned and active.</summary>
        void CollectActive(List<Transform> into);
    }

    /// <summary>
    /// A pool that remembers what it spawned, to tell the level what stands on its board. Everything else is the wrapped
    /// pool's. An instance that goes back to its pool on its own (a puff, a marker) is not told to this wrapper, so a
    /// remembered instance counts only while it is active: the pool keeps despawned ones inactive.
    /// </summary>
    internal sealed class TrackingPool<T> : IPool<T>, ISpawnedSource where T : Component
    {
        private readonly IPool<T> _inner;
        // A pool reuses its instances, so the set is as large as the pool and no larger.
        private readonly HashSet<T> _spawned = new HashSet<T>();

        public TrackingPool(IPool<T> inner) => _inner = inner;

        public T Prefab => _inner.Prefab;

        public int CountActive => _inner.CountActive;

        public int CountInactive => _inner.CountInactive;

        public T Spawn(Transform parent = null) => Track(_inner.Spawn(parent));

        public T Spawn(Vector3 position, Quaternion rotation, Transform parent = null) =>
            Track(_inner.Spawn(position, rotation, parent));

        public void Despawn(T instance)
        {
            _inner.Despawn(instance);
            _spawned.Remove(instance);
        }

        public void DespawnAll()
        {
            _inner.DespawnAll();
            _spawned.Clear();
        }

        public void Prewarm(int count) => _inner.Prewarm(count);

        public void CollectActive(List<Transform> into)
        {
            _spawned.RemoveWhere(instance => instance == null);
            foreach (var instance in _spawned)
            {
                if (instance.gameObject.activeInHierarchy)
                    into.Add(instance.transform);
            }
        }

        private T Track(T instance)
        {
            _spawned.Add(instance);
            return instance;
        }
    }
}
