using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace AF.Core.Tests
{
    /// <summary>Pooled test component that records its callbacks. ExecuteAlways: Edit Mode tests need Awake and OnEnable.</summary>
    [AddComponentMenu(""), ExecuteAlways]
    public sealed class PoolProbe : MonoBehaviour, IPoolable
    {
        [SerializeField]
        private bool _despawnOnEnable;

        [SerializeField]
        private bool _despawnOnSpawned;

        public List<string> Events { get; } = new List<string>();

        public bool DespawnOnEnable
        {
            get => _despawnOnEnable;
            set => _despawnOnEnable = value;
        }

        public bool DespawnOnSpawned
        {
            get => _despawnOnSpawned;
            set => _despawnOnSpawned = value;
        }
        public IPauseService Pause { get; private set; }
        public PoolLife Life { get; private set; }

        [Inject]
        public void Construct(IPauseService pause)
        {
            Pause = pause;
            Events.Add("construct");
        }

        public void OnSpawned(PoolLife life)
        {
            Life = life;
            Events.Add("spawned");
            if (_despawnOnSpawned)
                life.Despawn();
        }

        public void OnDespawned() => Events.Add($"despawned, life cancelled: {Life.Token.IsCancellationRequested}");

        private void Awake() => Events.Add("awake");

        private void OnEnable()
        {
            Events.Add("enable");
            if (_despawnOnEnable && TryGetComponent(out PooledObject pooled))
                pooled.Life.Despawn();
        }

        private void OnDisable() => Events.Add("disable");
    }
}
