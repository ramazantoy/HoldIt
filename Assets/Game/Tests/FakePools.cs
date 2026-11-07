using System;
using System.Collections.Generic;
using AF.Core;
using HoldIt.Pooling;
using UnityEngine;

namespace HoldIt.Tests
{
    /// <summary>The root's pools without a scene: a pool per prefab that only counts what it is asked.</summary>
    internal sealed class FakePools : IPersistentPools
    {
        private readonly Dictionary<Component, object> _pools = new Dictionary<Component, object>();

        public List<string> Log { get; } = new List<string>();

        public IPool<T> For<T>(T prefab) where T : Component
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new FakePool<T>(prefab, Log);
                _pools.Add(prefab, pool);
            }
            return (IPool<T>)pool;
        }

        public FakePool<T> Pool<T>(T prefab) where T : Component => (FakePool<T>)For(prefab);
    }

    internal sealed class FakePool<T> : IPool<T> where T : Component
    {
        private readonly List<string> _log;

        public FakePool(T prefab, List<string> log)
        {
            Prefab = prefab;
            _log = log;
        }

        public T Prefab { get; }
        public int CountActive { get; private set; }
        public int CountInactive { get; private set; }
        public int Prewarmed { get; private set; }
        public int Spawns { get; private set; }
        public int Despawns { get; private set; }
        public int DespawnAlls { get; private set; }
        public Exception FailOnDespawnAll { get; set; }

        public T Spawn(Transform parent = null)
        {
            Spawns++;
            CountActive++;
            _log.Add($"spawn {Prefab.name}");
            return Prefab;
        }

        public T Spawn(Vector3 position, Quaternion rotation, Transform parent = null) => Spawn(parent);

        public void Despawn(T instance)
        {
            Despawns++;
            CountActive--;
            _log.Add($"despawn {Prefab.name}");
        }

        public void DespawnAll()
        {
            DespawnAlls++;
            if (FailOnDespawnAll != null)
                throw FailOnDespawnAll;
            CountActive = 0;
        }

        public void Prewarm(int count)
        {
            Prewarmed = Math.Max(Prewarmed, count);
            CountInactive = Math.Max(CountInactive, count);
            _log.Add($"prewarm {Prefab.name} {count}");
        }
    }
}
