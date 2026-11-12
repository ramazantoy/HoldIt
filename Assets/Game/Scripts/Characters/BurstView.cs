using AF.Core;
using UnityEngine;
using VContainer;

namespace HoldIt.Characters
{
    /// <summary>
    /// A burst of particles (a puff, dust, a splash, sparkles) that plays where it is spawned. It goes back to its pool by
    /// itself once its particles are done, on game time like they are; the time comes from the particle system (its duration
    /// and the longest a particle lives). A looping system (sweat) never ends by itself: whoever spawned it despawns it,
    /// through <see cref="Life"/>. Spawned by <see cref="CharacterViews"/> and <see cref="CharacterView"/>.
    /// </summary>
    public sealed class BurstView : MonoBehaviour, IPoolable
    {
        [SerializeField, Required] private ParticleSystem _particles;

        private IGameClock _clock;
        private PoolLife _life;
        private bool _loops;
        private float _seconds;
        private float _age;

        /// <summary>The current life of this instance, for an owner that ends a looping burst.</summary>
        public PoolLife Life => _life;

        [Inject]
        public void Construct(IGameClock clock) => _clock = clock;

        public void OnSpawned(PoolLife life)
        {
            _life = life;
            _age = 0f;
            _particles.Clear();
            _particles.Play();
        }

        public void OnDespawned() => _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        private void Awake()
        {
            var main = _particles.main;
            _loops = main.loop;
            _seconds = BurstTime.Of(main.duration, main.startLifetime.constantMax);
        }

        private void LateUpdate()
        {
            if (_loops)
                return;
            _age += _clock.DeltaTime;
            if (_age >= _seconds)
                _life.Despawn();
        }
    }
}
