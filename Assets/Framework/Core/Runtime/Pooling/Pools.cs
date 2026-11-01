using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

namespace AF.Core
{
    /// <summary>
    /// Registered once in the root as Scoped: every scope that resolves it gets its own instance, built with that scope's
    /// resolver and disposed with it. Its pools live under an inactive <c>[Pools]</c> object below the scope's GameObject.
    /// </summary>
    public sealed class Pools : IPools, IDisposable
    {
        private const string CONTAINER_NAME = "[Pools]";

        private readonly IObjectResolver _resolver;
        private readonly Transform _parent;
        private readonly Func<FrameStamp> _clock;
        private readonly List<IOwnedPool> _pools = new List<IOwnedPool>();
        private Transform _container;
        private bool _disposed;

        [Inject]
        public Pools(IObjectResolver resolver)
            : this(resolver, (resolver.ApplicationOrigin as Component)?.transform, FrameStamp.Now)
        {
        }

        // Tests pass no scope and their own clock.
        internal Pools(IObjectResolver resolver, Transform parent, Func<FrameStamp> clock)
        {
            _resolver = resolver;
            _parent = parent;
            _clock = clock;
        }

        public IPool<T> For<T>(T prefab) where T : Component
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            if (_disposed)
                throw new ObjectDisposedException(nameof(Pools), "The scope that owned these pools is gone.");

            foreach (var owned in _pools)
            {
                if (ReferenceEquals(owned.Source, prefab) && owned is IPool<T> existing)
                    return existing;
            }

            var group = new GameObject(prefab.name).transform;
            group.SetParent(Container(), false);
            var pool = new PrefabPool<T>(prefab, _resolver, group, _clock);
            _pools.Add(pool);
            return pool;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            foreach (var pool in _pools)
                pool.Dispose();
            _pools.Clear();
            if (_container != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(_container.gameObject);
                else
                    Object.DestroyImmediate(_container.gameObject);
            }
        }

        private Transform Container()
        {
            if (_container != null)
                return _container;
            // Inactive before anything goes under it: instances wait there without Awake or OnEnable.
            var container = new GameObject(CONTAINER_NAME);
            container.SetActive(false);
            if (_parent != null)
                container.transform.SetParent(_parent, false);
            _container = container.transform;
            return _container;
        }
    }
}
