using System.Threading;

namespace AF.Core
{
    /// <summary>
    /// One life of a pooled instance, from spawn to despawn. Hold this instead of the instance itself where a reference may
    /// outlive the life: once the instance is despawned, and even after it is spawned again, the old value stays dead.
    /// </summary>
    public readonly struct PoolLife
    {
        private static readonly CancellationToken _ended = new CancellationToken(true);

        private readonly PooledObject _owner;
        private readonly int _generation;
        private readonly CancellationToken _token;

        internal PoolLife(PooledObject owner, int generation, CancellationToken token)
        {
            _owner = owner;
            _generation = generation;
            _token = token;
        }

        /// <summary>Cancelled when this life ends. Already cancelled for a default value.</summary>
        public CancellationToken Token => _owner is null ? _ended : _token;

        /// <summary>True while the instance is alive, spawned and still in this life.</summary>
        public bool IsCurrent => _owner != null && _owner.IsSpawned && _owner.Generation == _generation;

        /// <summary>Despawns the instance if it still lives this life; otherwise does nothing. True when it despawned.</summary>
        public bool Despawn() => IsCurrent && _owner.DespawnSelf();
    }
}
