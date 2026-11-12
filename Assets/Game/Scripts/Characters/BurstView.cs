using AF.Core;
using UnityEngine;
using VContainer;

namespace HoldIt.Characters
{
    /// <summary>
    /// A small cloud that bursts where a relieved character goes, then goes back to its pool by itself once the burst is
    /// over, on game time like the particles. Spawned by <see cref="CharacterViews"/>.
    /// </summary>
    public sealed class PuffView : MonoBehaviour, IPoolable
    {
        [SerializeField, Required] private ParticleSystem _particles;

        private IGameClock _clock;
        private CharacterSet _set;
        private PoolLife _life;
        private float _age;

        [Inject]
        public void Construct(IGameClock clock, CharacterSet set)
        {
            _clock = clock;
            _set = set;
        }

        public void OnSpawned(PoolLife life)
        {
            _life = life;
            _age = 0f;
            _particles.Clear();
            _particles.Play();
        }

        public void OnDespawned() => _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        private void LateUpdate()
        {
            _age += _clock.DeltaTime;
            if (_age >= _set.PuffSeconds)
                _life.Despawn();
        }
    }
}
