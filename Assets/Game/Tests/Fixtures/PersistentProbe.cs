using AF.Core;
using UnityEngine;
using VContainer;

namespace HoldIt.Tests
{
    /// <summary>What the root offers a pooled prefab in the persistent pools tests.</summary>
    public sealed class PersistentService
    {
    }

    /// <summary>A pooled prefab that, like the game's views, is injected once and told when it is spawned and despawned.</summary>
    public sealed class PersistentProbe : MonoBehaviour, IPoolable
    {
        public PersistentService Service { get; private set; }
        public int Spawned { get; private set; }
        public int Despawned { get; private set; }

        [Inject]
        public void Construct(PersistentService service) => Service = service;

        public void OnSpawned(PoolLife life) => Spawned++;

        public void OnDespawned() => Despawned++;
    }
}
