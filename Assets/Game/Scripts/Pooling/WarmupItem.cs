using System;
using AF.Core;
using UnityEngine;

namespace HoldIt.Pooling
{
    /// <summary>
    /// One prefab to warm: its instances are made ahead of time, and one is spawned and despawned, so the first real spawn
    /// has nothing left to do but show it (the first Awake, the first particle burst and the first text of a prefab are
    /// the dear part, not the copy). The pool is asked for with the prefab's own type: a pool is keyed by it.
    /// </summary>
    internal sealed class WarmupItem
    {
        private readonly Action<IPools> _warm;

        private WarmupItem(string name, int count, Action<IPools> warm)
        {
            Name = name;
            Count = count;
            _warm = warm;
        }

        public string Name { get; }

        /// <summary>How many instances are made; none or fewer means the prefab is left alone.</summary>
        public int Count { get; }

        public static WarmupItem Of<T>(T prefab, int count) where T : Component => new WarmupItem(prefab.name, count, pools =>
        {
            var pool = pools.For(prefab);
            pool.Prewarm(count);
            pool.Despawn(pool.Spawn());
        });

        public void Run(IPools pools)
        {
            if (Count > 0)
                _warm(pools);
        }
    }
}
