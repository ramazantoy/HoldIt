using System;
using System.Threading;
using UnityEngine;

namespace AF.Core
{
    /// <summary>
    /// Added by the pool to every instance it creates. Tells whether the instance is spawned and hands out its current
    /// <see cref="PoolLife"/>, whose token is cancelled when the instance is despawned.
    /// </summary>
    [AddComponentMenu(""), DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        private IPrefabPool _pool;
        private CancellationTokenSource _lifeSource;
        private CancellationToken _token;

        public bool IsSpawned { get; private set; }

        /// <summary>The current life; an ended one while the instance waits in the pool.</summary>
        public PoolLife Life => IsSpawned ? new PoolLife(this, Generation, _token) : default;

        internal int Generation { get; private set; }

        /// <summary>The pool's bookkeeping for this instance.</summary>
        internal object PoolEntry { get; private set; }

        /// <summary>Set once the app quits (in the Editor, when Play stops): instances destroyed after it are not reported.</summary>
        internal static bool IsQuitting { get; private set; }

        /// <summary>The <see cref="PooledObject"/> of a pooled instance. Throws for an object no pool created.</summary>
        public static PooledObject Of(Component instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            return instance.TryGetComponent(out PooledObject pooled) && pooled._pool != null
                ? pooled
                : throw new ArgumentException($"{instance.name} was not created by a pool.", nameof(instance));
        }

        internal void Attach(IPrefabPool pool, object entry)
        {
            _pool = pool;
            PoolEntry = entry;
        }

        internal PoolLife BeginLife()
        {
            Generation++;
            _lifeSource = new CancellationTokenSource();
            // Kept apart from the source: reading the token of a disposed source throws.
            _token = _lifeSource.Token;
            IsSpawned = true;
            return Life;
        }

        internal void EndLife()
        {
            if (!IsSpawned)
                return;
            IsSpawned = false;
            var source = _lifeSource;
            _lifeSource = null;
            try
            {
                source.Cancel();
            }
            catch (AggregateException exception)
            {
                // A throwing registration must not leave the instance half despawned.
                Log.Exception(exception, this);
            }
            finally
            {
                source.Dispose();
            }
        }

        internal bool DespawnSelf() => _pool != null && _pool.Despawn(this);

        // Unity calls it only for objects that were active at least once; the pool null-checks the rest.
        private void OnDestroy()
        {
            var wasSpawned = IsSpawned;
            EndLife();
            _pool?.OnDestroyed(this, wasSpawned);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void TrackQuitting()
        {
            IsQuitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        private static void OnQuitting() => IsQuitting = true;
    }

    /// <summary>A pool as the <see cref="Pools"/> that owns it sees it.</summary>
    internal interface IOwnedPool : IDisposable
    {
        Component Source { get; }
    }

    /// <summary>What a <see cref="PooledObject"/> reports to the pool that created it.</summary>
    internal interface IPrefabPool
    {
        /// <summary>
        /// Despawns the instance if it is spawned and not already despawning. True when it despawned, or will as soon as its
        /// activation ends.
        /// </summary>
        bool Despawn(PooledObject pooled);

        void OnDestroyed(PooledObject pooled, bool wasSpawned);
    }
}
