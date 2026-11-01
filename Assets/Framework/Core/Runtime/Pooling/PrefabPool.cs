using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace AF.Core
{
    /// <summary>
    /// Wraps <see cref="ObjectPool{T}"/>, which keeps the inactive instances and the size limit. Unity's pool cannot drop an
    /// instance destroyed elsewhere and only sees the inactive ones, so this class tracks every instance it created.
    /// </summary>
    internal sealed class PrefabPool<T> : IPool<T>, IPrefabPool, IOwnedPool where T : Component
    {
        private const int MaxInactive = 10000;

        private readonly T _prefab;
        // Kept apart from the prefab: messages and spawns must not touch it once it is gone (a scene object used as one).
        private readonly string _name;
        private readonly Vector3 _localPosition;
        private readonly Quaternion _localRotation;
        private readonly Vector3 _localScale;
        private readonly IObjectResolver _resolver;
        private readonly Transform _container;
        private readonly Func<FrameStamp> _clock;
        private readonly ObjectPool<T> _inactive;
        private readonly Dictionary<T, Entry> _entries = new Dictionary<T, Entry>(ReferenceComparer.Instance);
        private int _spawned;
        private bool _disposed;

        public PrefabPool(T prefab, IObjectResolver resolver, Transform container, Func<FrameStamp> clock)
        {
            _prefab = prefab;
            _name = prefab.name;
            var source = prefab.transform;
            _localPosition = source.localPosition;
            _localRotation = source.localRotation;
            _localScale = source.localScale;
            _resolver = resolver;
            _container = container;
            _clock = clock;
            _inactive = new ObjectPool<T>(Create, actionOnDestroy: DestroyInstance, collectionCheck: false, maxSize: MaxInactive);
        }

        public T Prefab => _prefab;
        Component IOwnedPool.Source => _prefab;
        public int CountActive => _spawned;
        public int CountInactive => _inactive.CountInactive;

        public T Spawn(Transform parent = null)
        {
            var entry = Take();
            var transform = entry.Instance.transform;
            transform.SetParent(parent, false);
            transform.SetLocalPositionAndRotation(_localPosition, _localRotation);
            transform.localScale = _localScale;
            return Activate(entry);
        }

        public T Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var entry = Take();
            var transform = entry.Instance.transform;
            transform.SetParent(parent, false);
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = _localScale;
            return Activate(entry);
        }

        public void Despawn(T instance)
        {
            if (ReferenceEquals(instance, null))
                throw new ArgumentNullException(nameof(instance));
            if (!_entries.TryGetValue(instance, out var entry))
            {
                // Destroyed and already forgotten, or the pool is gone with everything it created.
                if (instance == null || _disposed)
                    return;
                throw new ArgumentException($"{instance.name} was not spawned from the pool of {_name}.", nameof(instance));
            }
            if (instance == null)
            {
                // Destroyed before it was ever active: Unity sent no OnDestroy.
                Forget(entry);
                return;
            }
            if (entry.Despawning || entry.DespawnRequested)
                return;
            if (!entry.Spawned)
                throw new InvalidOperationException($"{instance.name} is already in its pool; it was despawned twice.");
            RequestDespawn(entry);
        }

        public void DespawnAll()
        {
            foreach (var entry in _entries.Values.Where(entry => entry.Spawned).ToList())
            {
                if (entry.Instance == null)
                    Forget(entry);
                else if (!entry.Despawning && !entry.DespawnRequested)
                    RequestDespawn(entry);
            }
        }

        public void Prewarm(int count)
        {
            ThrowIfDisposed();
            count = Math.Min(count, MaxInactive);
            while (_inactive.CountInactive < count)
                _inactive.Release(Create());
        }

        bool IPrefabPool.Despawn(PooledObject pooled)
        {
            if (_disposed || pooled.PoolEntry is not Entry entry || !entry.Spawned || entry.Despawning || entry.DespawnRequested)
                return false;
            RequestDespawn(entry);
            return true;
        }

        void IPrefabPool.OnDestroyed(PooledObject pooled, bool wasSpawned)
        {
            if (_disposed || pooled.PoolEntry is not Entry entry)
                return;
            Forget(entry);
            if (!wasSpawned || PooledObject.IsQuitting)
                return;

            // Instances die with their pool's scene. One that dies in another scene was moved there, and the pool lost it.
            var scene = pooled.gameObject.scene;
            if (_container != null && scene != _container.gameObject.scene)
            {
                Log.Warning($"A spawned {_name} was destroyed in the scene '{scene.name}', outside the scene of its pool. " +
                            "Despawn it before its parent goes away, or spawn it from the pools of the scope it belongs to.");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            foreach (var entry in _entries.Values.ToList())
            {
                if (entry.Pooled != null)
                    entry.Pooled.EndLife();
                if (entry.Instance != null)
                    Destroy(entry.Instance.gameObject);
            }
            _entries.Clear();
            _spawned = 0;
            _inactive.Clear();
        }

        private Entry Take()
        {
            ThrowIfDisposed();
            while (true)
            {
                var instance = _inactive.Get();
                // Destroyed while it waited: Unity's pool cannot drop it, so it is skipped here.
                if (instance != null && _entries.TryGetValue(instance, out var entry))
                    return entry;
                if (!ReferenceEquals(instance, null))
                    _entries.Remove(instance);
            }
        }

        private T Activate(Entry entry)
        {
            var instance = entry.Instance;
            if (entry.HasDespawned)
                TweenLeakAudit.Check(instance.gameObject, _name, entry.Despawned, _clock());

            var life = entry.Pooled.BeginLife();
            entry.Spawned = true;
            _spawned++;
            entry.Activating = true;
            try
            {
                instance.gameObject.SetActive(true);
            }
            finally
            {
                entry.Activating = false;
            }
            // Awake or OnEnable asked to despawn it: done now that the activation is over.
            if (entry.DespawnRequested)
            {
                entry.DespawnRequested = false;
                DespawnEntry(entry);
                return instance;
            }
            foreach (var hook in entry.Hooks)
            {
                // A hook before this one may have despawned it.
                if (!life.IsCurrent)
                    break;
                if (!hook.IsAlive())
                    continue;
                try
                {
                    hook.OnSpawned(life);
                }
                catch (Exception exception)
                {
                    Log.Exception(exception, instance);
                }
            }
            return instance;
        }

        private void DespawnEntry(Entry entry)
        {
            entry.Despawning = true;
            try
            {
                // First, so no work of this life goes on: awaits bound to the token throw OperationCanceledException (UniTask
                // ones at once; tweens at PrimeTween's next update, before they write a value). An awaited tween stopped any
                // other way would return as if it had completed. Code after an await of a life checks life.IsCurrent before
                // touching the instance: by the time it runs, the instance may be in its next life.
                entry.Pooled.EndLife();
                foreach (var hook in entry.Hooks)
                {
                    if (!hook.IsAlive())
                        continue;
                    try
                    {
                        hook.OnDespawned();
                    }
                    catch (Exception exception)
                    {
                        Log.Exception(exception, entry.Instance);
                    }
                }
                entry.Instance.gameObject.SetActive(false);
                entry.Instance.transform.SetParent(_container, false);
            }
            finally
            {
                entry.Despawning = false;
            }
            entry.Spawned = false;
            _spawned--;
            entry.Despawned = _clock();
            entry.HasDespawned = true;
            _inactive.Release(entry.Instance);
        }

        // Unity ignores deactivating or moving an object while it is being activated, so a despawn asked for from its Awake or
        // OnEnable waits until the activation is over.
        private void RequestDespawn(Entry entry)
        {
            if (entry.Activating)
                entry.DespawnRequested = true;
            else
                DespawnEntry(entry);
        }

        // The copy is made under the inactive container: no Awake or OnEnable runs before it is injected, nor until it is
        // spawned, whatever the prefab root's own active state.
        [InstantiateExempt("The pool injects every instance it creates.")]
        private T Create()
        {
            ThrowIfDisposed();
            var instance = Object.Instantiate(_prefab, _container, false);
            instance.name = _name;
            var gameObject = instance.gameObject;
            gameObject.SetActive(false);
            _resolver.InjectGameObject(gameObject);

            if (!gameObject.TryGetComponent(out PooledObject pooled))
                pooled = gameObject.AddComponent<PooledObject>();
            var entry = new Entry(instance, pooled, gameObject.GetComponentsInChildren<IPoolable>(true));
            pooled.Attach(this, entry);
            _entries.Add(instance, entry);
            return instance;
        }

        // Unity's pool calls it for instances over its size limit and on Clear.
        private void DestroyInstance(T instance)
        {
            if (!ReferenceEquals(instance, null))
                _entries.Remove(instance);
            if (instance != null)
                Destroy(instance.gameObject);
        }

        private void Forget(Entry entry)
        {
            if (!_entries.Remove(entry.Instance) || !entry.Spawned)
                return;
            entry.Spawned = false;
            _spawned--;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException($"Pool of {_name}", "The scope that owned this pool is gone.");
        }

        private static void Destroy(GameObject gameObject)
        {
            if (Application.isPlaying)
                Object.Destroy(gameObject);
            else
                Object.DestroyImmediate(gameObject);
        }

        private sealed class Entry
        {
            public readonly T Instance;
            public readonly PooledObject Pooled;
            public readonly IPoolable[] Hooks;
            public bool Spawned;
            public bool Activating;
            public bool DespawnRequested;
            public bool Despawning;
            public bool HasDespawned;
            public FrameStamp Despawned;

            public Entry(T instance, PooledObject pooled, IPoolable[] hooks)
            {
                Instance = instance;
                Pooled = pooled;
                Hooks = hooks;
            }
        }

        /// <summary>Destroyed Unity objects compare equal to each other; the pool keys its instances by reference.</summary>
        private sealed class ReferenceComparer : IEqualityComparer<T>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(T x, T y) => ReferenceEquals(x, y);
            public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
